import { Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { Button } from '../../../shared/ui/button/button';
import { Field } from '../../../shared/ui/field/field';
import { Input } from '../../../shared/ui/input/input';
import { UserAccount } from '../admin.models';
import { AdminService } from '../admin.service';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { I18nService } from '../../../core/i18n/i18n.service';

/**
 * Deliberately high-friction: the admin re-types the username and enters their own
 * authenticator code. The API checks both again. Closes with `true` once deleted.
 */
@Component({
    selector: 'app-delete-user-dialog',
    imports: [ReactiveFormsModule, Button, Field, Input, TranslatePipe],
    host: {
        class: 'flex flex-col rounded-lg border border-border bg-surface shadow-overlay',
    },
    template: `
        <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
            <div class="flex flex-col gap-4 p-5">
                <div>
                    <h2 class="text-danger text-base font-semibold">
                        {{ 'admin.deleteTitle' | t: { name: user.username } }}
                    </h2>
                    <p class="text-muted mt-1.5 text-sm">
                        {{
                            (user.solicitationCount === 0
                                ? 'admin.deleteMessage'
                                : 'admin.deleteMessageWithData'
                            ) | t: { count: user.solicitationCount }
                        }}
                    </p>
                </div>

                <app-field [label]="'admin.typeUsername' | t" [control]="form.controls.username">
                    <input
                        appInput
                        type="text"
                        formControlName="username"
                        autocomplete="off"
                        spellcheck="false"
                        [placeholder]="user.username"
                    />
                </app-field>

                <app-field [label]="'admin.yourCode' | t" [control]="form.controls.code">
                    <input
                        appInput
                        type="text"
                        inputmode="numeric"
                        autocomplete="one-time-code"
                        maxlength="7"
                        placeholder="123456"
                        class="font-mono tracking-widest"
                        formControlName="code"
                    />
                </app-field>

                @if (error()) {
                    <p class="text-danger text-sm" role="alert">{{ error() }}</p>
                }
            </div>
            <footer class="border-border flex justify-end gap-2 border-t px-5 py-3">
                <button appButton type="button" variant="secondary" (click)="ref.close(false)">
                    {{ 'common.cancel' | t }}
                </button>
                <button
                    appButton
                    type="submit"
                    variant="danger"
                    [disabled]="!usernameMatches() || busy()"
                >
                    {{ (busy() ? 'admin.deleting' : 'admin.deleteUser') | t }}
                </button>
            </footer>
        </form>
    `,
})
export class DeleteUserDialog {
    private readonly admin = inject(AdminService);
    private readonly i18n = inject(I18nService);
    protected readonly user = inject<UserAccount>(DIALOG_DATA);
    protected readonly ref = inject<DialogRef<boolean>>(DialogRef);

    readonly busy = signal(false);
    readonly error = signal<string | null>(null);
    readonly form = inject(FormBuilder).nonNullable.group({
        username: ['', Validators.required],
        code: ['', Validators.required],
    });

    private readonly typed = toSignal(this.form.controls.username.valueChanges, {
        initialValue: '',
    });
    /** Delete stays disabled until the exact username is typed. */
    readonly usernameMatches = computed(() => this.typed().trim() === this.user.username);

    submit(): void {
        if (this.form.invalid || !this.usernameMatches()) {
            this.form.markAllAsTouched();
            return;
        }
        this.busy.set(true);
        this.error.set(null);
        this.admin.deleteUser(this.user.id, this.form.getRawValue()).subscribe({
            next: () => this.ref.close(true),
            error: (err: HttpErrorResponse) => {
                this.error.set(err.error?.detail ?? this.i18n.t('errors.generic'));
                this.form.controls.code.reset();
                this.busy.set(false);
            },
        });
    }
}
