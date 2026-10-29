import { Component, computed, input } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { AbstractControl } from '@angular/forms';
import { startWith, switchMap } from 'rxjs';

/**
 * Label + control + validation message.
 *
 * The control's `events` stream (value/status/touched changes) is turned into a
 * signal, so the message updates on blur and on `markAllAsTouched()`.
 *
 * Usage:
 *   <app-field label="Email" [control]="form.controls.email">
 *     <input appInput type="email" formControlName="email" />
 *   </app-field>
 */
@Component({
    selector: 'app-field',
    host: { class: 'flex flex-col gap-1.5' },
    template: `
        <label class="flex flex-col gap-1.5">
            <span class="text-foreground text-sm font-medium">{{ label() }}</span>
            <ng-content />
        </label>
        @if (error(); as message) {
            <p class="text-danger text-xs">{{ message }}</p>
        }
    `,
})
export class Field {
    readonly label = input.required<string>();
    readonly control = input.required<AbstractControl>();

    private readonly changes = toSignal(
        toObservable(this.control).pipe(switchMap((c) => c.events.pipe(startWith(null)))),
    );

    protected readonly error = computed(() => {
        this.changes();
        const c = this.control();
        if (!c.touched || !c.errors) return null;
        if (c.errors['required']) return 'This field is required.';
        if (c.errors['email']) return 'Enter a valid email address.';
        if (c.errors['maxlength'])
            return `Use at most ${c.errors['maxlength'].requiredLength} characters.`;
        if (c.errors['minlength'])
            return `Use at least ${c.errors['minlength'].requiredLength} characters.`;
        // Custom validators can supply their own message: { someKey: 'Message.' }
        const custom = Object.values(c.errors).find((v) => typeof v === 'string');
        return custom ?? 'This value is invalid.';
    });
}
