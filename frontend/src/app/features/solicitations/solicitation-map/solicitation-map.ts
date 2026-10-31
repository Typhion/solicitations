import { Component, computed, inject, signal, viewChild } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { Button } from '../../../shared/ui/button/button';
import { MapComponent, MapPin } from '../../../shared/ui/map/map';
import { dotColor } from '../../calendar/calendar-colors';
import { SolicitationService } from '../solicitation.service';
import { StatusBadge } from '../status-badge/status-badge';
import { SolicitationsNav } from '../solicitations-nav/solicitations-nav';
import { groupByAddress } from './map-groups';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { I18nService } from '../../../core/i18n/i18n.service';

/** `/solicitations/map` — every solicitation with a verified company address, on a map. */
@Component({
    selector: 'app-solicitation-map',
    imports: [RouterLink, Button, MapComponent, StatusBadge, SolicitationsNav, TranslatePipe],
    templateUrl: './solicitation-map.html',
})
export class SolicitationMap {
    private readonly service = inject(SolicitationService);
    private readonly i18n = inject(I18nService);
    private readonly map = viewChild(MapComponent);

    protected readonly dotColor = dotColor;

    protected readonly solicitations = rxResource({ stream: () => this.service.listAll() });

    private readonly grouped = computed(() =>
        groupByAddress(this.solicitations.hasValue() ? this.solicitations.value() : []),
    );
    protected readonly groups = computed(() => this.grouped().groups);
    protected readonly unverified = computed(() => this.grouped().unverified);

    protected readonly pins = computed<MapPin[]>(() =>
        this.groups().map((g) => {
            const first = g.solicitations[0];
            const count = g.solicitations.length;
            return {
                id: g.key,
                latitude: g.latitude,
                longitude: g.longitude,
                colorClass: dotColor(first.id),
                count,
                label:
                    count === 1
                        ? `${first.website.name} · ${first.jobName}`
                        : `${first.website.name} · ${this.i18n.t('solicitations.map.pinCount', { count })}`,
            };
        }),
    );

    /** Key of the selected address group. */
    protected readonly selected = signal<string | null>(null);

    protected onPinClick(key: string): void {
        this.selected.set(key);
        document
            .getElementById(`group-${key}`)
            ?.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
    }

    protected onListClick(key: string): void {
        this.selected.set(key);
        this.map()?.focus(key);
    }
}
