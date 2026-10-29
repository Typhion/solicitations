import { Component, inject, input, signal } from '@angular/core';
import { rxResource, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { of } from 'rxjs';
import { AuthService } from '../../../core/auth/auth.service';
import { PASSWORD_HINT, matches, passwordStrength } from '../../../shared/forms/password-rules';
import { Button } from '../../../shared/ui/button/button';
import { Field } from '../../../shared/ui/field/field';
import { Input } from '../../../shared/ui/input/input';

/** `/register?token=…` — create an account from an emailed invite link. */
@Component({
    selector: 'app-register',
    imports: [ReactiveFormsModule, RouterLink, Button, Field, Input],
    templateUrl: './register.html',
})
export class Register {
    private readonly auth = inject(AuthService);
    private readonly router = inject(Router);

    /** Query param, bound via withComponentInputBinding(). */
    readonly token = input<string>();

    readonly passwordHint = PASSWORD_HINT;
    readonly saving = signal(false);
    readonly error = signal<string | null>(null);

    // Check the invite before showing the form, so a dead link fails fast.
    readonly invite = rxResource({
        params: () => ({ token: this.token() }),
        stream: ({ params }) => (params.token ? this.auth.lookupInvite(params.token) : of(null)),
    });

    readonly form = inject(FormBuilder).nonNullable.group({
        username: ['', [Validators.required, Validators.maxLength(256)]],
        password: ['', [Validators.required, passwordStrength]],
        confirmPassword: ['', [Validators.required, matches('password')]],
    });

    constructor() {
        this.form.controls.password.valueChanges
            .pipe(takeUntilDestroyed())
            .subscribe(() => this.form.controls.confirmPassword.updateValueAndValidity());
    }

    submit(): void {
        const token = this.token();
        const invite = this.invite.hasValue() ? this.invite.value() : null;
        if (!token || !invite) return;
        if (this.form.invalid) {
            this.form.markAllAsTouched();
            return;
        }

        const { username, password } = this.form.getRawValue();
        this.saving.set(true);
        this.error.set(null);
        // The email is fixed by the invite; the API rejects any other address.
        this.auth.register({ token, username, email: invite.email ?? '', password }).subscribe({
            next: () =>
                this.router.navigate(['/login'], { queryParams: { registered: 1, username } }),
            error: (err: HttpErrorResponse) => {
                const fieldErrors = err.error?.errors as Record<string, string[]> | undefined;
                this.error.set(
                    fieldErrors
                        ? Object.values(fieldErrors).flat().join(' ')
                        : (err.error?.detail ?? 'Something went wrong. Please try again.'),
                );
                this.saving.set(false);
            },
        });
    }
}
