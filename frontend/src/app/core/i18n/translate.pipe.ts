import { Pipe, PipeTransform, inject } from '@angular/core';
import { I18nService } from './i18n.service';
import { TranslationKey, TranslationParams } from './translation.types';

/**
 * `{{ 'solicitations.list.title' | t }}` / `{{ 'meetings.count' | t: { count: n } }}`.
 * Impure so it re-renders when the language changes (a lookup, so cheap).
 */
@Pipe({ name: 't', pure: false })
export class TranslatePipe implements PipeTransform {
    private readonly i18n = inject(I18nService);

    transform(key: TranslationKey, params?: TranslationParams): string {
        return this.i18n.t(key, params);
    }
}
