import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import {
    AbstractControl,
    FormBuilder,
    ReactiveFormsModule,
    ValidationErrors,
    Validators,
} from '@angular/forms';
import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { Observable } from 'rxjs';
import { Button } from '../../../shared/ui/button/button';
import { Field } from '../../../shared/ui/field/field';
import { I18nService } from '../../../core/i18n/i18n.service';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { TranslationKey } from '../../../core/i18n/translation.types';
import { Input } from '../../../shared/ui/input/input';
import { Meeting, MeetingType } from '../solicitation.models';
import { SolicitationService } from '../solicitation.service';
import {
    CUSTOM_DURATION,
    DEFAULT_DURATION,
    MeetingDuration,
    durationPreset,
    meetingDurationOptions,
    meetingTypeOptions,
    parseUtc,
    toLocalInputValue,
    toLocalTimeValue,
    withTime,
} from '../meeting-type';

const MINUTE = 60_000;

/** Custom end time (same day as the start) must come after the start. */
function endAfterStart(control: AbstractControl<string>): ValidationErrors | null {
    const start = control.parent?.get('scheduledAt')?.value as string | undefined;
    if (!start || !control.value) return null;
    return withTime(new Date(start), control.value) > new Date(start)
        ? null
        : { endAfterStart: 'validation.endAfterStart' satisfies TranslationKey };
}

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
    imports: [ReactiveFormsModule, Button, Field, Input, TranslatePipe],
    host: {
        class: 'flex flex-col rounded-lg border border-border bg-surface shadow-overlay',
    },
    templateUrl: './meeting-dialog.html',
})
export class MeetingDialog {
    private readonly fb = inject(FormBuilder);
    private readonly service = inject(SolicitationService);
    private readonly i18n = inject(I18nService);
    protected readonly data = inject<MeetingDialogData>(DIALOG_DATA);
    protected readonly ref = inject<DialogRef<boolean>>(DialogRef);

    readonly typeOptions = meetingTypeOptions;
    readonly durationOptions = meetingDurationOptions;
    readonly custom = CUSTOM_DURATION;
    readonly saving = signal(false);
    readonly errors = signal<string[]>([]);

    private readonly initial = this.initialTimes();

    readonly form = this.fb.nonNullable.group({
        scheduledAt: [toLocalInputValue(this.initial.start), Validators.required],
        duration: [this.initial.duration],
        // Only editable for a custom duration; for presets it just mirrors the computed end.
        endTime: [
            { value: this.initial.endTime, disabled: this.initial.duration !== CUSTOM_DURATION },
            [Validators.required, endAfterStart],
        ],
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

        const { scheduledAt, duration, endTime } = this.form.controls;
        duration.valueChanges.pipe(takeUntilDestroyed()).subscribe((value) => {
            // the end time already reflects the previous preset, so Custom starts from there
            if (value === CUSTOM_DURATION) endTime.enable();
            else {
                endTime.disable();
                this.syncEndTime();
            }
        });
        scheduledAt.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => {
            if (duration.value === CUSTOM_DURATION) endTime.updateValueAndValidity();
            else this.syncEndTime();
        });
    }

    submit(): void {
        if (this.form.invalid) {
            this.form.markAllAsTouched();
            return;
        }

        const { scheduledAt, duration, endTime, type, isOnline, onlineTool } =
            this.form.getRawValue();
        const start = new Date(scheduledAt);
        const end = this.endOf(start, duration, endTime);
        const body = {
            // datetime-local is local time; the API stores UTC.
            scheduledAtUtc: start.toISOString(),
            endsAtUtc: end.toISOString(),
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
                        : [err.error?.detail ?? this.i18n.t('errors.generic')],
                );
                this.saving.set(false);
            },
        });
    }

    /** Presets add minutes (so they may cross midnight); custom is a time on the start's day. */
    private endOf(start: Date, duration: MeetingDuration, endTime: string): Date {
        return duration === CUSTOM_DURATION
            ? withTime(start, endTime)
            : new Date(start.getTime() + duration * MINUTE);
    }

    private syncEndTime(): void {
        const { scheduledAt, duration, endTime } = this.form.controls;
        if (!scheduledAt.value || duration.value === CUSTOM_DURATION) return;
        const end = this.endOf(new Date(scheduledAt.value), duration.value, endTime.value);
        endTime.setValue(toLocalTimeValue(end));
    }

    private initialTimes(): { start: Date; duration: MeetingDuration; endTime: string } {
        const meeting = this.data.meeting;
        if (meeting) {
            const start = parseUtc(meeting.scheduledAtUtc);
            const end = parseUtc(meeting.endsAtUtc);
            const minutes = Math.round((end.getTime() - start.getTime()) / MINUTE);
            return { start, duration: durationPreset(minutes), endTime: toLocalTimeValue(end) };
        }
        // Default for new meetings: tomorrow 10:00–11:00.
        const start = new Date();
        start.setDate(start.getDate() + 1);
        start.setHours(10, 0, 0, 0);
        const end = new Date(start.getTime() + DEFAULT_DURATION * MINUTE);
        return { start, duration: DEFAULT_DURATION, endTime: toLocalTimeValue(end) };
    }
}
