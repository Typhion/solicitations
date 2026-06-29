import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = (_route, state) => {
    const auth = inject(AuthService);
    const router = inject(Router);

    if (auth.isLoggedIn()) return true;

    // Remember where the user was headed so login can send them back.
    return router.createUrlTree(['/login'], {
        queryParams: { returnUrl: state.url },
    });
};
