import { Pipe, PipeTransform, inject } from '@angular/core';
import { formatDate } from '@angular/common';
import { I18nService } from './i18n.service';

/**
 * `DatePipe` in the active language (`{{ d | localDate: 'EEEE d MMMM' }}`). The built-in pipe
 * reads LOCALE_ID once at startup, so it can't follow a language switch.
 */
@Pipe({ name: 'localDate', pure: false })
export class LocalDatePipe implements PipeTransform {
    private readonly i18n = inject(I18nService);

    transform(
        value: Date | string | number | null | undefined,
        format = 'mediumDate',
    ): string | null {
        return value == null || value === '' ? null : formatDate(value, format, this.i18n.locale());
    }
}
