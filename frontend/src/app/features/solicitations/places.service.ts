import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, shareReplay } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PlaceAddress, PlaceSuggestion } from './solicitation.models';

/** Mirrors backend WebApi/Api/PlacesEndpoints MapsConfig. */
export interface MapsConfig {
    browserApiKey: string | null;
    mapId: string;
}

/** EU member states: only these addresses can be verified (same list as backend EuCountries). */
export const EU_COUNTRY_CODES = new Set([
    'AT',
    'BE',
    'BG',
    'HR',
    'CY',
    'CZ',
    'DK',
    'EE',
    'FI',
    'FR',
    'DE',
    'GR',
    'HU',
    'IE',
    'IT',
    'LV',
    'LT',
    'LU',
    'MT',
    'NL',
    'PL',
    'PT',
    'RO',
    'SK',
    'SI',
    'ES',
    'SE',
]);

/**
 * Google Places, proxied by the API so the server key stays secret. Autocomplete calls and the
 * details call for the picked suggestion share a session token, which Google bills as one session.
 */
@Injectable({ providedIn: 'root' })
export class PlacesService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.apiUrl}/places`;

    private config$?: Observable<MapsConfig>;

    // GET /api/places/autocomplete?text=&sessionToken=
    autocomplete(text: string, sessionToken: string): Observable<PlaceSuggestion[]> {
        const params = new HttpParams().set('text', text).set('sessionToken', sessionToken);
        return this.http.get<PlaceSuggestion[]>(`${this.baseUrl}/autocomplete`, { params });
    }

    // GET /api/places/{placeId}?sessionToken= → the address to fill the form with
    get(placeId: string, sessionToken: string): Observable<PlaceAddress> {
        const params = new HttpParams().set('sessionToken', sessionToken);
        return this.http.get<PlaceAddress>(`${this.baseUrl}/${encodeURIComponent(placeId)}`, {
            params,
        });
    }

    // GET /api/maps/config → browser key + map id (fetched once per app load)
    mapsConfig(): Observable<MapsConfig> {
        this.config$ ??= this.http
            .get<MapsConfig>(`${environment.apiUrl}/maps/config`)
            .pipe(shareReplay({ bufferSize: 1, refCount: false }));
        return this.config$;
    }
}
