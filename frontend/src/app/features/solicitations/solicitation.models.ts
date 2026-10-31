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
    Unanswered = 10,
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
    OfferDiscussion = 10,
}

export interface Location {
    country: string;
    city: string;
    zipCode: string;
    street: string;
    streetNumber: string;
    /**
     * Request: verify this address with Google (EU only); false = free text, not on the map.
     * Response: the address is verified (has a place id and coordinates).
     */
    validated: boolean;
    /** Google place id of the picked suggestion (request) / the verified place (response). */
    placeId: string | null;
    /** Set by the server for verified addresses; ignored in requests. */
    latitude?: number | null;
    longitude?: number | null;
}

/** Mirrors backend Application/Places/PlaceSuggestion. */
export interface PlaceSuggestion {
    placeId: string;
    text: string;
    mainText: string;
    secondaryText: string;
}

/** Mirrors backend Application/Places/PlaceAddress. */
export interface PlaceAddress {
    placeId: string;
    formattedAddress: string;
    country: string;
    /** ISO 3166-1 alpha-2, upper case. */
    countryCode: string;
    city: string;
    zipCode: string;
    street: string;
    /** Empty when Google lists the place without a house number. */
    streetNumber: string;
    latitude: number;
    longitude: number;
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

export type UpdateMeetingRequest = AddMeetingRequest;
