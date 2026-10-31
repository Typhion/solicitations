import { BadgeTone } from '../../shared/ui/badge/badge';
import { TranslationKey } from '../../core/i18n/translation.types';
import { SolicitationStatus } from './solicitation.models';

// Mirrors the grouping in backend Domain/Solicitation/SolicitationStatus.cs.
// `label` is a translation key (render with the `t` pipe).
export const statusDisplay: Record<SolicitationStatus, { label: TranslationKey; tone: BadgeTone }> =
    {
        // Pre-submission
        [SolicitationStatus.Draft]: { label: 'status.draft', tone: 'neutral' },
        // Active pipeline
        [SolicitationStatus.Applied]: { label: 'status.applied', tone: 'accent' },
        [SolicitationStatus.Acknowledged]: { label: 'status.acknowledged', tone: 'accent' },
        [SolicitationStatus.Screening]: { label: 'status.screening', tone: 'warning' },
        [SolicitationStatus.Interviewing]: { label: 'status.interviewing', tone: 'warning' },
        [SolicitationStatus.OfferReceived]: { label: 'status.offerReceived', tone: 'success' },
        // Terminal outcomes
        [SolicitationStatus.Accepted]: { label: 'status.accepted', tone: 'success' },
        [SolicitationStatus.Rejected]: { label: 'status.rejected', tone: 'danger' },
        [SolicitationStatus.Withdrawn]: { label: 'status.withdrawn', tone: 'neutral' },
        [SolicitationStatus.Unanswered]: { label: 'status.unanswered', tone: 'neutral' },
    };

// Ordered options for status pickers.
// Numeric enums also have reverse (name) keys, so keep only the numbers.
export const statusOptions = Object.values(SolicitationStatus)
    .filter((value): value is SolicitationStatus => typeof value === 'number')
    .map((value) => ({ value, label: statusDisplay[value].label }));
