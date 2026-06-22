namespace Domain.Meeting;

public enum MeetingType
{
    RecruiterScreen = 1, // intro/recruiter call
    PhoneScreen = 2, // initial screening call
    TechnicalInterview = 3, // technical/skills interview
    CodingChallenge = 4, // live coding / take-home review
    BehavioralInterview = 5, // behavioral/culture-fit interview
    HiringManager = 6, // meeting with the hiring manager
    PanelInterview = 7, // multiple interviewers at once
    OnSite = 8, // on-site / full-day round
    FinalRound = 9, // final-stage interview
    OfferDiscussion = 10 // offer / compensation conversation
}