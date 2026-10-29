import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule, ValidatorFn, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Observable, map } from 'rxjs';
import { Button } from '../../../shared/ui/button/button';
import { Field } from '../../../shared/ui/field/field';
import { Input } from '../../../shared/ui/input/input';
import { SolicitationService } from '../solicitation.service';
import { SolicitationStatus } from '../solicitation.models';
import { statusOptions } from '../solicitation-status';

// Same rules as backend WebApi/Validation/SolicitationValidators.cs.
const text = (max: number): [string, ValidatorFn[]] => [
    '',
    [Validators.required, Validators.maxLength(max)],
];

/** Create (`/solicitations/new`) and edit (`/solicitations/:id/edit`) page. */
@Component({
    selector: 'app-solicitation-form',
    imports: [ReactiveFormsModule, RouterLink, Button, Field, Input],
    templateUrl: './solicitation-form.html',
})
export class SolicitationForm implements OnInit {
    private readonly fb = inject(FormBuilder);
    private readonly service = inject(SolicitationService);
    private readonly router = inject(Router);

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
        }),
    });

    ngOnInit(): void {
        const id = this.id();
        if (!id) return;

        this.loading.set(true);
        this.service.get(id).subscribe({
            next: (s) => {
                this.form.patchValue(s);
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
        if (err.status === 404) return ['This solicitation no longer exists.'];
        return [err.error?.detail ?? 'Something went wrong. Please try again.'];
    }
}
