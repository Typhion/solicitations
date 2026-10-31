import { CalendarView } from './calendar.models';

// Pure local-time date helpers for the calendar. Weeks start on Monday.
// Day arithmetic goes through setDate(), so DST transitions don't shift days.

export function startOfDay(date: Date): Date {
    return new Date(date.getFullYear(), date.getMonth(), date.getDate());
}

export function addDays(date: Date, days: number): Date {
    const d = startOfDay(date);
    d.setDate(d.getDate() + days);
    return d;
}

/** Same day-of-month in another month, clamped (31 Jan + 1 month → 28/29 Feb). */
export function addMonths(date: Date, months: number): Date {
    const first = new Date(date.getFullYear(), date.getMonth() + months, 1);
    const lastDay = new Date(first.getFullYear(), first.getMonth() + 1, 0).getDate();
    return new Date(first.getFullYear(), first.getMonth(), Math.min(date.getDate(), lastDay));
}

export function startOfMonth(date: Date): Date {
    return new Date(date.getFullYear(), date.getMonth(), 1);
}

export function startOfWeek(date: Date): Date {
    const mondayOffset = (date.getDay() + 6) % 7; // Sun=0 → 6, Mon=1 → 0
    return addDays(date, -mondayOffset);
}

export function isSameDay(a: Date, b: Date): boolean {
    return (
        a.getFullYear() === b.getFullYear() &&
        a.getMonth() === b.getMonth() &&
        a.getDate() === b.getDate()
    );
}

/** The 7 days of the week containing `date`. */
export function weekDays(date: Date): Date[] {
    const start = startOfWeek(date);
    return Array.from({ length: 7 }, (_, i) => addDays(start, i));
}

/** Always 6 weeks (42 days) so the month grid never changes height. */
export function monthGrid(date: Date): Date[] {
    const start = startOfWeek(startOfMonth(date));
    return Array.from({ length: 42 }, (_, i) => addDays(start, i));
}

/** The [from, to) range a view shows, which is also what gets fetched. */
export function visibleRange(view: CalendarView, date: Date): { from: Date; to: Date } {
    switch (view) {
        case 'month': {
            const from = startOfWeek(startOfMonth(date));
            return { from, to: addDays(from, 42) };
        }
        case 'week': {
            const from = startOfWeek(date);
            return { from, to: addDays(from, 7) };
        }
        case 'agenda': {
            const from = startOfMonth(date);
            return { from, to: addMonths(from, 1) };
        }
    }
}

/** Moves one view-sized step backwards (-1) or forwards (+1). */
export function step(view: CalendarView, date: Date, direction: 1 | -1): Date {
    return view === 'week' ? addDays(date, 7 * direction) : addMonths(date, direction);
}

/** `yyyy-MM-dd` in local time, for the `date` query param. */
export function toDateParam(date: Date): string {
    const pad = (n: number) => String(n).padStart(2, '0');
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
}

/** Parses `yyyy-MM-dd` as a local date; null when missing or malformed. */
export function fromDateParam(value: string | null | undefined): Date | null {
    const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value ?? '');
    if (!match) return null;
    const [year, month, day] = [Number(match[1]), Number(match[2]) - 1, Number(match[3])];
    const date = new Date(year, month, day);
    return date.getMonth() === month && date.getDate() === day ? date : null;
}

export interface PositionedEvent<T> {
    item: T;
    column: number;
    columns: number;
}

/**
 * Side-by-side columns for events in one day column. Each event occupies
 * [start, start + duration); events that overlap (directly or through a
 * chain) form a cluster and share its column count, like Google Calendar.
 */
export function layoutDay<T>(
    items: T[],
    startMinutes: (item: T) => number,
    durationMinutes: (item: T) => number = () => 60,
): PositionedEvent<T>[] {
    const sorted = [...items].sort((a, b) => startMinutes(a) - startMinutes(b));
    const result: PositionedEvent<T>[] = [];
    let cluster: PositionedEvent<T>[] = [];
    let columnEnds: number[] = []; // end minute of the last event in each column
    let clusterEnd = -Infinity;

    const closeCluster = () => {
        for (const e of cluster) e.columns = columnEnds.length;
        cluster = [];
        columnEnds = [];
    };

    for (const item of sorted) {
        const start = startMinutes(item);
        const end = start + durationMinutes(item);
        if (start >= clusterEnd) closeCluster();

        let column = columnEnds.findIndex((end) => end <= start);
        if (column === -1) column = columnEnds.length;
        columnEnds[column] = end;

        const positioned = { item, column, columns: 1 };
        cluster.push(positioned);
        result.push(positioned);
        clusterEnd = Math.max(clusterEnd, end);
    }
    closeCluster();
    return result;
}
