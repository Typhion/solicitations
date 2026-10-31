import { Component, computed, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Dialog } from '@angular/cdk/dialog';
import { CdkMenuTrigger } from '@angular/cdk/menu';
import { RouterLink } from '@angular/router';
import { Observable, filter, switchMap } from 'rxjs';
import { AuthService } from '../../../core/auth/auth.service';
import { Badge, BadgeTone } from '../../../shared/ui/badge/badge';
import { Button } from '../../../shared/ui/button/button';
import { ConfirmService } from '../../../shared/ui/dialog/confirm-dialog';
import { Field } from '../../../shared/ui/field/field';
import { Input } from '../../../shared/ui/input/input';
import { Menu, MenuItem, MenuSeparator } from '../../../shared/ui/menu/menu';
import { AccountService } from '../../profile/account.service';
import { CreatedInvite, Invite, InviteStatus, UserAccount } from '../admin.models';
import { AdminService } from '../admin.service';
import { DeleteUserDialog } from '../delete-user-dialog/delete-user-dialog';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { LocalDatePipe } from '../../../core/i18n/local-date.pipe';
import { I18nService } from '../../../core/i18n/i18n.service';
import { TranslationKey } from '../../../core/i18n/translation.types';

type Tab = 'members' | 'invites';

/** `/admin/users` — registered users and invites. Changes require the admin's own 2FA. */
@Component({
    selector: 'app-users',
    imports: [
        ReactiveFormsModule,
        RouterLink,
        CdkMenuTrigger,
        Badge,
        Button,
        Field,
        Input,
        Menu,
        MenuItem,
        MenuSeparator,
        TranslatePipe,
        LocalDatePipe,
    ],
    templateUrl: './users.html',
})
export class Users {
    private readonly admin = inject(AdminService);
    private readonly account = inject(AccountService);
    private readonly auth = inject(AuthService);
    private readonly confirm = inject(ConfirmService);
    private readonly dialog = inject(Dialog);
    private readonly i18n = inject(I18nService);

    protected readonly Pending = InviteStatus.Pending;

    readonly tab = signal<Tab>('members');
    readonly expiryOptions = [1, 3, 7, 14, 30];

    readonly users = rxResource({ stream: () => this.admin.users() });
    readonly invites = rxResource({ stream: () => this.admin.invites() });
    private readonly profile = rxResource({ stream: () => this.account.profile() });

    readonly me = computed(() => this.auth.username());
    /** The API refuses every change without the admin's 2FA; mirror that in the UI. */
    readonly canManage = computed(
        () => this.profile.hasValue() && this.profile.value().twoFactorEnabled,
    );
    readonly profileLoaded = computed(() => this.profile.hasValue());

    readonly busyId = signal<string | null>(null);
    readonly actionError = signal<string | null>(null);

    // ---------- Invite form ----------

    readonly inviteForm = inject(FormBuilder).nonNullable.group({
        email: ['', [Validators.required, Validators.email, Validators.maxLength(200)]],
        expiresInDays: [7],
    });
    readonly inviting = signal(false);
    /** Result of the last create/resend: the only moment the link is available. */
    readonly lastInvite = signal<(CreatedInvite & { email: string }) | null>(null);
    readonly copied = signal(false);

    readonly pendingInvites = computed(() =>
        this.invites.hasValue()
            ? this.invites.value().filter((i) => i.status === InviteStatus.Pending && !i.isExpired)
                  .length
            : 0,
    );

    sendInvite(): void {
        if (this.inviteForm.invalid) {
            this.inviteForm.markAllAsTouched();
            return;
        }
        const body = this.inviteForm.getRawValue();
        this.inviting.set(true);
        this.actionError.set(null);
        this.admin.createInvite(body).subscribe({
            next: (created) => {
                this.lastInvite.set({ ...created, email: body.email });
                this.inviteForm.reset({ email: '', expiresInDays: 7 });
                this.inviting.set(false);
                this.invites.reload();
            },
            error: (err: HttpErrorResponse) => {
                this.actionError.set(this.describe(err));
                this.inviting.set(false);
            },
        });
    }

    resend(invite: Invite): void {
        this.run(invite.id, this.admin.resendInvite(invite.id), (created) => {
            this.lastInvite.set({ ...created, email: invite.email ?? '' });
            this.invites.reload();
        });
    }

    revoke(invite: Invite): void {
        this.confirmThen(
            {
                title: this.i18n.t('admin.revokeTitle'),
                message: this.i18n.t('admin.revokeMessage', { email: invite.email ?? '' }),
                confirmLabel: this.i18n.t('admin.revoke'),
                danger: true,
            },
            invite.id,
            () => this.admin.revokeInvite(invite.id),
            () => this.invites.reload(),
        );
    }

    copyLink(): void {
        const link = this.lastInvite()?.link;
        if (!link) return;
        navigator.clipboard.writeText(link).then(() => {
            this.copied.set(true);
            setTimeout(() => this.copied.set(false), 2000);
        });
    }

    inviteStatus(invite: Invite): { label: TranslationKey; tone: BadgeTone } {
        if (invite.status === InviteStatus.Accepted)
            return { label: 'admin.inviteStatus.accepted', tone: 'success' };
        if (invite.status === InviteStatus.Revoked)
            return { label: 'admin.inviteStatus.revoked', tone: 'neutral' };
        return invite.isExpired
            ? { label: 'admin.inviteStatus.expired', tone: 'warning' }
            : { label: 'admin.inviteStatus.pending', tone: 'accent' };
    }

    // ---------- Members ----------

    isAdmin(user: UserAccount): boolean {
        return user.roles.includes('Admin');
    }

    toggleAdmin(user: UserAccount): void {
        const makeAdmin = !this.isAdmin(user);
        this.confirmThen(
            makeAdmin
                ? {
                      title: this.i18n.t('admin.makeAdminTitle', { name: user.username }),
                      message: this.i18n.t('admin.makeAdminMessage'),
                      confirmLabel: this.i18n.t('admin.makeAdmin'),
                  }
                : {
                      title: this.i18n.t('admin.removeAdminTitle', { name: user.username }),
                      message: this.i18n.t('admin.removeAdminMessage'),
                      confirmLabel: this.i18n.t('admin.removeAdmin'),
                      danger: true,
                  },
            user.id,
            () => (makeAdmin ? this.admin.grantAdmin(user.id) : this.admin.revokeAdmin(user.id)),
            () => this.users.reload(),
        );
    }

    toggleLock(user: UserAccount): void {
        const lock = !user.isLockedOut;
        this.confirmThen(
            lock
                ? {
                      title: this.i18n.t('admin.lockTitle', { name: user.username }),
                      message: this.i18n.t('admin.lockMessage'),
                      confirmLabel: this.i18n.t('admin.lock'),
                      danger: true,
                  }
                : {
                      title: this.i18n.t('admin.unlockTitle', { name: user.username }),
                      message: this.i18n.t('admin.unlockMessage'),
                      confirmLabel: this.i18n.t('admin.unlock'),
                  },
            user.id,
            () => this.admin.setLocked(user.id, lock),
            () => this.users.reload(),
        );
    }

    remove(user: UserAccount): void {
        this.actionError.set(null);
        this.dialog
            .open<boolean, UserAccount>(DeleteUserDialog, {
                data: user,
                width: '28rem',
                maxWidth: 'calc(100vw - 2rem)',
            })
            .closed.pipe(filter(Boolean))
            .subscribe(() => this.users.reload());
    }

    // ---------- helpers ----------

    private confirmThen<T>(
        options: Parameters<ConfirmService['confirm']>[0],
        id: string,
        action: () => Observable<T>,
        done: (result: T) => void,
    ): void {
        this.actionError.set(null);
        this.confirm
            .confirm(options)
            .pipe(
                filter(Boolean),
                switchMap(() => {
                    this.busyId.set(id);
                    return action();
                }),
            )
            .subscribe({
                next: (result) => {
                    this.busyId.set(null);
                    done(result);
                },
                error: (err: HttpErrorResponse) => {
                    this.busyId.set(null);
                    this.actionError.set(this.describe(err));
                },
            });
    }

    private run<T>(id: string, action: Observable<T>, done: (result: T) => void): void {
        this.actionError.set(null);
        this.busyId.set(id);
        action.subscribe({
            next: (result) => {
                this.busyId.set(null);
                done(result);
            },
            error: (err: HttpErrorResponse) => {
                this.busyId.set(null);
                this.actionError.set(this.describe(err));
            },
        });
    }

    private describe(err: HttpErrorResponse): string {
        const fieldErrors = err.error?.errors as Record<string, string[]> | undefined;
        if (fieldErrors) return Object.values(fieldErrors).flat().join(' ');
        if (err.status === 429) return this.i18n.t('errors.tooManyAttempts');
        return err.error?.detail ?? this.i18n.t('errors.generic');
    }
}
