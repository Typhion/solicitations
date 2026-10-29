import {
    Component,
    DestroyRef,
    computed,
    inject,
    input,
    linkedSignal,
    signal,
} from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CdkConnectedOverlay, ConnectedPosition } from '@angular/cdk/overlay';
import { Button } from '../../../shared/ui/button/button';
import { Input } from '../../../shared/ui/input/input';
import { Segment, Segmented } from '../../../shared/ui/segmented/segmented';
import { meetingTypeLabels } from '../../solicitations/meeting-type';
import { SolicitationService } from '../../solicitations/solicitation.service';
import { StatusBadge } from '../../solicitations/status-badge/status-badge';
import { AgendaView } from '../agenda-view/agenda-view';
import { CalendarMeeting, CalendarView, MeetingSelection } from '../calendar.models';
import { dotColor } from '../calendar-colors';
import { addDays, fromDateParam, step, toDateParam, visibleRange } from '../calendar-date';
import { MeetingCalendarService } from '../meeting-calendar.service';
import { MonthView } from '../month-view/month-view';
import { WeekView } from '../week-view/week-view';

const VIEWS: CalendarView[] = ['month', 'week', 'agenda'];
const SEARCH_DEBOUNCE_MS = 250;

const POPOVER_POSITIONS: ConnectedPosition[] = [
    { originX: 'start', originY: 'bottom', overlayX: 'start', overlayY: 'top', offsetY: 6 },
    { originX: 'start', originY: 'top', overlayX: 'start', overlayY: 'bottom', offsetY: -6 },
    { originX: 'end', originY: 'top', overlayX: 'start', overlayY: 'top', offsetX: 6 },
    { originX: 'start', originY: 'top', overlayX: 'end', overlayY: 'top', offsetX: -6 },
];

/**
 * `/solicitations/calendar` — read-only calendar of all the user's meetings.
 * All state (view, date, solicitation filter, search) lives in the query string,
 * so links and the back button restore exactly what was on screen.
 */
@Component({
    selector: 'app-meeting-calendar',
    imports: [
        Button,
        Input,
        Segmented,
        Segment,
        RouterLink,
        DatePipe,
        CdkConnectedOverlay,
        StatusBadge,
        MonthView,
        WeekView,
        AgendaView,
    ],
    templateUrl: './meeting-calendar.html',
    host: { '(document:keydown)': 'onKeydown($event)' },
})
export class MeetingCalendar {
    private readonly calendarService = inject(MeetingCalendarService);
    private readonly solicitationService = inject(SolicitationService);
    private readonly router = inject(Router);
    private readonly route = inject(ActivatedRoute);

    // Query params, bound via withComponentInputBinding().
    readonly view = input<string>();
    readonly date = input<string>();
    readonly solicitation = input<string>();
    readonly q = input<string>();

    protected readonly views = VIEWS;
    protected readonly typeLabels = meetingTypeLabels;
    protected readonly dotColor = dotColor;
    protected readonly positions = POPOVER_POSITIONS;

    /** Ticks every minute so "now" markers and past-meeting dimming stay current. */
    protected readonly now = signal(new Date());

    protected readonly currentView = computed<CalendarView>(() =>
        VIEWS.includes(this.view() as CalendarView) ? (this.view() as CalendarView) : 'month',
    );
    protected readonly anchor = computed(() => fromDateParam(this.date()) ?? this.now());
    private readonly range = computed(() => visibleRange(this.currentView(), this.anchor()));

    protected readonly meetings = rxResource({
        params: () => ({ ...this.range(), solicitationId: this.solicitation() ?? null }),
        stream: ({ params }) =>
            this.calendarService.list(params.from, params.to, params.solicitationId),
    });

    // Keep showing the previous range while the next one loads, so the grid doesn't flash empty.
    private readonly loaded = linkedSignal<CalendarMeeting[] | undefined, CalendarMeeting[]>({
        source: () => (this.meetings.hasValue() ? this.meetings.value() : undefined),
        computation: (value, previous) => value ?? previous?.value ?? [],
    });

    protected readonly solicitationOptions = rxResource({
        stream: () => this.solicitationService.list(1, 100),
    });

    /** Typed search text; filters instantly and is written to the URL after a short pause. */
    protected readonly search = linkedSignal(() => this.q() ?? '');
    private searchTimer: ReturnType<typeof setTimeout> | undefined;

    protected readonly visibleMeetings = computed(() => {
        const term = this.search().trim().toLowerCase();
        if (!term) return this.loaded();
        return this.loaded().filter((m) =>
            [m.jobName, m.company, this.typeLabels[m.type], m.onlineTool ?? '', m.city]
                .join(' ')
                .toLowerCase()
                .includes(term),
        );
    });

    protected readonly title = computed(() => {
        const anchor = this.anchor();
        if (this.currentView() !== 'week') {
            return anchor.toLocaleDateString(undefined, { month: 'long', year: 'numeric' });
        }
        // formatRange collapses shared parts: "Oct 26 – Nov 1, 2026" / "Nov 2 – 8, 2026".
        const { from } = this.range();
        return new Intl.DateTimeFormat(undefined, {
            day: 'numeric',
            month: 'short',
            year: 'numeric',
        }).formatRange(from, addDays(from, 6));
    });

    protected readonly filtersActive = computed(
        () => !!this.solicitation() || !!this.search().trim(),
    );

    /** The meeting whose details popover is open. Wrapped in an array so switching recreates the overlay. */
    protected readonly selected = signal<MeetingSelection | null>(null);
    protected readonly popover = computed(() => {
        const s = this.selected();
        return s ? [s] : [];
    });

    constructor() {
        const timer = setInterval(() => this.now.set(new Date()), 60_000);
        inject(DestroyRef).onDestroy(() => {
            clearInterval(timer);
            clearTimeout(this.searchTimer);
        });
    }

    protected previous(): void {
        this.go({ date: toDateParam(step(this.currentView(), this.anchor(), -1)) });
    }

    protected next(): void {
        this.go({ date: toDateParam(step(this.currentView(), this.anchor(), 1)) });
    }

    protected today(): void {
        this.go({ date: null });
    }

    protected setView(view: CalendarView): void {
        this.go({ view: view === 'month' ? null : view });
    }

    protected showDay(date: Date): void {
        this.go({ view: 'week', date: toDateParam(date) });
    }

    protected setSolicitation(id: string): void {
        this.go({ solicitation: id || null });
    }

    protected onSearch(value: string): void {
        this.search.set(value);
        clearTimeout(this.searchTimer);
        this.searchTimer = setTimeout(
            // untrimmed, so the q → search link doesn't eat a trailing space mid-typing
            () => this.go({ q: value || null }, true),
            SEARCH_DEBOUNCE_MS,
        );
    }

    protected clearFilters(): void {
        clearTimeout(this.searchTimer);
        this.search.set('');
        this.go({ solicitation: null, q: null });
    }

    protected open(selection: MeetingSelection): void {
        this.selected.update((current) =>
            current?.meeting.id === selection.meeting.id ? null : selection,
        );
    }

    protected close(): void {
        this.selected.set(null);
    }

    protected onOutsideClick(event: MouseEvent): void {
        // Clicking another meeting re-targets the popover instead of closing it.
        if ((event.target as Element | null)?.closest('[data-meeting]')) return;
        this.close();
    }

    protected onKeydown(event: KeyboardEvent): void {
        if (event.defaultPrevented || event.metaKey || event.ctrlKey || event.altKey) return;
        const target = event.target as HTMLElement | null;
        if (target?.closest('input, select, textarea, [contenteditable="true"]')) return;

        if (event.key === 'Escape' && this.selected()) {
            this.close();
            return;
        }

        const actions: Record<string, () => void> = {
            ArrowLeft: () => this.previous(),
            ArrowRight: () => this.next(),
            t: () => this.today(),
            m: () => this.setView('month'),
            w: () => this.setView('week'),
            a: () => this.setView('agenda'),
        };
        const action = actions[event.key];
        if (action) {
            event.preventDefault();
            action();
        }
    }

    /** Merges into the current query params; null removes a param. */
    private go(params: Record<string, string | null>, replaceUrl = false): void {
        this.close();
        this.router.navigate([], {
            relativeTo: this.route,
            queryParams: params,
            queryParamsHandling: 'merge',
            replaceUrl,
        });
    }
}
