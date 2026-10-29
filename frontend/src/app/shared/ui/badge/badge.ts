import { Directive, computed, input } from '@angular/core';

export type BadgeTone = 'neutral' | 'accent' | 'success' | 'warning' | 'danger';

const base =
    'inline-flex items-center gap-1.5 rounded-full px-2 py-0.5 text-xs font-medium whitespace-nowrap';

// Tinted backgrounds (token at low opacity) read well on both light and dark surfaces.
const tones: Record<BadgeTone, string> = {
    neutral: 'bg-muted/15 text-muted',
    accent: 'bg-accent/15 text-accent',
    success: 'bg-success/15 text-success',
    warning: 'bg-warning/20 text-foreground',
    danger: 'bg-danger/15 text-danger',
};

/**
 * Small status pill.
 *
 * Usage:
 *   <span appBadge tone="success">Accepted</span>
 */
@Directive({
    selector: 'span[appBadge]',
    host: { '[class]': 'classes()' },
})
export class Badge {
    readonly tone = input<BadgeTone>('neutral');

    protected readonly classes = computed(() => `${base} ${tones[this.tone()]}`);
}
