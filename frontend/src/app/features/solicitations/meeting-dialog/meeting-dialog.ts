import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { Observable } from 'rxjs';
import { Button } from '../../../shared/ui/button/button';
import { Field } from '../../../shared/ui/field/field';
import { Input } from '../../../shared/ui/input/input';
import { Meeting, MeetingType } from '../solicitation.models';
import { SolicitationService } from '../solicitation.service';
import { meetingTypeOptions, parseUtc, toLocalInputValue } from '../meeting-type';

export interface MeetingDialogData {
    solicitationId: string;
    /** Present when editing; absent when adding. */
    meeting?: Meeting;
}

/**
 * Add / edit a meeting. Saves through the API itself so errors keep the dialog
 * open; closes with `true` once saved.
 */
@Component({
    selector: 'app-meeting-dialog',
    imports: [ReactiveFormsModule, Button, Field, Input],
    host: {
        class: 'flex flex-col rounded-lg border border-border bg-surface shadow-overlay',
    },
    templateUrl: './meeting-dialog.html',
})
export class MeetingDialog {
    private readonly fb = inject(FormBuilder);
    private readonly service = inject(SolicitationService);
    protected readonly data = inject<MeetingDialogData>(DIALOG_DATA);
    protected readonly ref = inject<DialogRef<boolean>>(DialogRef);

    readonly typeOptions = meetingTypeOptions;
    readonly saving = signal(false);
    readonly errors = signal<string[]>([]);

    readonly form = this.fb.nonNullable.group({
        scheduledAt: [this.initialScheduledAt(), Validators.required],
        type: [this.data.meeting?.type ?? MeetingType.RecruiterScreen],
        isOnline: [this.data.meeting?.isOnline ?? false],
        // Same rule as backend MeetingValidators: required (≤100) only when online.
        onlineTool: [
            { value: this.data.meeting?.onlineTool ?? '', disabled: !this.data.meeting?.isOnline },
            [Validators.required, Validators.maxLength(100)],
        ],
    });

    constructor() {
        // Disabled controls skip validation, so toggling enable/disable is the conditional rule.
        this.form.controls.isOnline.valueChanges.pipe(takeUntilDestroyed()).subscribe((online) => {
            const tool = this.form.controls.onlineTool;
            if (online) tool.enable();
            else tool.disable();
        });
    }

    submit(): void {
        if (this.form.invalid) {
            this.form.markAllAsTouched();
            return;
        }

        const { scheduledAt, type, isOnline, onlineTool } = this.form.getRawValue();
        const body = {
            // datetime-local is local time; the API stores UTC.
            scheduledAtUtc: new Date(scheduledAt).toISOString(),
            type,
            isOnline,
            onlineTool: isOnline ? onlineTool : null,
        };
        const { solicitationId, meeting } = this.data;
        const request: Observable<unknown> = meeting
            ? this.service.updateMeeting(solicitationId, meeting.id, body)
            : this.service.addMeeting(solicitationId, body);

        this.saving.set(true);
        this.errors.set([]);
        request.subscribe({
            next: () => this.ref.close(true),
            error: (err: HttpErrorResponse) => {
                const fieldErrors = err.error?.errors as Record<string, string[]> | undefined;
                this.errors.set(
                    err.status === 400 && fieldErrors
                        ? Object.values(fieldErrors).flat()
                        : [err.error?.detail ?? 'Something went wrong. Please try again.'],
                );
                this.saving.set(false);
            },
        });
    }

    private initialScheduledAt(): string {
        const meeting = this.data.meeting;
        if (meeting) return toLocalInputValue(parseUtc(meeting.scheduledAtUtc));
        // Default for new meetings: tomorrow at 10:00.
        const d = new Date();
        d.setDate(d.getDate() + 1);
        d.setHours(10, 0, 0, 0);
        return toLocalInputValue(d);
    }
}
