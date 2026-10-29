import { Directive, computed, input } from '@angular/core';

export type ButtonVariant = 'primary' | 'secondary' | 'ghost' | 'danger';
export type ButtonSize = 'sm' | 'md' | 'icon';

const base =
    'inline-flex cursor-pointer items-center justify-center gap-1.5 rounded-md font-medium whitespace-nowrap transition-colors focus-visible:ring-2 focus-visible:ring-ring/40 focus-visible:outline-none disabled:pointer-events-none disabled:opacity-50';

const variants: Record<ButtonVariant, string> = {
    primary: 'bg-accent text-accent-foreground hover:bg-accent-hover',
    secondary: 'border border-input bg-surface text-foreground hover:bg-surface-hover',
    ghost: 'text-muted hover:bg-surface-hover hover:text-foreground',
    danger: 'bg-danger text-white hover:bg-danger/90',
};

const sizes: Record<ButtonSize, string> = {
    sm: 'h-8 px-2.5 text-sm',
    md: 'h-9 px-3.5 text-sm',
    icon: 'h-8 w-8',
};

/**
 * Button styling for native <button> / <a>.
 *
 * Usage:
 *   <button appButton type="submit">Save</button>
 *   <button appButton variant="secondary" size="sm" (click)="next()">Next</button>
 */
@Directive({
    selector: 'button[appButton], a[appButton]',
    host: { '[class]': 'classes()' },
})
export class Button {
    readonly variant = input<ButtonVariant>('primary');
    readonly size = input<ButtonSize>('md');

    protected readonly classes = computed(
        () => `${base} ${variants[this.variant()]} ${sizes[this.size()]}`,
    );
}
