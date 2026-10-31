import { Component } from '@angular/core';
import { CdkMenu, CdkMenuItem, CdkMenuItemRadio } from '@angular/cdk/menu';

/**
 * Dropdown menu — design-system reference component.
 *
 * Pattern for every base component: take the *behavior* from a headless CDK
 * directive via `hostDirectives`, and attach our *look* (Tailwind token
 * utilities) on the host. Consumers then write plain, semantic markup and get
 * keyboard navigation, focus management, ARIA roles and outside-click handling
 * for free.
 *
 * Usage:
 *   <button [cdkMenuTriggerFor]="menu">Account</button>
 *   <ng-template #menu>
 *     <div appMenu>
 *       <button appMenuItem (triggered)="edit()">Edit</button>
 *       <div appMenuSeparator></div>
 *       <button appMenuItem [disabled]="!canDelete()" (triggered)="remove()">
 *         Delete
 *       </button>
 *     </div>
 *   </ng-template>
 *
 * The trigger uses CDK's `cdkMenuTriggerFor` directly (import `CdkMenuTrigger`
 * in the host component); style the trigger like any other button.
 */
@Component({
    selector: 'div[appMenu]',
    // CdkMenu sets role="menu" and wires the FocusKeyManager (arrow keys, Home/End,
    // type-ahead, Esc-to-close).
    hostDirectives: [CdkMenu],
    host: {
        class: 'flex min-w-44 flex-col gap-0.5 rounded-md border border-border bg-surface p-1 shadow-overlay',
    },
    template: '<ng-content />',
})
export class Menu {}

/**
 * A single menu item. Renders on a native <button> for correct semantics.
 *
 * Re-exposes CDK's API under friendlier names:
 *   - `(triggered)`  — fired on click or Enter/Space (CDK also closes the menu).
 *   - `[disabled]`   — keeps the item focusable but inert (aria-disabled).
 */
@Component({
    selector: 'button[appMenuItem]',
    hostDirectives: [
        {
            directive: CdkMenuItem,
            inputs: ['cdkMenuItemDisabled: disabled'],
            outputs: ['cdkMenuItemTriggered: triggered'],
        },
    ],
    host: {
        type: 'button',
        // `focus:` (not focus-visible) because CDK moves focus programmatically
        // during keyboard navigation — we want the highlight to follow it.
        class: 'flex w-full cursor-pointer items-center gap-2.5 rounded px-2.5 py-1.5 text-left text-sm text-foreground transition-colors hover:bg-surface-hover focus:bg-surface-hover focus:outline-none aria-disabled:pointer-events-none aria-disabled:opacity-50',
    },
    template: '<ng-content />',
})
export class MenuItem {}

const itemClasses =
    'flex w-full cursor-pointer items-center gap-2.5 rounded px-2.5 py-1.5 text-left text-sm text-foreground transition-colors hover:bg-surface-hover focus:bg-surface-hover focus:outline-none aria-disabled:pointer-events-none aria-disabled:opacity-50';

/**
 * One choice of several (role="menuitemradio" + aria-checked), with a check mark on the
 * selected one. Use inside `appMenu`:
 *   <button appMenuItemRadio [checked]="lang() === 'en'" (triggered)="choose('en')">English</button>
 */
@Component({
    selector: 'button[appMenuItemRadio]',
    hostDirectives: [
        {
            directive: CdkMenuItemRadio,
            inputs: ['cdkMenuItemChecked: checked', 'cdkMenuItemDisabled: disabled'],
            outputs: ['cdkMenuItemTriggered: triggered'],
        },
    ],
    host: { type: 'button', class: itemClasses },
    template: `
        <svg
            class="text-accent invisible size-4 shrink-0 in-aria-checked:visible"
            xmlns="http://www.w3.org/2000/svg"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            stroke-width="2.5"
            stroke-linecap="round"
            stroke-linejoin="round"
            aria-hidden="true"
        >
            <path d="M20 6 9 17l-5-5" />
        </svg>
        <ng-content />
    `,
})
export class MenuItemRadio {}

/** Thin divider between groups of items. */
@Component({
    selector: 'div[appMenuSeparator]',
    host: { role: 'separator', class: 'my-1 h-px bg-border' },
    template: '',
})
export class MenuSeparator {}
