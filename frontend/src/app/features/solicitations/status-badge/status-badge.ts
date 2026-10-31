import { Component, computed, input } from '@angular/core';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { Badge } from '../../../shared/ui/badge/badge';
import { SolicitationStatus } from '../solicitation.models';
import { statusDisplay } from '../solicitation-status';

@Component({
    selector: 'app-status-badge',
    imports: [Badge, TranslatePipe],
    template: `<span appBadge [tone]="display()?.tone ?? 'neutral'">
        @if (display(); as d) {
            {{ d.label | t }}
        } @else {
            {{ 'status.unknown' | t: { status: status() } }}
        }
    </span>`,
})
export class StatusBadge {
    readonly status = input.required<SolicitationStatus>();

    protected readonly display = computed(() => statusDisplay[this.status()]);
}
