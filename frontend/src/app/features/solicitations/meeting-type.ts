import { TranslationKey } from '../../core/i18n/translation.types';
import { MeetingType } from './solicitation.models';

// Mirrors backend Domain/Meeting/MeetingType.cs. Values are translation keys (`t` pipe).
export const meetingTypeLabels: Record<MeetingType, TranslationKey> = {
    [MeetingType.RecruiterScreen]: 'meetingType.recruiterScreen',
    [MeetingType.PhoneScreen]: 'meetingType.phoneScreen',
    [MeetingType.TechnicalInterview]: 'meetingType.technicalInterview',
    [MeetingType.CodingChallenge]: 'meetingType.codingChallenge',
    [MeetingType.BehavioralInterview]: 'meetingType.behavioralInterview',
    [MeetingType.HiringManager]: 'meetingType.hiringManager',
    [MeetingType.PanelInterview]: 'meetingType.panelInterview',
    [MeetingType.OnSite]: 'meetingType.onSite',
    [MeetingType.FinalRound]: 'meetingType.finalRound',
    [MeetingType.OfferDiscussion]: 'meetingType.offerDiscussion',
};

// Numeric enums also have reverse (name) keys, so keep only the numbers.
export const meetingTypeOptions = Object.values(MeetingType)
    .filter((value): value is MeetingType => typeof value === 'number')
    .map((value) => ({ value, label: meetingTypeLabels[value] }));

/**
 * Parses the API's `scheduledAtUtc`. .NET may omit the `Z` for UTC values, in
 * which case JS would read the string as local time — so add it when missing.
 */
export function parseUtc(value: string): Date {
    return new Date(/[zZ]|[+-]\d\d:\d\d$/.test(value) ? value : `${value}Z`);
}

/** Date → `yyyy-MM-ddTHH:mm` in local time, the format `<input type="datetime-local">` uses. */
export function toLocalInputValue(date: Date): string {
    const pad = (n: number) => String(n).padStart(2, '0');
    return (
        `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}` +
        `T${pad(date.getHours())}:${pad(date.getMinutes())}`
    );
}

/** Date → `HH:mm` in local time, the format `<input type="time">` uses. */
export function toLocalTimeValue(date: Date): string {
    return toLocalInputValue(date).slice(11);
}

/** Same local day as `date`, at the `HH:mm` given. */
export function withTime(date: Date, time: string): Date {
    const [hours, minutes] = time.split(':').map(Number);
    const result = new Date(date);
    result.setHours(hours, minutes, 0, 0);
    return result;
}

export const CUSTOM_DURATION = 'custom';
/** Minutes, or `'custom'` when the end time is entered directly. */
export type MeetingDuration = number | typeof CUSTOM_DURATION;

export const DEFAULT_DURATION = 60;

export const meetingDurationOptions: { value: MeetingDuration; label: TranslationKey }[] = [
    { value: 30, label: 'meetings.duration.min30' },
    { value: 45, label: 'meetings.duration.min45' },
    { value: 60, label: 'meetings.duration.hour1' },
    { value: 90, label: 'meetings.duration.hour1min30' },
    { value: 120, label: 'meetings.duration.hour2' },
    { value: CUSTOM_DURATION, label: 'meetings.duration.custom' },
];

/** The preset matching a duration in minutes, else `'custom'`. */
export function durationPreset(minutes: number): MeetingDuration {
    return meetingDurationOptions.some((o) => o.value === minutes) ? minutes : CUSTOM_DURATION;
}
