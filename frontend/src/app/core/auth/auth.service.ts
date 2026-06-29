import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, finalize, shareReplay, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AccessToken, LoginRequest } from './auth.models';

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

    get token(): string | null {
        return this.accessToken()?.token ?? null;
    }

    login(body: LoginRequest): Observable<AccessToken> {
        // withCredentials so the Set-Cookie (refresh token) from the response is stored.
        return this.http.post<AccessToken>(`${this.baseUrl}/login`, body, { withCredentials: true }).pipe(
            tap(t => this.accessToken.set(t)),
        );
    }

    refresh(): Observable<AccessToken> {
        if (this.refreshInFlight$) return this.refreshInFlight$; // join the in-flight call

        this.refreshInFlight$ = this.http
            .post<AccessToken>(`${this.baseUrl}/refresh`, {}, { withCredentials: true })
            .pipe(
                tap(t => this.accessToken.set(t)),
                finalize(() => (this.refreshInFlight$ = null)), // reset so a later 401 can refresh again
                shareReplay(1),                                 // all concurrent subscribers share one call
            );

        return this.refreshInFlight$;
    }

    logout(): void {
        this.accessToken.set(null);
        // Fire-and-forget server-side revocation; cookie is cleared by the response.
        this.http.post(`${this.baseUrl}/logout`, {}, { withCredentials: true }).subscribe();
    }
}
