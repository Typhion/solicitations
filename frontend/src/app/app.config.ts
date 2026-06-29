import { ApplicationConfig, provideBrowserGlobalErrorListeners, provideAppInitializer, inject } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

import { routes } from './app.routes';
import { authInterceptor } from './core/auth/auth.interceptor';
import { AuthService } from './core/auth/auth.service';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor])),
    // On startup, try to restore the session from the httpOnly refresh cookie.
    // No valid cookie -> the 401 is swallowed and the user stays logged out.
    provideAppInitializer(() => {
      const auth = inject(AuthService);
      return firstValueFrom(auth.refresh()).catch(() => undefined);
    }),
  ]
};
