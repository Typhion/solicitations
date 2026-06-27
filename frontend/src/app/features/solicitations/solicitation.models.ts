export enum SolicitationStatus {
    Draft = 1,
    Applied = 2,
    Acknowledged = 3,
    Screening = 4,
    Interviewing = 5,
    OfferReceived = 6,
    Accepted = 7,
    Rejected = 8,
    Withdrawn = 9,
    Unanswered = 10
}

export enum MeetingType {
    RecruiterScreen = 1,
    PhoneScreen = 2,
    TechnicalInterview = 3,
    CodingChallenge = 4,
    BehavioralInterview = 5,
    HiringManager = 6,
    PanelInterview = 7,
    OnSite = 8,
    FinalRound = 9,
    OfferDiscussion = 10
}

export interface Location {
    country: string;
    city: string;
    zipCode: string;
    street: string;
    streetNumber: string;
}

export interface Website {
    name: string;
    link: string;
}

export interface Contact {
    name: string;
    phoneNumber: string;
    email: string;
}

export interface Meeting {
    id: string;
    scheduledAtUtc: string;
    type: MeetingType;
    isOnline: boolean;
    onlineTool: string | null;
}

export interface Solicitation {
    id: string;
    jobName: string;
    status: SolicitationStatus;
    location: Location;
    website: Website;
    contact: Contact;
    meetings: Meeting[];
}

export interface CreateSolicitationRequest {
    jobName: string;
    location: Location;
    website: Website;
    contact: Contact;
}

export interface UpdateSolicitationRequest extends CreateSolicitationRequest {
    status: SolicitationStatus;
}

export interface AddMeetingRequest {
    scheduledAtUtc: string;
    type: MeetingType;
    isOnline: boolean;
    onlineTool: string | null;
}
