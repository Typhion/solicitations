import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
    const auth = inject(AuthService);
    const router = inject(Router);
    const token = auth.token;

    // Don't attach the token to the auth endpoints themselves.
    const isAuthCall = req.url.includes('/auth/');

    const authReq =
        token && !isAuthCall
            ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
            : req;

    return next(authReq).pipe(
        catchError((err: HttpErrorResponse) => {
            // 401 = token expired, or the API rejected it because the account changed
            // (locked, deleted, password/role/2FA change). A refresh fixes the legitimate cases.
            if (err.status === 401 && !isAuthCall) {
                return auth.refresh().pipe(
                    switchMap((t) =>
                        next(req.clone({ setHeaders: { Authorization: `Bearer ${t.token}` } })),
                    ),
                    catchError((refreshErr) => {
                        // Refresh refused too (e.g. account locked): the session is over.
                        // Leave the page so nothing stale stays editable.
                        auth.logout();
                        router.navigate(['/login'], { queryParams: { sessionEnded: 1 } });
                        return throwError(() => refreshErr);
                    }),
                );
            }
            return throwError(() => err);
        }),
    );
};
