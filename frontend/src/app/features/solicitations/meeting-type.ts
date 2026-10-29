import { MeetingType } from './solicitation.models';

// Mirrors backend Domain/Meeting/MeetingType.cs.
export const meetingTypeLabels: Record<MeetingType, string> = {
    [MeetingType.RecruiterScreen]: 'Recruiter screen',
    [MeetingType.PhoneScreen]: 'Phone screen',
    [MeetingType.TechnicalInterview]: 'Technical interview',
    [MeetingType.CodingChallenge]: 'Coding challenge',
    [MeetingType.BehavioralInterview]: 'Behavioral interview',
    [MeetingType.HiringManager]: 'Hiring manager',
    [MeetingType.PanelInterview]: 'Panel interview',
    [MeetingType.OnSite]: 'On-site',
    [MeetingType.FinalRound]: 'Final round',
    [MeetingType.OfferDiscussion]: 'Offer discussion',
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
