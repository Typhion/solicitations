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
    endsAtUtc: string;
    type: MeetingType;
    isOnline: boolean;
    onlineTool: string | null;
    city: string;
}

/** A meeting with its start and end parsed to local `Date`s, ready for the views. */
export interface CalendarMeeting extends CalendarMeetingDto {
    at: Date;
    end: Date;
}

/** Emitted when a meeting is clicked; the element anchors the details popover. */
export interface MeetingSelection {
    meeting: CalendarMeeting;
    origin: HTMLElement;
}
