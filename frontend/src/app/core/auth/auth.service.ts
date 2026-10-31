import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, finalize, map, shareReplay, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
    AccessToken,
    InviteLookup,
    LoginRequest,
    RegisterRequest,
    LoginResponse,
    TwoFactorLoginRequest,
    isTwoFactorChallenge,
} from './auth.models';

// System.Security.Claims.ClaimTypes.Role — what JwtTokenService puts in the token.
const ROLE_CLAIM = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';

@Injectable({ providedIn: 'root' })
export class AuthService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.apiUrl}/auth`;

    // Access token is kept in memory only (never persisted) so XSS can't exfiltrate it.
    // The long-lived refresh token lives in an httpOnly cookie the browser sends automatically.
    private readonly accessToken = signal<AccessToken | null>(null);

    // Shared in-flight refresh, so concurrent 401s trigger only ONE /refresh call
    // (your backend's single-use refresh tokens would trip reuse detection otherwise).
    private refreshInFlight$: Observable<AccessToken> | null = null;

    readonly isLoggedIn = computed(() => this.accessToken() !== null);

    // Decoded access-token payload. Only used for display/navigation — the API enforces access.
    private readonly claims = computed<Record<string, unknown>>(() => {
        const token = this.accessToken()?.token;
        if (!token) return {};
        try {
            // JWT payload is base64url; atob needs standard base64.
            const payload = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
            return JSON.parse(atob(payload));
        } catch {
            return {};
        }
    });

    /** Display name from the access token's `unique_name` claim. */
    readonly username = computed(
        () => (this.claims()['unique_name'] as string | undefined) ?? null,
    );

    /**
     * Roles (a string for one role, an array for several). The API writes them under the full
     * .NET claim type URI, not the short `role` name, so accept both.
     */
    readonly roles = computed<string[]>(() => {
        const claims = this.claims();
        const role = claims[ROLE_CLAIM] ?? claims['role'];
        return Array.isArray(role) ? role : typeof role === 'string' ? [role] : [];
    });

    readonly isAdmin = computed(() => this.roles().includes('Admin'));

    /** Saved UI language from the `locale` claim; null when the user hasn't chosen one. */
    readonly locale = computed(() => (this.claims()['locale'] as string | undefined) ?? null);

    get token(): string | null {
        return this.accessToken()?.token ?? null;
    }

    /** Password step. With 2FA on, this yields a challenge and no session yet. */
    login(body: LoginRequest): Observable<LoginResponse> {
        // withCredentials so the Set-Cookie (refresh token) from the response is stored.
        return this.http
            .post<LoginResponse>(`${this.baseUrl}/login`, body, { withCredentials: true })
            .pipe(tap((r) => isTwoFactorChallenge(r) || this.accessToken.set(r)));
    }

    /** Second step: authenticator (or recovery) code for the challenge from `login`. */
    loginWithTwoFactor(body: TwoFactorLoginRequest): Observable<AccessToken> {
        return this.http
            .post<AccessToken>(`${this.baseUrl}/login/2fa`, body, { withCredentials: true })
            .pipe(tap((t) => this.accessToken.set(t)));
    }

    /** What the register page may show for an invite token; 404 when invalid/expired/used. */
    lookupInvite(token: string): Observable<InviteLookup> {
        return this.http.get<InviteLookup>(`${this.baseUrl}/invites/${encodeURIComponent(token)}`);
    }

    /** Creates the account from an invite (the user signs in afterwards). */
    register(body: RegisterRequest): Observable<void> {
        return this.http.post<void>(`${this.baseUrl}/register`, body);
    }

    /** Adopt a session issued elsewhere (e.g. after a password change). */
    setSession(token: AccessToken): void {
        this.accessToken.set(token);
    }

    refresh(): Observable<AccessToken> {
        if (this.refreshInFlight$) return this.refreshInFlight$; // join the in-flight call

        this.refreshInFlight$ = this.http
            .post<AccessToken | null>(`${this.baseUrl}/refresh`, {}, { withCredentials: true })
            .pipe(
                // 204 (null body) = no refresh cookie, i.e. no session. Fail like a 401 would,
                // so the startup probe and the interceptor's logout path behave the same.
                map((t) => {
                    if (!t) throw new Error('No session to refresh');
                    return t;
                }),
                tap((t) => this.accessToken.set(t)),
                finalize(() => (this.refreshInFlight$ = null)), // reset so a later 401 can refresh again
                shareReplay(1), // all concurrent subscribers share one call
            );

        return this.refreshInFlight$;
    }

    logout(): void {
        this.accessToken.set(null);
        // Fire-and-forget server-side revocation; cookie is cleared by the response.
        this.http.post(`${this.baseUrl}/logout`, {}, { withCredentials: true }).subscribe();
    }
}
