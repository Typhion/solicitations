import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { parseUtc } from '../solicitations/meeting-type';
import { CalendarMeeting, CalendarMeetingDto } from './calendar.models';

@Injectable({ providedIn: 'root' })
export class MeetingCalendarService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.apiUrl}/meetings`;

    // GET /api/meetings?from=&to=&solicitationId= → meetings in [from, to), ordered by time
    list(from: Date, to: Date, solicitationId?: string | null): Observable<CalendarMeeting[]> {
        let params = new HttpParams().set('from', from.toISOString()).set('to', to.toISOString());
        if (solicitationId) params = params.set('solicitationId', solicitationId);
        return this.http
            .get<CalendarMeetingDto[]>(this.baseUrl, { params })
            .pipe(map((items) => items.map((m) => ({ ...m, at: parseUtc(m.scheduledAtUtc) }))));
    }
}
