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

/** Admin-only pages. Use after `authGuard`; non-admins go back to the app. */
export const adminGuard: CanActivateFn = () => {
    const auth = inject(AuthService);
    return auth.isAdmin() || inject(Router).createUrlTree(['/solicitations']);
};
