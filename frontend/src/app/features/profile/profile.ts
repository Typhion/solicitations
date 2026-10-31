import { Component, inject, signal } from '@angular/core';
import { rxResource, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Dialog } from '@angular/cdk/dialog';
import { map } from 'rxjs';
import QRCode from 'qrcode';
import { Badge } from '../../shared/ui/badge/badge';
import { Button } from '../../shared/ui/button/button';
import { Field } from '../../shared/ui/field/field';
import { Input } from '../../shared/ui/input/input';
import { AuthenticatorSetup } from './account.models';
import { AccountService } from './account.service';
import { PASSWORD_HINT, matches, passwordStrength } from '../../shared/forms/password-rules';
import {
    PasswordConfirmData,
    PasswordConfirmDialog,
} from './password-confirm-dialog/password-confirm-dialog';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { I18nService } from '../../core/i18n/i18n.service';
import { TranslationKey } from '../../core/i18n/translation.types';

/** `/profile` — the signed-in user's account, password and two-factor authentication. */
@Component({
    selector: 'app-profile',
    imports: [ReactiveFormsModule, Badge, Button, Field, Input, TranslatePipe],
    templateUrl: './profile.html',
})
export class ProfilePage {
    private readonly fb = inject(FormBuilder);
    private readonly account = inject(AccountService);
    private readonly dialog = inject(Dialog);
    private readonly i18n = inject(I18nService);

    readonly passwordHint = PASSWORD_HINT;

    readonly profile = rxResource({ stream: () => this.account.profile() });

    // ---------- Password ----------

    readonly passwordForm = this.fb.nonNullable.group({
        currentPassword: ['', Validators.required],
        newPassword: ['', [Validators.required, passwordStrength]],
        confirmPassword: ['', [Validators.required, matches('newPassword')]],
    });
    readonly passwordSaving = signal(false);
    readonly passwordError = signal<string | null>(null);
    readonly passwordChanged = signal(false);

    // ---------- Two-factor ----------

    /** Pending authenticator setup (QR shown) until the first code is verified. */
    readonly setup = signal<AuthenticatorSetup | null>(null);
    readonly qrDataUrl = signal<string | null>(null);
    readonly setupBusy = signal(false);
    readonly twoFactorError = signal<string | null>(null);
    readonly codeForm = this.fb.nonNullable.group({ code: ['', Validators.required] });

    /** Freshly issued recovery codes — only ever shown once, right after they're created. */
    readonly recoveryCodes = signal<string[] | null>(null);
    readonly copied = signal<'key' | 'codes' | null>(null);

    constructor() {
        // Re-check "confirm" when the new password changes after it was typed.
        this.passwordForm.controls.newPassword.valueChanges
            .pipe(takeUntilDestroyed())
            .subscribe(() => this.passwordForm.controls.confirmPassword.updateValueAndValidity());
    }

    changePassword(): void {
        if (this.passwordForm.invalid) {
            this.passwordForm.markAllAsTouched();
            return;
        }
        const { currentPassword, newPassword } = this.passwordForm.getRawValue();
        this.passwordSaving.set(true);
        this.passwordError.set(null);
        this.passwordChanged.set(false);
        this.account.changePassword({ currentPassword, newPassword }).subscribe({
            next: () => {
                this.passwordForm.reset();
                this.passwordChanged.set(true);
                this.passwordSaving.set(false);
            },
            error: (err: HttpErrorResponse) => {
                this.passwordError.set(this.describe(err));
                this.passwordSaving.set(false);
            },
        });
    }

    startSetup(): void {
        this.setupBusy.set(true);
        this.twoFactorError.set(null);
        this.account.setupAuthenticator().subscribe({
            next: async (setup) => {
                this.setup.set(setup);
                // Dark modules on a light tile, regardless of theme — scanners need the contrast.
                this.qrDataUrl.set(
                    await QRCode.toDataURL(setup.authenticatorUri, { margin: 1, width: 192 }),
                );
                this.setupBusy.set(false);
            },
            error: (err: HttpErrorResponse) => {
                this.twoFactorError.set(this.describe(err));
                this.setupBusy.set(false);
            },
        });
    }

    cancelSetup(): void {
        this.setup.set(null);
        this.qrDataUrl.set(null);
        this.codeForm.reset();
        this.twoFactorError.set(null);
    }

    enable(): void {
        if (this.codeForm.invalid) {
            this.codeForm.markAllAsTouched();
            return;
        }
        this.setupBusy.set(true);
        this.twoFactorError.set(null);
        this.account.enableTwoFactor(this.codeForm.getRawValue().code).subscribe({
            next: ({ recoveryCodes }) => {
                this.cancelSetup();
                this.recoveryCodes.set(recoveryCodes);
                this.setupBusy.set(false);
                this.profile.reload();
            },
            error: (err: HttpErrorResponse) => {
                this.twoFactorError.set(this.describe(err));
                this.setupBusy.set(false);
            },
        });
    }

    disable(): void {
        this.confirmWithPassword({
            title: this.i18n.t('profile.disableTitle'),
            message: this.i18n.t('profile.disableMessage'),
            confirmLabel: this.i18n.t('profile.disable'),
            danger: true,
            action: (password) => this.account.disableTwoFactor(password).pipe(map(() => true)),
        }).subscribe((done) => {
            if (!done) return;
            this.recoveryCodes.set(null);
            this.profile.reload();
        });
    }

    regenerateCodes(): void {
        this.confirmWithPassword({
            title: this.i18n.t('profile.regenerateTitle'),
            message: this.i18n.t('profile.regenerateMessage'),
            confirmLabel: this.i18n.t('profile.generate'),
            action: (password) =>
                this.account.regenerateRecoveryCodes(password).pipe(map((r) => r.recoveryCodes)),
        }).subscribe((codes) => {
            if (!codes) return;
            this.recoveryCodes.set(codes);
            this.profile.reload();
        });
    }

    dismissCodes(): void {
        this.recoveryCodes.set(null);
    }

    copy(what: 'key' | 'codes'): void {
        const text =
            what === 'key'
                ? (this.setup()?.sharedKey.replace(/ /g, '') ?? '')
                : (this.recoveryCodes() ?? []).join('\n');
        navigator.clipboard.writeText(text).then(() => {
            this.copied.set(what);
            setTimeout(() => this.copied.set(null), 2000);
        });
    }

    downloadCodes(): void {
        const codes = this.recoveryCodes();
        if (!codes) return;
        const text = `${this.i18n.t('profile.codesFileHeader')}\n\n${codes.join('\n')}\n`;
        const url = URL.createObjectURL(new Blob([text], { type: 'text/plain' }));
        const a = document.createElement('a');
        a.href = url;
        a.download = 'solicitations-recovery-codes.txt';
        a.click();
        URL.revokeObjectURL(url);
    }

    private confirmWithPassword<T>(data: PasswordConfirmData<T>) {
        this.twoFactorError.set(null);
        return this.dialog.open<T, PasswordConfirmData<T>>(PasswordConfirmDialog<T>, {
            data,
            width: '26rem',
            maxWidth: 'calc(100vw - 2rem)',
        }).closed;
    }

    private describe(err: HttpErrorResponse): string {
        // 400 from the ValidationFilter carries an `errors` map; account failures carry `detail`.
        const fieldErrors = err.error?.errors as Record<string, string[]> | undefined;
        if (fieldErrors) return Object.values(fieldErrors).flat().join(' ');
        if (err.status === 429) return this.i18n.t('errors.tooManyAttempts');
        return err.error?.detail ?? this.i18n.t('errors.generic');
    }

    protected roleLabel(role: string): TranslationKey {
        return role === 'Admin'
            ? 'roles.admin'
            : role === 'Member'
              ? 'roles.member'
              : 'roles.unknown';
    }
}
