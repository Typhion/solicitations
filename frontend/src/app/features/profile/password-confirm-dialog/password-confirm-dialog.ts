import { Component, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { Observable } from 'rxjs';
import { Button } from '../../../shared/ui/button/button';
import { Field } from '../../../shared/ui/field/field';
import { Input } from '../../../shared/ui/input/input';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { I18nService } from '../../../core/i18n/i18n.service';

export interface PasswordConfirmData<T> {
    title: string;
    message: string;
    confirmLabel: string;
    danger?: boolean;
    /** Runs with the entered password; the dialog closes with its result on success. */
    action: (password: string) => Observable<T>;
}

/**
 * Asks for the current password before a sensitive action and runs it.
 * Stays open on errors (e.g. wrong password) so the user can retry.
 */
@Component({
    selector: 'app-password-confirm-dialog',
    imports: [ReactiveFormsModule, Button, Field, Input, TranslatePipe],
    host: {
        class: 'flex flex-col rounded-lg border border-border bg-surface shadow-overlay',
    },
    template: `
        <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
            <div class="flex flex-col gap-4 p-5">
                <div>
                    <h2 class="text-foreground text-base font-semibold">{{ data.title }}</h2>
                    <p class="text-muted mt-1.5 text-sm">{{ data.message }}</p>
                </div>
                <app-field
                    [label]="'profile.currentPassword' | t"
                    [control]="form.controls.password"
                >
                    <input
                        appInput
                        type="password"
                        formControlName="password"
                        autocomplete="current-password"
                    />
                </app-field>
                @if (error()) {
                    <p class="text-danger text-sm" role="alert">{{ error() }}</p>
                }
            </div>
            <footer class="border-border flex justify-end gap-2 border-t px-5 py-3">
                <button appButton type="button" variant="secondary" (click)="ref.close()">
                    {{ 'common.cancel' | t }}
                </button>
                <button
                    appButton
                    type="submit"
                    [variant]="data.danger ? 'danger' : 'primary'"
                    [disabled]="busy()"
                >
                    {{ busy() ? ('common.working' | t) : data.confirmLabel }}
                </button>
            </footer>
        </form>
    `,
})
export class PasswordConfirmDialog<T> {
    protected readonly data = inject<PasswordConfirmData<T>>(DIALOG_DATA);
    protected readonly ref = inject<DialogRef<T>>(DialogRef);
    private readonly i18n = inject(I18nService);

    readonly busy = signal(false);
    readonly error = signal<string | null>(null);
    readonly form = inject(FormBuilder).nonNullable.group({
        password: ['', Validators.required],
    });

    submit(): void {
        if (this.form.invalid) {
            this.form.markAllAsTouched();
            return;
        }
        this.busy.set(true);
        this.error.set(null);
        this.data.action(this.form.getRawValue().password).subscribe({
            next: (result) => this.ref.close(result),
            error: (err: HttpErrorResponse) => {
                this.error.set(err.error?.detail ?? this.i18n.t('errors.generic'));
                this.busy.set(false);
            },
        });
    }
}
