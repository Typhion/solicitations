import { BadgeTone } from '../../shared/ui/badge/badge';
import { SolicitationStatus } from './solicitation.models';

// Mirrors the grouping in backend Domain/Solicitation/SolicitationStatus.cs.
export const statusDisplay: Record<SolicitationStatus, { label: string; tone: BadgeTone }> = {
    // Pre-submission
    [SolicitationStatus.Draft]: { label: 'Draft', tone: 'neutral' },
    // Active pipeline
    [SolicitationStatus.Applied]: { label: 'Applied', tone: 'accent' },
    [SolicitationStatus.Acknowledged]: { label: 'Acknowledged', tone: 'accent' },
    [SolicitationStatus.Screening]: { label: 'Screening', tone: 'warning' },
    [SolicitationStatus.Interviewing]: { label: 'Interviewing', tone: 'warning' },
    [SolicitationStatus.OfferReceived]: { label: 'Offer received', tone: 'success' },
    // Terminal outcomes
    [SolicitationStatus.Accepted]: { label: 'Accepted', tone: 'success' },
    [SolicitationStatus.Rejected]: { label: 'Rejected', tone: 'danger' },
    [SolicitationStatus.Withdrawn]: { label: 'Withdrawn', tone: 'neutral' },
    [SolicitationStatus.Unanswered]: { label: 'Unanswered', tone: 'neutral' },
};

// Ordered options for status pickers.
// Numeric enums also have reverse (name) keys, so keep only the numbers.
export const statusOptions = Object.values(SolicitationStatus)
    .filter((value): value is SolicitationStatus => typeof value === 'number')
    .map((value) => ({ value, label: statusDisplay[value].label }));
