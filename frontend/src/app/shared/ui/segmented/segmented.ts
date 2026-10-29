import { Directive, computed, input } from '@angular/core';

/**
 * Segmented control: a pill group of mutually exclusive options (view switches).
 *
 * Usage:
 *   <nav appSegmented aria-label="View">
 *     <a appSegment routerLink="/solicitations" [active]="true">List</a>
 *     <a appSegment routerLink="/solicitations/calendar">Calendar</a>
 *   </nav>
 */
@Directive({
    selector: '[appSegmented]',
    host: {
        class: 'inline-flex items-center gap-0.5 rounded-md border border-border bg-surface-hover p-0.5',
    },
})
export class Segmented {}

const base =
    'inline-flex h-7 cursor-pointer items-center gap-1.5 rounded px-2.5 text-sm font-medium whitespace-nowrap transition-colors focus-visible:ring-2 focus-visible:ring-ring/40 focus-visible:outline-none';

@Directive({
    selector: 'a[appSegment], button[appSegment]',
    host: {
        '[class]': 'classes()',
        '[attr.aria-current]': "active() ? 'true' : null",
    },
})
export class Segment {
    readonly active = input(false);

    protected readonly classes = computed(
        () =>
            `${base} ${
                this.active()
                    ? 'bg-surface text-foreground ring-1 ring-border'
                    : 'text-muted hover:text-foreground'
            }`,
    );
}
