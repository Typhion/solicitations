import { Component, computed, inject, input } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { AbstractControl } from '@angular/forms';
import { startWith, switchMap } from 'rxjs';
import { I18nService } from '../../../core/i18n/i18n.service';
import { TranslationKey } from '../../../core/i18n/translation.types';

/**
 * Label + control + validation message.
 *
 * The control's `events` stream (value/status/touched changes) is turned into a
 * signal, so the message updates on blur and on `markAllAsTouched()`.
 *
 * Usage:
 *   <app-field [label]="'auth.email' | t" [control]="form.controls.email">
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

    private readonly i18n = inject(I18nService);

    private readonly changes = toSignal(
        toObservable(this.control).pipe(switchMap((c) => c.events.pipe(startWith(null)))),
    );

    protected readonly error = computed(() => {
        this.changes();
        const c = this.control();
        if (!c.touched || !c.errors) return null;
        const t = this.i18n.t.bind(this.i18n);
        if (c.errors['required']) return t('validation.required');
        if (c.errors['email']) return t('validation.email');
        if (c.errors['maxlength'])
            return t('validation.maxLength', { count: c.errors['maxlength'].requiredLength });
        if (c.errors['minlength'])
            return t('validation.minLength', { count: c.errors['minlength'].requiredLength });
        // Custom validators supply a translation key as their message: { someKey: 'validation.x' }
        const custom = Object.values(c.errors).find((v) => typeof v === 'string');
        return t((custom ?? 'validation.invalid') as TranslationKey);
    });
}
