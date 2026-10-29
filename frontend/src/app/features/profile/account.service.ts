import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AccessToken } from '../../core/auth/auth.models';
import { AuthService } from '../../core/auth/auth.service';
import {
    AuthenticatorSetup,
    ChangePasswordRequest,
    Profile,
    RecoveryCodes,
} from './account.models';

@Injectable({ providedIn: 'root' })
export class AccountService {
    private readonly http = inject(HttpClient);
    private readonly auth = inject(AuthService);
    private readonly baseUrl = `${environment.apiUrl}/account`;

    // GET /api/account
    profile(): Observable<Profile> {
        return this.http.get<Profile>(this.baseUrl);
    }

    // POST /api/account/password → new session; every other session is signed out.
    // withCredentials so the new refresh cookie from the response is stored.
    changePassword(body: ChangePasswordRequest): Observable<AccessToken> {
        return this.http
            .post<AccessToken>(`${this.baseUrl}/password`, body, { withCredentials: true })
            .pipe(tap((t) => this.auth.setSession(t)));
    }

    // POST /api/account/2fa/setup → fresh key (only while 2FA is off)
    setupAuthenticator(): Observable<AuthenticatorSetup> {
        return this.http.post<AuthenticatorSetup>(`${this.baseUrl}/2fa/setup`, {});
    }

    // POST /api/account/2fa/enable → recovery codes (shown once)
    enableTwoFactor(code: string): Observable<RecoveryCodes> {
        return this.http.post<RecoveryCodes>(`${this.baseUrl}/2fa/enable`, { code });
    }

    // POST /api/account/2fa/disable → 204
    disableTwoFactor(password: string): Observable<void> {
        return this.http.post<void>(`${this.baseUrl}/2fa/disable`, { password });
    }

    // POST /api/account/2fa/recovery-codes → new set; old codes stop working
    regenerateRecoveryCodes(password: string): Observable<RecoveryCodes> {
        return this.http.post<RecoveryCodes>(`${this.baseUrl}/2fa/recovery-codes`, { password });
    }
}
