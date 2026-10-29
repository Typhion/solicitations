import { Component, computed, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { Button } from '../../../shared/ui/button/button';
import { Segment, Segmented } from '../../../shared/ui/segmented/segmented';
import { SolicitationService } from '../solicitation.service';
import { StatusBadge } from '../status-badge/status-badge';

const PAGE_SIZE = 20;

@Component({
    selector: 'app-solicitation-list',
    imports: [Button, StatusBadge, RouterLink, Segmented, Segment],
    templateUrl: './solicitation-list.html',
})
export class SolicitationList {
    private readonly service = inject(SolicitationService);

    readonly page = signal(1);

    readonly solicitations = rxResource({
        params: () => ({ page: this.page() }),
        stream: ({ params }) => this.service.list(params.page, PAGE_SIZE),
    });

    // Range shown in the footer, e.g. "21–40 of 53".
    readonly range = computed(() => {
        // value() throws while the resource is in an error state, so check hasValue() first.
        if (!this.solicitations.hasValue()) return null;
        const r = this.solicitations.value();
        if (r.totalCount === 0) return null;
        const from = (r.page - 1) * r.pageSize + 1;
        return { from, to: from + r.items.length - 1, total: r.totalCount };
    });

    previous(): void {
        this.page.update((p) => Math.max(1, p - 1));
    }

    next(): void {
        this.page.update((p) => p + 1);
    }

    // The API only checks the link is non-empty, so it may lack a scheme.
    href(link: string): string {
        return /^https?:\/\//i.test(link) ? link : `https://${link}`;
    }
}
