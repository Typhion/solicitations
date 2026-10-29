import {
    addMonths,
    fromDateParam,
    layoutDay,
    monthGrid,
    startOfWeek,
    step,
    toDateParam,
    visibleRange,
} from './calendar-date';

const ymd = (d: Date) => toDateParam(d);

describe('calendar-date', () => {
    it('starts weeks on Monday, including from a Sunday', () => {
        expect(ymd(startOfWeek(new Date(2026, 9, 29)))).toBe('2026-10-26'); // Thu
        expect(ymd(startOfWeek(new Date(2026, 10, 1)))).toBe('2026-10-26'); // Sun
        expect(ymd(startOfWeek(new Date(2026, 9, 26)))).toBe('2026-10-26'); // Mon
    });

    it('builds a 42-day month grid of consecutive days, across a DST change', () => {
        const grid = monthGrid(new Date(2026, 9, 15)); // DST ends 25 Oct in Europe
        expect(grid).toHaveLength(42);
        expect(ymd(grid[0])).toBe('2026-09-28');
        expect(ymd(grid[41])).toBe('2026-11-08');
        for (let i = 1; i < grid.length; i++) {
            expect(grid[i].getHours()).toBe(0);
            expect(grid[i].getDate()).not.toBe(grid[i - 1].getDate());
        }
    });

    it('clamps the day when adding months', () => {
        expect(ymd(addMonths(new Date(2026, 0, 31), 1))).toBe('2026-02-28');
        expect(ymd(step('month', new Date(2026, 2, 31), -1))).toBe('2026-02-28');
        expect(ymd(step('week', new Date(2026, 9, 29), 1))).toBe('2026-11-05');
    });

    it('computes the fetched range per view', () => {
        const date = new Date(2026, 9, 29);
        const week = visibleRange('week', date);
        expect([ymd(week.from), ymd(week.to)]).toEqual(['2026-10-26', '2026-11-02']);
        const agenda = visibleRange('agenda', date);
        expect([ymd(agenda.from), ymd(agenda.to)]).toEqual(['2026-10-01', '2026-11-01']);
    });

    it('round-trips date params and rejects invalid ones', () => {
        expect(ymd(fromDateParam('2026-10-29')!)).toBe('2026-10-29');
        expect(fromDateParam('2026-02-30')).toBeNull();
        expect(fromDateParam('garbage')).toBeNull();
        expect(fromDateParam(undefined)).toBeNull();
    });

    describe('layoutDay', () => {
        const layout = (starts: number[]) =>
            layoutDay(starts, (s) => s).map((e) => [e.item, e.column, e.columns]);

        it('gives non-overlapping events the full width', () => {
            expect(layout([540, 600, 660])).toEqual([
                [540, 0, 1],
                [600, 0, 1],
                [660, 0, 1],
            ]);
        });

        it('puts overlapping events side by side and reuses freed columns', () => {
            // 9:00, 9:30, 10:00 → the 10:00 one fits back in column 0
            expect(layout([570, 540, 600])).toEqual([
                [540, 0, 2],
                [570, 1, 2],
                [600, 0, 2],
            ]);
        });

        it('keeps separate clusters independent', () => {
            expect(layout([540, 540, 540, 900])).toEqual([
                [540, 0, 3],
                [540, 1, 3],
                [540, 2, 3],
                [900, 0, 1],
            ]);
        });
    });
});
