import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
    const auth = inject(AuthService);
    const token = auth.token;

    // Don't attach the token to the auth endpoints themselves.
    const isAuthCall = req.url.includes('/auth/');

    const authReq = token && !isAuthCall
        ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
        : req;

    return next(authReq).pipe(
        catchError((err: HttpErrorResponse) => {
            if (err.status === 401 && !isAuthCall) {
                return auth.refresh().pipe(
                    switchMap(t =>
                        next(req.clone({ setHeaders: { Authorization: `Bearer ${t.token}` } }))
                    ),
                    catchError(refreshErr => {
                        auth.logout();
                        return throwError(() => refreshErr);
                    }),
                );
            }
            return throwError(() => err);
        }),
    );
};
