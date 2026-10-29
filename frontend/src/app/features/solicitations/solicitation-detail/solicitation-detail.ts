import { Component, computed, inject, input, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { DatePipe } from '@angular/common';
import { CdkMenuTrigger } from '@angular/cdk/menu';
import { Dialog } from '@angular/cdk/dialog';
import { Router, RouterLink } from '@angular/router';
import { filter, switchMap } from 'rxjs';
import { Button } from '../../../shared/ui/button/button';
import { ConfirmService } from '../../../shared/ui/dialog/confirm-dialog';
import { Menu, MenuItem, MenuSeparator } from '../../../shared/ui/menu/menu';
import { MeetingDialog, MeetingDialogData } from '../meeting-dialog/meeting-dialog';
import { meetingTypeLabels, parseUtc } from '../meeting-type';
import { Meeting } from '../solicitation.models';
import { SolicitationService } from '../solicitation.service';
import { StatusBadge } from '../status-badge/status-badge';

/** `/solicitations/:id` — solicitation summary plus its meetings (add/edit/delete). */
@Component({
    selector: 'app-solicitation-detail',
    imports: [
        Button,
        StatusBadge,
        RouterLink,
        DatePipe,
        CdkMenuTrigger,
        Menu,
        MenuItem,
        MenuSeparator,
    ],
    templateUrl: './solicitation-detail.html',
})
export class SolicitationDetail {
    private readonly service = inject(SolicitationService);
    private readonly confirm = inject(ConfirmService);
    private readonly dialog = inject(Dialog);
    private readonly router = inject(Router);

    /** Route param, bound via withComponentInputBinding(). */
    readonly id = input.required<string>();

    readonly typeLabels = meetingTypeLabels;
    readonly busyMeetingId = signal<string | null>(null);
    readonly actionError = signal<string | null>(null);

    readonly solicitation = rxResource({
        params: () => ({ id: this.id() }),
        stream: ({ params }) => this.service.get(params.id),
    });

    // Chronological, with a parsed date and whether it's already happened.
    readonly meetings = computed(() => {
        if (!this.solicitation.hasValue()) return [];
        const now = Date.now();
        return this.solicitation
            .value()
            .meetings.map((m) => {
                const at = parseUtc(m.scheduledAtUtc);
                return { ...m, at, past: at.getTime() < now };
            })
            .sort((a, b) => a.at.getTime() - b.at.getTime());
    });

    readonly upcomingCount = computed(() => this.meetings().filter((m) => !m.past).length);

    openMeeting(meeting?: Meeting): void {
        this.actionError.set(null);
        this.dialog
            .open<boolean, MeetingDialogData>(MeetingDialog, {
                data: { solicitationId: this.id(), meeting },
                width: '32rem',
                maxWidth: 'calc(100vw - 2rem)',
            })
            .closed.pipe(filter(Boolean))
            .subscribe(() => this.solicitation.reload());
    }

    removeMeeting(meeting: Meeting): void {
        this.actionError.set(null);
        this.confirm
            .confirm({
                title: 'Delete meeting?',
                message: `The ${meetingTypeLabels[meeting.type].toLowerCase()} will be permanently deleted.`,
                confirmLabel: 'Delete',
                danger: true,
            })
            .pipe(
                filter(Boolean),
                switchMap(() => {
                    this.busyMeetingId.set(meeting.id);
                    return this.service.removeMeeting(this.id(), meeting.id);
                }),
            )
            .subscribe({
                next: () => {
                    this.busyMeetingId.set(null);
                    this.solicitation.reload();
                },
                error: () => {
                    this.busyMeetingId.set(null);
                    this.actionError.set('Could not delete the meeting. Please try again.');
                },
            });
    }

    removeSolicitation(): void {
        if (!this.solicitation.hasValue()) return;
        const s = this.solicitation.value();
        this.actionError.set(null);
        this.confirm
            .confirm({
                title: 'Delete solicitation?',
                message: `"${s.jobName}" and its meetings will be permanently deleted.`,
                confirmLabel: 'Delete',
                danger: true,
            })
            .pipe(
                filter(Boolean),
                switchMap(() => this.service.delete(s.id)),
            )
            .subscribe({
                next: () => this.router.navigate(['/solicitations']),
                error: () => this.actionError.set(`Could not delete "${s.jobName}".`),
            });
    }

    // The API only checks the link is non-empty, so it may lack a scheme.
    href(link: string): string {
        return /^https?:\/\//i.test(link) ? link : `https://${link}`;
    }
}
