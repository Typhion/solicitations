import { Component, computed, input, output } from '@angular/core';
import { meetingTypeLabels } from '../../solicitations/meeting-type';
import { CalendarMeeting, MeetingSelection } from '../calendar.models';
import { eventColor } from '../calendar-colors';
import { isSameDay, monthGrid, toDateParam } from '../calendar-date';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { LocalDatePipe } from '../../../core/i18n/local-date.pipe';

const MAX_CHIPS = 3;

/** 6×7 month grid; each day lists its meetings as chips, overflowing into "+N more". */
@Component({
    selector: 'app-month-view',
    imports: [TranslatePipe, LocalDatePipe],
    templateUrl: './month-view.html',
})
export class MonthView {
    readonly date = input.required<Date>();
    readonly meetings = input.required<CalendarMeeting[]>();
    readonly now = input.required<Date>();

    readonly select = output<MeetingSelection>();
    readonly showDay = output<Date>();

    protected readonly typeLabels = meetingTypeLabels;
    protected readonly eventColor = eventColor;
    protected readonly weekdays = monthGrid(new Date(2024, 0, 1)).slice(0, 7); // any Monday-start week

    protected readonly days = computed(() => {
        const byDay = new Map<string, CalendarMeeting[]>();
        for (const m of this.meetings()) {
            const key = toDateParam(m.at);
            byDay.set(key, [...(byDay.get(key) ?? []), m]);
        }

        const month = this.date().getMonth();
        const now = this.now();
        return monthGrid(this.date()).map((date) => {
            const meetings = byDay.get(toDateParam(date)) ?? [];
            // Show all when they fit; otherwise leave the last row for "+N more".
            const visible =
                meetings.length > MAX_CHIPS ? meetings.slice(0, MAX_CHIPS - 1) : meetings;
            return {
                date,
                inMonth: date.getMonth() === month,
                isToday: isSameDay(date, now),
                visible,
                hidden: meetings.length - visible.length,
            };
        });
    });

    protected open(meeting: CalendarMeeting, event: MouseEvent): void {
        this.select.emit({ meeting, origin: event.currentTarget as HTMLElement });
    }
}
