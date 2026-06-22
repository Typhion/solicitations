namespace Domain.Solicitation;

public enum SolicitationStatus
{
    // Pre-submission
    Draft = 1, // created but not sent yet

    // Active pipeline
    Applied = 2, // application submitted
    Acknowledged = 3, // employer confirmed receipt
    Screening = 4, // phone/recruiter screen in progress
    Interviewing = 5, // one or more interviews underway
    OfferReceived = 6, // offer extended

    // Terminal outcomes
    Accepted = 7, // offer accepted (success)
    Rejected = 8, // employer declined
    Withdrawn = 9, // candidate pulled out
    Unanswered = 10 // no response after a reasonable wait / ghosted
}