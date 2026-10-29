import { Directive, inject } from '@angular/core';
import { NgControl } from '@angular/forms';

/**
 * Text field styling for native form controls.
 *
 * Uses the `input` token (stronger than the hairline `border`) so the control
 * stays visible against `surface` in both themes. When bound to a form control,
 * `aria-invalid` is set once the control is touched and invalid, which switches
 * to the danger border.
 *
 * Usage:
 *   <input appInput type="text" formControlName="name" />
 */
@Directive({
    selector: 'input[appInput], select[appInput], textarea[appInput]',
    host: {
        class: 'h-9 w-full rounded-md border border-input bg-surface px-3 text-sm text-foreground transition-colors outline-none placeholder:text-muted focus-visible:border-ring focus-visible:ring-2 focus-visible:ring-ring/30 disabled:cursor-not-allowed disabled:opacity-50 aria-invalid:border-danger aria-invalid:focus-visible:ring-danger/30',
        '[attr.aria-invalid]': 'invalid',
    },
})
export class Input {
    private readonly ngControl = inject(NgControl, { self: true, optional: true });

    protected get invalid(): true | null {
        return this.ngControl?.invalid && this.ngControl.touched ? true : null;
    }
}
