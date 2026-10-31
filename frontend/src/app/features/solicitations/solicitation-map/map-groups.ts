import { Solicitation } from '../solicitation.models';

/** Solicitations sharing one verified address (e.g. several jobs at the same company office). */
export interface AddressGroup {
    key: string;
    latitude: number;
    longitude: number;
    solicitations: Solicitation[];
}

/** Groups the verified solicitations by coordinates; unverified ones are only counted. */
export function groupByAddress(solicitations: Solicitation[]): {
    groups: AddressGroup[];
    unverified: number;
} {
    const groups = new Map<string, AddressGroup>();
    let unverified = 0;
    for (const s of solicitations) {
        const { latitude, longitude } = s.location;
        if (!s.location.validated || latitude == null || longitude == null) {
            unverified++;
            continue;
        }
        const key = `${latitude.toFixed(5)},${longitude.toFixed(5)}`;
        const group = groups.get(key) ?? { key, latitude, longitude, solicitations: [] };
        group.solicitations.push(s);
        groups.set(key, group);
    }
    return { groups: [...groups.values()], unverified };
}
