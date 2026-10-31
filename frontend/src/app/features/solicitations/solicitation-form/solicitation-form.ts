import { Component, DestroyRef, OnInit, computed, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule, ValidatorFn, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Observable, map, merge } from 'rxjs';
import { PageTitle } from '../../../core/title/page-title';
import { Button } from '../../../shared/ui/button/button';
import { Field } from '../../../shared/ui/field/field';
import { Input } from '../../../shared/ui/input/input';
import { Segment, Segmented } from '../../../shared/ui/segmented/segmented';
import { AddressSearch } from '../address-search/address-search';
import { SolicitationService } from '../solicitation.service';
import { PlaceAddress, SolicitationStatus } from '../solicitation.models';
import { EU_COUNTRY_CODES } from '../places.service';
import { statusOptions } from '../solicitation-status';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { I18nService } from '../../../core/i18n/i18n.service';
import { TranslationKey } from '../../../core/i18n/translation.types';

// Same rules as backend WebApi/Validation/SolicitationValidators.cs.
const text = (max: number): [string, ValidatorFn[]] => [
    '',
    [Validators.required, Validators.maxLength(max)],
];

/** Create (`/solicitations/new`) and edit (`/solicitations/:id/edit`) page. */
@Component({
    selector: 'app-solicitation-form',
    imports: [
        ReactiveFormsModule,
        RouterLink,
        Button,
        Field,
        Input,
        Segmented,
        Segment,
        AddressSearch,
        TranslatePipe,
    ],
    templateUrl: './solicitation-form.html',
})
export class SolicitationForm implements OnInit {
    private readonly fb = inject(FormBuilder);
    private readonly service = inject(SolicitationService);
    private readonly router = inject(Router);
    private readonly pageTitle = inject(PageTitle);
    private readonly i18n = inject(I18nService);

    /** Route param, bound via withComponentInputBinding(). Absent on create. */
    readonly id = input<string>();

    readonly statusOptions = statusOptions;
    readonly loading = signal(false);
    readonly loadError = signal(false);
    readonly saving = signal(false);
    readonly errors = signal<string[]>([]);

    /** Where Back/Cancel go: the detail page when editing, the list when creating. */
    readonly backLink = computed(() => {
        const id = this.id();
        return id ? ['/solicitations', id] : ['/solicitations'];
    });

    readonly form = this.fb.nonNullable.group({
        jobName: text(200),
        // New solicitations always start as Draft (domain rule); only editable on edit.
        status: [SolicitationStatus.Draft],
        website: this.fb.nonNullable.group({ name: text(100), link: text(500) }),
        contact: this.fb.nonNullable.group({
            name: text(100),
            email: ['', [Validators.required, Validators.email, Validators.maxLength(200)]],
            phoneNumber: text(50),
        }),
        location: this.fb.nonNullable.group({
            street: text(200),
            streetNumber: text(20),
            zipCode: text(20),
            city: text(100),
            country: text(100),
            // On: the server verifies the address with Google (EU) → shown on the map.
            // Off: free text (e.g. outside the EU), never on the map.
            validated: [true],
            // The picked Google place. Cleared when the address is edited by hand; the server then
            // looks up the typed address instead.
            placeId: this.fb.control<string | null>(null),
        }),
    });

    /** Set when the picked place can't be verified (outside the EU). */
    readonly placeWarning = signal<TranslationKey | null>(null);

    constructor() {
        const l = this.form.controls.location.controls;
        merge(
            l.street.valueChanges,
            l.streetNumber.valueChanges,
            l.zipCode.valueChanges,
            l.city.valueChanges,
            l.country.valueChanges,
        )
            .pipe(takeUntilDestroyed(inject(DestroyRef)))
            .subscribe(() => {
                l.placeId.setValue(null);
                this.placeWarning.set(null);
            });
    }

    /** Copies the picked place into the address fields (without clearing its place id). */
    applyPlace(place: PlaceAddress): void {
        const location = this.form.controls.location;
        location.patchValue(
            {
                street: place.street,
                streetNumber: place.streetNumber,
                zipCode: place.zipCode,
                city: place.city,
                country: place.country,
                placeId: place.placeId,
            },
            { emitEvent: false },
        );
        location.markAllAsTouched(); // surface a missing house number right away
        this.placeWarning.set(
            EU_COUNTRY_CODES.has(place.countryCode) ? null : 'solicitations.form.outsideEu',
        );
    }

    setValidated(validated: boolean): void {
        this.form.controls.location.controls.validated.setValue(validated);
    }

    ngOnInit(): void {
        const id = this.id();
        if (!id) return;

        this.loading.set(true);
        this.service.get(id).subscribe({
            next: (s) => {
                // no events: keeps the loaded place id (field edits would otherwise clear it)
                this.form.patchValue(s, { emitEvent: false });
                this.pageTitle.setKey('titles.editNamed', { name: s.jobName });
                this.loading.set(false);
            },
            error: () => {
                this.loadError.set(true);
                this.loading.set(false);
            },
        });
    }

    submit(): void {
        if (this.form.invalid) {
            this.form.markAllAsTouched();
            return;
        }

        const { status, ...body } = this.form.getRawValue();
        const id = this.id();
        // Emits the id of the saved solicitation.
        const request: Observable<string> = id
            ? this.service.update(id, { ...body, status }).pipe(map(() => id))
            : this.service.create(body).pipe(map((created) => created.id));

        this.saving.set(true);
        this.errors.set([]);
        request.subscribe({
            // Land on the detail page, where meetings can be added next.
            next: (savedId) => this.router.navigate(['/solicitations', savedId]),
            error: (err: HttpErrorResponse) => {
                this.errors.set(this.describe(err));
                this.saving.set(false);
            },
        });
    }

    private describe(err: HttpErrorResponse): string[] {
        // 400 from the ValidationFilter: ProblemDetails with an `errors` map of field → messages.
        const fieldErrors = err.error?.errors as Record<string, string[]> | undefined;
        if (err.status === 400 && fieldErrors) return Object.values(fieldErrors).flat();
        if (err.status === 404) return [this.i18n.t('solicitations.gone')];
        return [err.error?.detail ?? this.i18n.t('errors.generic')];
    }
}
