import { Solicitation, SolicitationStatus } from '../solicitation.models';
import { groupByAddress } from './map-groups';

const solicitation = (id: string, coords: [number, number] | null): Solicitation => ({
    id,
    jobName: `Job ${id}`,
    status: SolicitationStatus.Applied,
    location: {
        country: 'België',
        city: 'Gent',
        zipCode: '9000',
        street: 'Veldstraat',
        streetNumber: '1',
        validated: coords !== null,
        placeId: coords ? `place-${id}` : null,
        latitude: coords?.[0] ?? null,
        longitude: coords?.[1] ?? null,
    },
    website: { name: 'Acme', link: 'https://acme.example' },
    contact: { name: 'A', phoneNumber: '1', email: 'a@acme.example' },
    meetings: [],
});

describe('groupByAddress', () => {
    it('groups verified solicitations at the same address and counts unverified ones', () => {
        const { groups, unverified } = groupByAddress([
            solicitation('a', [51.0543, 3.7174]),
            solicitation('free', null),
            solicitation('b', [51.0543, 3.7174]),
            solicitation('c', [50.8503, 4.3517]),
        ]);

        expect(unverified).toBe(1);
        expect(groups.map((g) => g.solicitations.map((s) => s.id))).toEqual([['a', 'b'], ['c']]);
    });

    it('returns nothing to pin when no address is verified', () => {
        expect(groupByAddress([solicitation('x', null)])).toEqual({ groups: [], unverified: 1 });
    });
});
