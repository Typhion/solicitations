import {
    Component,
    DestroyRef,
    ElementRef,
    ViewEncapsulation,
    afterNextRender,
    effect,
    inject,
    input,
    output,
    signal,
    untracked,
    viewChild,
} from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ThemeService } from '../../../core/theme/theme.service';
import { MapsConfig, PlacesService } from '../../../features/solicitations/places.service';

/** One marker. `colorClass` is a Tailwind bg-* class; `count` > 1 is drawn inside the dot. */
export interface MapPin {
    id: string;
    latitude: number;
    longitude: number;
    label: string;
    colorClass: string;
    count?: number;
}

const BELGIUM: google.maps.LatLngLiteral = { lat: 50.5, lng: 4.47 };
const PIN_ZOOM = 15;
const CALLBACK = '__solicitationsMapsReady';

let loading: Promise<MapsConfig> | null = null;

/**
 * Loads the Maps JavaScript API once, with the browser key from `GET /api/maps/config` (so keys
 * live in the server's config, not in the build). Rejects when no key is configured or the
 * script can't load. `gm_authFailure` is Google's hook for a key it refuses (wrong referrer,
 * API not enabled, billing off).
 */
function loadGoogleMaps(places: PlacesService, onAuthFailure: () => void): Promise<MapsConfig> {
    const w = window as unknown as Record<string, unknown>;
    w['gm_authFailure'] = onAuthFailure;
    loading ??= firstValueFrom(places.mapsConfig())
        .then(
            (config) =>
                new Promise<MapsConfig>((resolve, reject) => {
                    if (!config.browserApiKey) {
                        reject(new Error('No Google Maps browser key configured.'));
                        return;
                    }
                    if (typeof google !== 'undefined' && 'importLibrary' in (google.maps ?? {})) {
                        resolve(config);
                        return;
                    }
                    w[CALLBACK] = () => resolve(config);
                    const script = document.createElement('script');
                    const params = new URLSearchParams({
                        key: config.browserApiKey,
                        v: 'weekly',
                        loading: 'async',
                        callback: CALLBACK,
                    });
                    script.src = `https://maps.googleapis.com/maps/api/js?${params}`;
                    script.async = true;
                    script.onerror = () => reject(new Error('Google Maps failed to load.'));
                    document.head.appendChild(script);
                }),
        )
        .catch((err) => {
            loading = null; // allow a retry on the next map
            throw err;
        });
    return loading;
}

/**
 * Google map with clickable dot markers; fits itself to the pins and follows the app theme.
 * Size it from the outside, e.g. `<app-map class="h-96" … />`.
 */
@Component({
    selector: 'app-map',
    host: { class: 'relative block overflow-hidden' },
    template: `
        <div #map class="bg-surface-hover absolute inset-0"></div>
        @if (failed(); as message) {
            <div class="bg-surface/80 absolute inset-0 flex items-center justify-center p-6">
                <p class="text-danger text-center text-sm">{{ message }}</p>
            </div>
        }
    `,
    styleUrl: './map.css',
    // The pin styles target marker elements created outside the template.
    encapsulation: ViewEncapsulation.None,
})
export class MapComponent {
    private readonly theme = inject(ThemeService).theme;
    private readonly places = inject(PlacesService);

    readonly pins = input.required<MapPin[]>();
    readonly selectedId = input<string | null>(null);
    /** Off for small preview maps: no controls, and the page keeps scrolling over the map. */
    readonly interactive = input(true);

    readonly pinClick = output<string>();

    private readonly container = viewChild.required<ElementRef<HTMLElement>>('map');
    protected readonly failed = signal<string | null>(null);

    private config: MapsConfig | null = null;
    private map: google.maps.Map | null = null;
    private markers = new Map<string, google.maps.marker.AdvancedMarkerElement>();
    /** Bumps on every (re)created map so the effects re-render onto it. */
    private readonly mapVersion = signal(0);

    constructor() {
        afterNextRender(async () => {
            try {
                this.config = await loadGoogleMaps(this.places, () =>
                    this.failed.set(
                        'Google rejected the Maps key. Check its referrer restrictions and that the Maps JavaScript API is enabled.',
                    ),
                );
                await this.createMap();
            } catch (err) {
                console.error('Map failed to load', err);
                this.failed.set('The map could not be loaded.');
            }
        });

        effect(() => {
            const pins = this.pins();
            if (this.mapVersion()) untracked(() => this.render(pins));
        });

        effect(() => {
            const selected = this.selectedId();
            if (!this.mapVersion()) return;
            for (const [id, marker] of this.markers) {
                (marker.content as HTMLElement).setAttribute(
                    'aria-pressed',
                    String(id === selected),
                );
            }
        });

        // A Google map's colour scheme is fixed at creation, so a theme switch recreates it.
        effect(() => {
            this.theme();
            if (untracked(this.mapVersion)) untracked(() => void this.createMap());
        });

        inject(DestroyRef).onDestroy(() => this.clearMarkers());
    }

    /** Centres the map on a pin (e.g. when it's picked from a list next to the map). */
    focus(id: string): void {
        const position = this.markers.get(id)?.position;
        if (!position || !this.map) return;
        this.map.panTo(position);
        this.map.setZoom(PIN_ZOOM);
    }

    private async createMap(): Promise<void> {
        const config = this.config;
        if (!config) return;
        // ColorScheme lives in the *core* library, not *maps*.
        const [{ Map }, { ColorScheme }] = await Promise.all([
            google.maps.importLibrary('maps') as Promise<google.maps.MapsLibrary>,
            google.maps.importLibrary('core') as Promise<google.maps.CoreLibrary>,
            google.maps.importLibrary('marker'),
        ]);

        const interactive = this.interactive();
        this.clearMarkers();
        this.map = new Map(this.container().nativeElement, {
            mapId: config.mapId,
            center: BELGIUM,
            zoom: 7,
            colorScheme: this.theme() === 'dark' ? ColorScheme.DARK : ColorScheme.LIGHT,
            clickableIcons: false,
            disableDefaultUI: true,
            zoomControl: interactive,
            fullscreenControl: interactive,
            // 'cooperative': scrolling the page over the map scrolls the page; ctrl/two fingers zoom.
            gestureHandling: interactive ? 'cooperative' : 'none',
            keyboardShortcuts: interactive,
        });
        this.mapVersion.update((v) => v + 1);
    }

    private render(pins: MapPin[]): void {
        const map = this.map;
        if (!map) return;

        this.clearMarkers();
        for (const pin of pins) {
            const marker = new google.maps.marker.AdvancedMarkerElement({
                map,
                position: { lat: pin.latitude, lng: pin.longitude },
                content: this.pinElement(pin),
                title: pin.label,
            });
            this.markers.set(pin.id, marker);
        }

        if (pins.length === 1) {
            map.setCenter({ lat: pins[0].latitude, lng: pins[0].longitude });
            map.setZoom(PIN_ZOOM);
        } else if (pins.length > 1) {
            const bounds = new google.maps.LatLngBounds();
            pins.forEach((p) => bounds.extend({ lat: p.latitude, lng: p.longitude }));
            map.fitBounds(bounds, 64);
            // fitBounds zooms all the way in on close pins; cap it once it settles.
            google.maps.event.addListenerOnce(map, 'idle', () => {
                if ((map.getZoom() ?? 0) > PIN_ZOOM) map.setZoom(PIN_ZOOM);
            });
        }
    }

    private clearMarkers(): void {
        this.markers.forEach((m) => (m.map = null));
        this.markers.clear();
    }

    private pinElement(pin: MapPin): HTMLElement {
        const el = document.createElement('button');
        el.type = 'button';
        el.className = `map-pin ${pin.colorClass}`;
        el.textContent = pin.count && pin.count > 1 ? String(pin.count) : '';
        el.setAttribute('aria-label', pin.label);
        el.setAttribute('aria-pressed', String(pin.id === this.selectedId()));
        el.addEventListener('click', (e) => {
            e.stopPropagation();
            this.pinClick.emit(pin.id);
        });
        return el;
    }
}
