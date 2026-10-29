import { MeetingType, SolicitationStatus } from '../solicitations/solicitation.models';

export type CalendarView = 'month' | 'week' | 'agenda';

/** Mirrors backend Application/Meetings/CalendarMeetingDto. */
export interface CalendarMeetingDto {
    id: string;
    solicitationId: string;
    jobName: string;
    company: string;
    status: SolicitationStatus;
    scheduledAtUtc: string;
    type: MeetingType;
    isOnline: boolean;
    onlineTool: string | null;
    city: string;
}

/** A meeting with its start parsed to a local `Date`, ready for the views. */
export interface CalendarMeeting extends CalendarMeetingDto {
    at: Date;
}

/** Emitted when a meeting is clicked; the element anchors the details popover. */
export interface MeetingSelection {
    meeting: CalendarMeeting;
    origin: HTMLElement;
}
