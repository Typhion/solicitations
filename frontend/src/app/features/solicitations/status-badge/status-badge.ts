import { Component, computed, input } from '@angular/core';
import { Badge } from '../../../shared/ui/badge/badge';
import { SolicitationStatus } from '../solicitation.models';
import { statusDisplay } from '../solicitation-status';

@Component({
    selector: 'app-status-badge',
    imports: [Badge],
    template: `<span appBadge [tone]="display().tone">{{ display().label }}</span>`,
})
export class StatusBadge {
    readonly status = input.required<SolicitationStatus>();

    protected readonly display = computed(
        () =>
            statusDisplay[this.status()] ?? {
                label: `Unknown (${this.status()})`,
                tone: 'neutral',
            },
    );
}
