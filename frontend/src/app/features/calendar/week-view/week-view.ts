import {
    Component,
    ElementRef,
    afterNextRender,
    computed,
    input,
    output,
    viewChild,
} from '@angular/core';
import { meetingTypeLabels } from '../../solicitations/meeting-type';
import { CalendarMeeting, MeetingSelection } from '../calendar.models';
import { eventColor } from '../calendar-colors';
import { isSameDay, layoutDay, weekDays } from '../calendar-date';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { LocalDatePipe } from '../../../core/i18n/local-date.pipe';

/** Pixel height of one hour row. */
const HOUR = 48;
const DAY_MINUTES = 24 * 60;
/** Shortest block drawn, so very short meetings stay clickable and readable. */
const MIN_BLOCK_MINUTES = 20;
/** Hour the grid is scrolled to on open. */
const SCROLL_TO_HOUR = 8;

const minutesOf = (date: Date) => date.getHours() * 60 + date.getMinutes();
const durationOf = (m: CalendarMeeting) => (m.end.getTime() - m.at.getTime()) / 60_000;

/** Time grid for one Monday–Sunday week, with overlapping meetings side by side. */
@Component({
    selector: 'app-week-view',
    imports: [TranslatePipe, LocalDatePipe],
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
                durationOf,
            ).map(({ item, column, columns }) => {
                const start = minutesOf(item.at);
                const duration = Math.max(durationOf(item), MIN_BLOCK_MINUTES);
                return {
                    meeting: item,
                    top: (start / 60) * HOUR,
                    // a meeting running past midnight is cut off rather than overflowing the grid
                    height: (Math.min(duration, DAY_MINUTES - start) / 60) * HOUR - 2,
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
