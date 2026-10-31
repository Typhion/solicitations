import { Component, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { isTwoFactorChallenge } from '../../../core/auth/auth.models';
import { AuthService } from '../../../core/auth/auth.service';
import { I18nService } from '../../../core/i18n/i18n.service';
import { LanguagePreference } from '../../../core/i18n/language-preference';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { Button } from '../../../shared/ui/button/button';
import { Input } from '../../../shared/ui/input/input';

@Component({
    selector: 'app-login',
    imports: [ReactiveFormsModule, Input, Button, TranslatePipe],
    templateUrl: './login.html',
})
export class Login {
    private readonly fb = inject(FormBuilder);
    private readonly auth = inject(AuthService);
    private readonly router = inject(Router);
    private readonly route = inject(ActivatedRoute);
    private readonly i18n = inject(I18nService);
    private readonly languagePreference = inject(LanguagePreference);

    // UI state, held in signals.
    readonly error = signal<string | null>(null);
    readonly submitting = signal(false);

    /** Set after a correct password when the account has 2FA on → shows the code step. */
    readonly challengeToken = signal<string | null>(null);
    readonly useRecoveryCode = signal(false);

    // Reactive form. nonNullable so the values are typed as string (not string|null).
    /** Arriving from the register page: show a note and pre-fill the new username. */
    readonly justRegistered = this.route.snapshot.queryParamMap.has('registered');

    /** Sent here because the API ended the session (e.g. account locked, or refresh expired). */
    readonly sessionEnded = this.route.snapshot.queryParamMap.has('sessionEnded');

    readonly form = this.fb.nonNullable.group({
        username: [this.route.snapshot.queryParamMap.get('username') ?? '', Validators.required],
        password: ['', Validators.required],
    });

    readonly codeForm = this.fb.nonNullable.group({
        code: ['', Validators.required],
    });

    submit(): void {
        if (this.form.invalid) {
            this.form.markAllAsTouched();
            return;
        }

        this.submitting.set(true);
        this.error.set(null);

        this.auth.login(this.form.getRawValue()).subscribe({
            next: (res) => {
                if (isTwoFactorChallenge(res)) {
                    this.challengeToken.set(res.challengeToken);
                    this.submitting.set(false);
                } else {
                    this.done();
                }
            },
            error: (err: HttpErrorResponse) => {
                // 401 = genuinely bad credentials; anything else is a network/server/CORS problem.
                this.error.set(
                    this.i18n.t(err.status === 401 ? 'auth.invalidCredentials' : 'errors.generic'),
                );
                this.submitting.set(false);
            },
        });
    }

    submitCode(): void {
        const challengeToken = this.challengeToken();
        if (!challengeToken) return;
        if (this.codeForm.invalid) {
            this.codeForm.markAllAsTouched();
            return;
        }

        this.submitting.set(true);
        this.error.set(null);

        this.auth
            .loginWithTwoFactor({
                challengeToken,
                code: this.codeForm.getRawValue().code,
                useRecoveryCode: this.useRecoveryCode(),
            })
            .subscribe({
                next: () => this.done(),
                error: (err: HttpErrorResponse) => {
                    // The API explains 401s (wrong code, expired challenge, locked out) in `detail`.
                    this.error.set(
                        err.status === 401
                            ? (err.error?.detail ?? this.i18n.t('auth.invalidCode'))
                            : this.i18n.t('errors.generic'),
                    );
                    this.submitting.set(false);
                },
            });
    }

    toggleRecoveryCode(): void {
        this.useRecoveryCode.update((v) => !v);
        this.codeForm.reset();
        this.error.set(null);
    }

    /** Back to the password step (e.g. the 5-minute challenge expired). */
    restart(): void {
        this.challengeToken.set(null);
        this.useRecoveryCode.set(false);
        this.codeForm.reset();
        this.form.controls.password.reset();
        this.error.set(null);
    }

    private done(): void {
        this.languagePreference.signedIn();
        const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') ?? '/solicitations';
        this.router.navigateByUrl(returnUrl);
    }
}
