import {
    ApplicationConfig,
    provideBrowserGlobalErrorListeners,
    provideAppInitializer,
    inject,
} from '@angular/core';
import { TitleStrategy, provideRouter, withComponentInputBinding } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

import { routes } from './app.routes';
import { authInterceptor } from './core/auth/auth.interceptor';
import { AuthService } from './core/auth/auth.service';
import { AppTitleStrategy } from './core/title/page-title';

export const appConfig: ApplicationConfig = {
    providers: [
        provideBrowserGlobalErrorListeners(),
        provideRouter(routes, withComponentInputBinding()),
        { provide: TitleStrategy, useClass: AppTitleStrategy }, // route `title` → "Page · Solicitations"
        provideHttpClient(withInterceptors([authInterceptor])),
        // On startup, try to restore the session from the httpOnly refresh cookie.
        // No cookie (204) or an invalid one (401) -> swallowed and the user stays logged out.
        provideAppInitializer(() => {
            const auth = inject(AuthService);
            return firstValueFrom(auth.refresh()).catch(() => undefined);
        }),
    ],
};
