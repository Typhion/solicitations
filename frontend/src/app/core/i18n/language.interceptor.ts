import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { environment } from '../../../environments/environment';
import { I18nService } from './i18n.service';

/**
 * Tells the API which language to answer in (error messages, address suggestions). Only the
 * app's own API: other hosts (e.g. Google) never see it.
 */
export const languageInterceptor: HttpInterceptorFn = (req, next) => {
    if (!req.url.startsWith(environment.apiUrl)) return next(req);
    return next(req.clone({ setHeaders: { 'Accept-Language': inject(I18nService).language() } }));
};
