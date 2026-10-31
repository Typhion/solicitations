import { Subject, of, throwError } from 'rxjs';
import { PlaceSuggestion } from '../solicitation.models';
import { DEBOUNCE_MS, suggestionsFor } from './address-search';

const suggestion: PlaceSuggestion = {
    placeId: 'ChIJ-ghent',
    text: 'Veldstraat 1, 9000 Gent, België',
    mainText: 'Veldstraat 1',
    secondaryText: '9000 Gent, België',
};

describe('suggestionsFor', () => {
    beforeEach(() => vi.useFakeTimers());
    afterEach(() => vi.useRealTimers());

    function setup(fetch = vi.fn((_: string) => of([suggestion]))) {
        const text$ = new Subject<string>();
        const results: PlaceSuggestion[][] = [];
        suggestionsFor(text$, fetch).subscribe((r) => results.push(r));
        return { text$, fetch, results };
    }

    it('debounces typing into a single request for the final text', () => {
        const { text$, fetch, results } = setup();

        text$.next('Vel');
        text$.next('Veld');
        text$.next('Veldstraat ');
        vi.advanceTimersByTime(DEBOUNCE_MS);

        expect(fetch).toHaveBeenCalledTimes(1);
        expect(fetch).toHaveBeenCalledWith('Veldstraat');
        expect(results).toEqual([[suggestion]]);
    });

    it('clears suggestions without a request for fewer than 3 characters', () => {
        const { text$, fetch, results } = setup();

        text$.next('Ve');
        vi.advanceTimersByTime(DEBOUNCE_MS);

        expect(fetch).not.toHaveBeenCalled();
        expect(results).toEqual([[]]);
    });

    it('drops the response for text the user has moved past', () => {
        const slow = new Subject<PlaceSuggestion[]>();
        const fetch = vi.fn((t: string) => (t === 'Veld' ? slow : of([suggestion])));
        const { text$, results } = setup(fetch);

        text$.next('Veld');
        vi.advanceTimersByTime(DEBOUNCE_MS);
        text$.next('Veldstraat');
        vi.advanceTimersByTime(DEBOUNCE_MS);
        slow.next([]); // stale response arrives late

        expect(results).toEqual([[suggestion]]);
    });

    it('degrades to no suggestions when the request fails', () => {
        const { text$, results } = setup(vi.fn(() => throwError(() => new Error('429'))));

        text$.next('Veldstraat');
        vi.advanceTimersByTime(DEBOUNCE_MS);

        expect(results).toEqual([[]]);
    });
});
