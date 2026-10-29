import {
    Component,
    ElementRef,
    afterNextRender,
    computed,
    input,
    output,
    viewChild,
} from '@angular/core';
import { DatePipe } from '@angular/common';
import { meetingTypeLabels } from '../../solicitations/meeting-type';
import { CalendarMeeting, MeetingSelection } from '../calendar.models';
import { eventColor } from '../calendar-colors';
import { isSameDay, layoutDay, weekDays } from '../calendar-date';

/** Pixel height of one hour row. */
const HOUR = 48;
const DAY_MINUTES = 24 * 60;
/** Meetings have no end time, so each is drawn as a one-hour block. */
const DURATION = 60;
/** Hour the grid is scrolled to on open. */
const SCROLL_TO_HOUR = 8;

const minutesOf = (date: Date) => date.getHours() * 60 + date.getMinutes();

/** Time grid for one Monday–Sunday week, with overlapping meetings side by side. */
@Component({
    selector: 'app-week-view',
    imports: [DatePipe],
    templateUrl: './week-view.html',
})
export class WeekView {
    readonly date = input.required<Date>();
    readonly meetings = input.required<CalendarMeeting[]>();
    readonly now = input.required<Date>();

    readonly select = output<MeetingSelection>();

    private readonly scroller = viewChild.required<ElementRef<HTMLElement>>('scroller');

    protected readonly typeLabels = meetingTypeLabels;
    protected readonly eventColor = eventColor;
    protected readonly hour = HOUR;
    protected readonly gridHeight = 24 * HOUR;
    protected readonly hourLines = Array.from({ length: 23 }, (_, i) => i + 1);

    protected readonly days = computed(() => {
        const now = this.now();
        return weekDays(this.date()).map((date) => ({
            date,
            isToday: isSameDay(date, now),
            events: layoutDay(
                this.meetings().filter((m) => isSameDay(m.at, date)),
                (m) => minutesOf(m.at),
                DURATION,
            ).map(({ item, column, columns }) => {
                const start = minutesOf(item.at);
                return {
                    meeting: item,
                    top: (start / 60) * HOUR,
                    // a meeting at 23:30 is cut off at midnight rather than overflowing the grid
                    height: (Math.min(DURATION, DAY_MINUTES - start) / 60) * HOUR - 2,
                    left: `calc(${(column / columns) * 100}% + 2px)`,
                    width: `calc(${100 / columns}% - 4px)`,
                    compact: columns > 2,
                };
            }),
        }));
    });

    protected readonly nowTop = computed(() => (minutesOf(this.now()) / 60) * HOUR);

    constructor() {
        afterNextRender(() => {
            this.scroller().nativeElement.scrollTop = SCROLL_TO_HOUR * HOUR;
        });
    }

    protected hourLabel(hour: number): string {
        return `${String(hour).padStart(2, '0')}:00`;
    }

    protected open(meeting: CalendarMeeting, event: MouseEvent): void {
        this.select.emit({ meeting, origin: event.currentTarget as HTMLElement });
    }
}
