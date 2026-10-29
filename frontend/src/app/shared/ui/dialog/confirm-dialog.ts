import { Component, Injectable, inject } from '@angular/core';
import { DIALOG_DATA, Dialog, DialogRef } from '@angular/cdk/dialog';
import { Observable, map } from 'rxjs';
import { Button } from '../button/button';

export interface ConfirmOptions {
    title: string;
    message: string;
    confirmLabel?: string;
    danger?: boolean;
}

/**
 * Confirmation dialog body. Behaviour (focus trap, Esc/backdrop to close,
 * aria-modal) comes from the CDK `Dialog`; open it through `ConfirmService`.
 */
@Component({
    selector: 'app-confirm-dialog',
    imports: [Button],
    host: {
        class: 'flex flex-col gap-4 rounded-lg border border-border bg-surface p-5 shadow-overlay',
    },
    template: `
        <div>
            <h2 class="text-foreground text-base font-semibold">{{ data.title }}</h2>
            <p class="text-muted mt-1.5 text-sm">{{ data.message }}</p>
        </div>
        <div class="flex justify-end gap-2">
            <button appButton variant="secondary" (click)="ref.close(false)">Cancel</button>
            <button
                appButton
                [variant]="data.danger ? 'danger' : 'primary'"
                (click)="ref.close(true)"
            >
                {{ data.confirmLabel ?? 'Confirm' }}
            </button>
        </div>
    `,
})
export class ConfirmDialog {
    protected readonly data = inject<ConfirmOptions>(DIALOG_DATA);
    protected readonly ref = inject<DialogRef<boolean>>(DialogRef);
}

@Injectable({ providedIn: 'root' })
export class ConfirmService {
    private readonly dialog = inject(Dialog);

    /** Emits once: true when confirmed, false when cancelled or dismissed. */
    confirm(options: ConfirmOptions): Observable<boolean> {
        return this.dialog
            .open<boolean>(ConfirmDialog, {
                data: options,
                width: '26rem',
                maxWidth: 'calc(100vw - 2rem)',
            })
            .closed.pipe(map((result) => result === true));
    }
}
