import { Component, inject, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
    Observable,
    Subject,
    catchError,
    debounceTime,
    distinctUntilChanged,
    map,
    of,
    switchMap,
} from 'rxjs';
import { Input } from '../../../shared/ui/input/input';
import { PlacesService } from '../places.service';
import { PlaceAddress, PlaceSuggestion } from '../solicitation.models';

export const MIN_QUERY_LENGTH = 3;
export const DEBOUNCE_MS = 300;

/**
 * Typed text → suggestions. Debounced; queries shorter than {@link MIN_QUERY_LENGTH} clear the
 * list without a request; `switchMap` drops responses for text the user has already moved past;
 * failures degrade to "no suggestions".
 */
export function suggestionsFor<T>(
    text$: Observable<string>,
    fetch: (text: string) => Observable<T[]>,
): Observable<T[]> {
    return text$.pipe(
        map((t) => t.trim()),
        debounceTime(DEBOUNCE_MS),
        distinctUntilChanged(),
        switchMap((t) =>
            t.length < MIN_QUERY_LENGTH ? of([]) : fetch(t).pipe(catchError(() => of([]))),
        ),
    );
}

let nextId = 0;

/**
 * Company / address search backed by Google Places (ARIA combobox). Picking a suggestion looks up
 * its address and emits it; the parent copies it into its form. The server re-verifies on save.
 */
@Component({
    selector: 'app-address-search',
    imports: [Input],
    host: { class: 'relative block' },
    template: `
        <input
            appInput
            type="search"
            role="combobox"
            autocomplete="off"
            placeholder="Search a company or address, e.g. Acme Gent"
            aria-autocomplete="list"
            [attr.aria-expanded]="open()"
            [attr.aria-controls]="listId"
            [attr.aria-activedescendant]="active() >= 0 ? listId + '-' + active() : null"
            [attr.aria-busy]="resolving()"
            [value]="text()"
            (input)="onInput($any($event.target).value)"
            (keydown)="onKeydown($event)"
            (blur)="close()"
        />
        @if (open()) {
            <div
                class="border-border bg-surface absolute inset-x-0 top-full z-20 mt-1 overflow-hidden rounded-md border shadow-lg"
            >
                <ul [id]="listId" role="listbox" class="max-h-72 overflow-auto py-1">
                    @for (s of suggestions(); track s.placeId) {
                        <li
                            [id]="listId + '-' + $index"
                            role="option"
                            [attr.aria-selected]="$index === active()"
                            class="cursor-pointer px-3 py-2 text-sm"
                            [class.bg-surface-hover]="$index === active()"
                            (mousedown)="$event.preventDefault(); pick(s)"
                            (mouseenter)="active.set($index)"
                        >
                            <span class="text-foreground">{{ s.mainText }}</span>
                            <span class="text-muted ml-1.5 text-xs">{{ s.secondaryText }}</span>
                        </li>
                    }
                </ul>
                <!-- Required by Google when Places results are shown without a Google map. -->
                <p class="border-border text-muted border-t px-3 py-1.5 text-right text-[11px]">
                    Powered by Google
                </p>
            </div>
        }
        @if (error()) {
            <p class="text-danger mt-1.5 text-xs">{{ error() }}</p>
        }
    `,
})
export class AddressSearch {
    private readonly places = inject(PlacesService);

    readonly picked = output<PlaceAddress>();

    protected readonly listId = `address-search-${nextId++}`;
    protected readonly text = signal('');
    protected readonly suggestions = signal<PlaceSuggestion[]>([]);
    protected readonly open = signal(false);
    protected readonly active = signal(-1);
    protected readonly resolving = signal(false);
    protected readonly error = signal<string | null>(null);

    private readonly input$ = new Subject<string>();

    /** Groups this search's autocomplete calls + the final details call into one Google session. */
    private sessionToken = crypto.randomUUID();

    constructor() {
        suggestionsFor(this.input$, (t) => this.places.autocomplete(t, this.sessionToken))
            .pipe(takeUntilDestroyed())
            .subscribe((list) => {
                this.suggestions.set(list);
                this.active.set(list.length ? 0 : -1);
                this.open.set(list.length > 0);
            });
    }

    protected onInput(value: string): void {
        this.text.set(value);
        this.error.set(null);
        this.input$.next(value);
    }

    protected onKeydown(e: KeyboardEvent): void {
        const count = this.suggestions().length;
        if (!this.open() || !count) return;
        if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
            e.preventDefault();
            const step = e.key === 'ArrowDown' ? 1 : -1;
            this.active.update((i) => (i + step + count) % count);
        } else if (e.key === 'Enter' && this.active() >= 0) {
            e.preventDefault(); // don't submit the form
            this.pick(this.suggestions()[this.active()]);
        } else if (e.key === 'Escape') {
            this.close();
        }
    }

    protected pick(s: PlaceSuggestion): void {
        this.text.set(s.text);
        this.close();
        this.resolving.set(true);
        this.places.get(s.placeId, this.sessionToken).subscribe({
            next: (address) => {
                this.resolving.set(false);
                this.picked.emit(address);
            },
            error: () => {
                this.resolving.set(false);
                this.error.set(
                    'That suggestion has no street address. Try another one, or enter it as free text.',
                );
            },
        });
        this.sessionToken = crypto.randomUUID(); // the session ends with the details call
    }

    protected close(): void {
        this.open.set(false);
        this.active.set(-1);
    }
}
