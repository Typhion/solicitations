import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { Segment, Segmented } from '../../../shared/ui/segmented/segmented';

/** List / Calendar / Map switch shown in the header of each solicitations overview page. */
@Component({
    selector: 'app-solicitations-nav',
    imports: [RouterLink, RouterLinkActive, Segmented, Segment, TranslatePipe],
    template: `
        <nav appSegmented [attr.aria-label]="'solicitations.nav.label' | t">
            <a
                appSegment
                routerLink="/solicitations"
                routerLinkActive
                [routerLinkActiveOptions]="{ exact: true }"
                #list="routerLinkActive"
                [active]="list.isActive"
            >
                <svg
                    xmlns="http://www.w3.org/2000/svg"
                    width="14"
                    height="14"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    stroke-width="2"
                    stroke-linecap="round"
                    stroke-linejoin="round"
                >
                    <path d="M8 6h13M8 12h13M8 18h13M3 6h.01M3 12h.01M3 18h.01" />
                </svg>
                {{ 'solicitations.nav.list' | t }}
            </a>
            <a
                appSegment
                routerLink="/solicitations/calendar"
                routerLinkActive
                #calendar="routerLinkActive"
                [active]="calendar.isActive"
            >
                <svg
                    xmlns="http://www.w3.org/2000/svg"
                    width="14"
                    height="14"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    stroke-width="2"
                    stroke-linecap="round"
                    stroke-linejoin="round"
                >
                    <rect x="3" y="4" width="18" height="18" rx="2" />
                    <path d="M16 2v4M8 2v4M3 10h18" />
                </svg>
                {{ 'solicitations.nav.calendar' | t }}
            </a>
            <a
                appSegment
                routerLink="/solicitations/map"
                routerLinkActive
                #map="routerLinkActive"
                [active]="map.isActive"
            >
                <svg
                    xmlns="http://www.w3.org/2000/svg"
                    width="14"
                    height="14"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    stroke-width="2"
                    stroke-linecap="round"
                    stroke-linejoin="round"
                >
                    <path d="M20 10c0 6-8 12-8 12s-8-6-8-12a8 8 0 0 1 16 0Z" />
                    <circle cx="12" cy="10" r="3" />
                </svg>
                {{ 'solicitations.nav.map' | t }}
            </a>
        </nav>
    `,
})
export class SolicitationsNav {}
