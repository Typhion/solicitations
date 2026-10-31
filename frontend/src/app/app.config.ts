import {
    ApplicationConfig,
    provideBrowserGlobalErrorListeners,
    provideAppInitializer,
    inject,
} from '@angular/core';
import { TitleStrategy, provideRouter, withComponentInputBinding } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { registerLocaleData } from '@angular/common';
import localeNl from '@angular/common/locales/nl';
import { firstValueFrom } from 'rxjs';

import { routes } from './app.routes';
import { authInterceptor } from './core/auth/auth.interceptor';
import { AuthService } from './core/auth/auth.service';
import { AppTitleStrategy } from './core/title/page-title';
import { languageInterceptor } from './core/i18n/language.interceptor';
import { LanguagePreference } from './core/i18n/language-preference';

// Dutch day/month names for LocalDatePipe ('en' is built in).
registerLocaleData(localeNl);

export const appConfig: ApplicationConfig = {
    providers: [
        provideBrowserGlobalErrorListeners(),
        provideRouter(routes, withComponentInputBinding()),
        { provide: TitleStrategy, useClass: AppTitleStrategy }, // route `title` → "Page · Solicitations"
        provideHttpClient(withInterceptors([languageInterceptor, authInterceptor])),
        // On startup, try to restore the session from the httpOnly refresh cookie.
        // No cookie (204) or an invalid one (401) -> swallowed and the user stays logged out.
        // A restored session applies the account's saved language before the first render.
        provideAppInitializer(() => {
            const auth = inject(AuthService);
            const languagePreference = inject(LanguagePreference);
            return firstValueFrom(auth.refresh())
                .then(() => languagePreference.signedIn())
                .catch(() => undefined);
        }),
    ],
};
