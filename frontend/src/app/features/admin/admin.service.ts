import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
    CreateInviteRequest,
    CreatedInvite,
    DeleteUserRequest,
    Invite,
    UserAccount,
} from './admin.models';

/** Admin-only endpoints. Every change also requires the admin to have 2FA enabled (API-enforced). */
@Injectable({ providedIn: 'root' })
export class AdminService {
    private readonly http = inject(HttpClient);
    private readonly usersUrl = `${environment.apiUrl}/users`;
    private readonly invitesUrl = `${environment.apiUrl}/invites`;

    // ---------- Users ----------

    // GET /api/users
    users(): Observable<UserAccount[]> {
        return this.http.get<UserAccount[]>(this.usersUrl);
    }

    // POST /api/users/{id}/roles → 204
    grantAdmin(id: string): Observable<void> {
        return this.http.post<void>(`${this.usersUrl}/${id}/roles`, { role: 'Admin' });
    }

    // DELETE /api/users/{id}/roles/Admin → 204
    revokeAdmin(id: string): Observable<void> {
        return this.http.delete<void>(`${this.usersUrl}/${id}/roles/Admin`);
    }

    // POST /api/users/{id}/lock | unlock → 204
    setLocked(id: string, locked: boolean): Observable<void> {
        return this.http.post<void>(`${this.usersUrl}/${id}/${locked ? 'lock' : 'unlock'}`, {});
    }

    // POST /api/users/{id}/delete → 204. Needs the typed username + the admin's authenticator code.
    deleteUser(id: string, body: DeleteUserRequest): Observable<void> {
        return this.http.post<void>(`${this.usersUrl}/${id}/delete`, body);
    }

    // ---------- Invites ----------

    // GET /api/invites
    invites(): Observable<Invite[]> {
        return this.http.get<Invite[]>(this.invitesUrl);
    }

    // POST /api/invites → 201 (always a Member invite)
    createInvite(body: CreateInviteRequest): Observable<CreatedInvite> {
        return this.http.post<CreatedInvite>(this.invitesUrl, body);
    }

    // POST /api/invites/{id}/resend → new link; the old one stops working
    resendInvite(id: string): Observable<CreatedInvite> {
        return this.http.post<CreatedInvite>(`${this.invitesUrl}/${id}/resend`, {});
    }

    // POST /api/invites/{id}/revoke → 204
    revokeInvite(id: string): Observable<void> {
        return this.http.post<void>(`${this.invitesUrl}/${id}/revoke`, {});
    }
}
