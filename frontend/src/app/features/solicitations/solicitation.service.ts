import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResult } from '../../core/models/paged-result';
import {
    Solicitation,
    CreateSolicitationRequest,
    UpdateSolicitationRequest,
    AddMeetingRequest,
} from './solicitation.models';

@Injectable({ providedIn: 'root' })
export class SolicitationService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.apiUrl}/solicitations`;

    // GET /api/solicitations?page=&pageSize=
    list(page = 1, pageSize = 20): Observable<PagedResult<Solicitation>> {
        const params = new HttpParams()
            .set('page', page)
            .set('pageSize', pageSize);
        return this.http.get<PagedResult<Solicitation>>(this.baseUrl, { params });
    }

    // GET /api/solicitations/{id}
    get(id: string): Observable<Solicitation> {
        return this.http.get<Solicitation>(`${this.baseUrl}/${id}`);
    }

    // POST /api/solicitations → 201 with the created Solicitation
    create(body: CreateSolicitationRequest): Observable<Solicitation> {
        return this.http.post<Solicitation>(this.baseUrl, body);
    }

    // PUT /api/solicitations/{id} → 204 No Content
    update(id: string, body: UpdateSolicitationRequest): Observable<void> {
        return this.http.put<void>(`${this.baseUrl}/${id}`, body);
    }

    // DELETE /api/solicitations/{id} → 204
    delete(id: string): Observable<void> {
        return this.http.delete<void>(`${this.baseUrl}/${id}`);
    }

    // POST /api/solicitations/{id}/meetings → 200 with updated Solicitation
    addMeeting(id: string, body: AddMeetingRequest): Observable<Solicitation> {
        return this.http.post<Solicitation>(`${this.baseUrl}/${id}/meetings`, body);
    }

    // DELETE /api/solicitations/{id}/meetings/{meetingId} → 204
    removeMeeting(id: string, meetingId: string): Observable<void> {
        return this.http.delete<void>(`${this.baseUrl}/${id}/meetings/${meetingId}`);
    }
}
