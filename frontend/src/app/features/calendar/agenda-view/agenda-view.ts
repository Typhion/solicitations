import { Component, computed, input, output } from '@angular/core';
import { meetingTypeLabels } from '../../solicitations/meeting-type';
import { StatusBadge } from '../../solicitations/status-badge/status-badge';
import { CalendarMeeting, MeetingSelection } from '../calendar.models';
import { dotColor } from '../calendar-colors';
import { isSameDay, toDateParam } from '../calendar-date';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { LocalDatePipe } from '../../../core/i18n/local-date.pipe';

/** Chronological list of the month's meetings, grouped by day. */
@Component({
    selector: 'app-agenda-view',
    imports: [StatusBadge, TranslatePipe, LocalDatePipe],
    templateUrl: './agenda-view.html',
})
export class AgendaView {
    readonly meetings = input.required<CalendarMeeting[]>();
    readonly now = input.required<Date>();
    /** Shown in the empty state, e.g. "No meetings match …". */
    readonly searching = input(false);

    readonly select = output<MeetingSelection>();

    protected readonly typeLabels = meetingTypeLabels;
    protected readonly dotColor = dotColor;

    protected readonly groups = computed(() => {
        const groups = new Map<string, { date: Date; meetings: CalendarMeeting[] }>();
        for (const m of this.meetings()) {
            const key = toDateParam(m.at);
            const group = groups.get(key) ?? { date: m.at, meetings: [] };
            group.meetings.push(m);
            groups.set(key, group);
        }
        const now = this.now();
        return [...groups.values()].map((g) => ({ ...g, isToday: isSameDay(g.date, now) }));
    });

    protected open(meeting: CalendarMeeting, event: MouseEvent): void {
        this.select.emit({ meeting, origin: event.currentTarget as HTMLElement });
    }
}
