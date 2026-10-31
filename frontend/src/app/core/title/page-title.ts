import { Injectable, effect, inject, signal } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { RouterStateSnapshot, TitleStrategy } from '@angular/router';
import { I18nService } from '../i18n/i18n.service';
import { TranslationKey, TranslationParams } from '../i18n/translation.types';

/** A page title: a translation key (route titles) or literal text (e.g. a job name). */
export type PageTitleValue =
    | { key: TranslationKey; params?: TranslationParams }
    | { text: string }
    | null;

/**
 * Formats and sets the browser tab title: "Page · Solicitations", in the active language.
 * Re-applies when the language changes.
 */
@Injectable({ providedIn: 'root' })
export class PageTitle {
    private readonly title = inject(Title);
    private readonly i18n = inject(I18nService);
    private readonly current = signal<PageTitleValue>(null);

    constructor() {
        effect(() => {
            const page = this.current();
            const appName = this.i18n.t('app.name');
            const text = !page
                ? null
                : 'key' in page
                  ? this.i18n.t(page.key, page.params)
                  : page.text;
            this.title.setTitle(text && text !== appName ? `${text} · ${appName}` : appName);
        });
    }

    /** For titles only known after loading data (e.g. a solicitation's job name). */
    set(page?: string): void {
        this.current.set(page ? { text: page } : null);
    }

    /** A translated title, e.g. `setKey('titles.editNamed', { name })`. */
    setKey(key: TranslationKey, params?: TranslationParams): void {
        this.current.set({ key, params });
    }
}

/** Applies each route's `title` (a translation key) on navigation, in the same format. */
@Injectable()
export class AppTitleStrategy extends TitleStrategy {
    private readonly pageTitle = inject(PageTitle);

    override updateTitle(snapshot: RouterStateSnapshot): void {
        const key = this.buildTitle(snapshot);
        if (key) this.pageTitle.setKey(key as TranslationKey);
        else this.pageTitle.set();
    }
}
