import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { AuthService } from '../auth/auth.service';
import { I18nService } from './i18n.service';
import { Language, isLanguage } from './language';

/**
 * Keeps the active language and the account's saved choice in sync:
 * - choosing while signed in saves it to the account;
 * - choosing while signed out (e.g. on the login page) is saved once the user signs in;
 * - otherwise signing in applies the account's choice. No choice → the browser default stays.
 */
@Injectable({ providedIn: 'root' })
export class LanguagePreference {
    private readonly http = inject(HttpClient);
    private readonly auth = inject(AuthService);
    private readonly i18n = inject(I18nService);

    /** Chosen while signed out; pushed to the account on the next sign-in. */
    private pendingChoice = false;

    choose(language: Language): void {
        this.i18n.set(language);
        if (this.auth.isLoggedIn()) this.save(language);
        else this.pendingChoice = true;
    }

    /** Call after a session starts (startup refresh or interactive login). */
    signedIn(): void {
        const saved = this.auth.locale();
        if (this.pendingChoice) {
            this.pendingChoice = false;
            if (saved !== this.i18n.language()) this.save(this.i18n.language());
        } else if (isLanguage(saved) && saved !== this.i18n.language()) {
            this.i18n.set(saved);
        }
    }

    // Best effort: the choice already applies locally, so a failed save isn't worth an error.
    private save(language: Language): void {
        this.http
            .put(`${environment.apiUrl}/account/language`, { language })
            .subscribe({ error: () => undefined });
    }
}
