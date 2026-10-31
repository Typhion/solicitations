import { Injectable, computed, effect, signal } from '@angular/core';
import { DEFAULT_LANGUAGE, Language, detectBrowserLanguage, isLanguage } from './language';
import { Plural, TranslationKey, TranslationParams, Translations } from './translation.types';
import { en } from './translations/en';
import { nl } from './translations/nl';

const STORAGE_KEY = 'language';
const dictionaries: Record<Language, Translations> = { en, nl };

/**
 * The active UI language. Starts from this device's last choice, else the browser's language,
 * else English; the account's saved choice is applied on sign-in (LanguagePreference).
 * Everything that renders text reads `language()`, so switching is instant.
 */
@Injectable({ providedIn: 'root' })
export class I18nService {
    readonly language = signal<Language>(this.initialLanguage());
    /** Angular locale id for dates (`registerLocaleData` in app.config). */
    readonly locale = computed(() => this.language());

    constructor() {
        effect(() => (document.documentElement.lang = this.language()));
    }

    set(language: Language): void {
        this.language.set(language);
        localStorage.setItem(STORAGE_KEY, language);
    }

    t(key: TranslationKey, params?: TranslationParams): string {
        const language = this.language();
        const entry =
            lookup(dictionaries[language], key) ?? lookup(dictionaries[DEFAULT_LANGUAGE], key);
        if (entry === undefined) return key;

        const text =
            typeof entry === 'string'
                ? entry
                : (entry[
                      new Intl.PluralRules(language).select(
                          Number(params?.['count'] ?? 0),
                      ) as keyof Plural
                  ] ?? entry.other);
        return params ? interpolate(text, params) : text;
    }

    private initialLanguage(): Language {
        const stored = localStorage.getItem(STORAGE_KEY);
        return isLanguage(stored) ? stored : detectBrowserLanguage();
    }
}

function lookup(dictionary: Translations, key: string): string | Plural | undefined {
    let node: unknown = dictionary;
    for (const part of key.split('.')) {
        if (typeof node !== 'object' || node === null) return undefined;
        node = (node as Record<string, unknown>)[part];
    }
    return typeof node === 'string' || isPlural(node) ? node : undefined;
}

function isPlural(node: unknown): node is Plural {
    return typeof node === 'object' && node !== null && 'one' in node && 'other' in node;
}

/** `'Hello {name}'` + `{ name: 'Ann' }` → `'Hello Ann'`. Unknown placeholders are left as-is. */
function interpolate(text: string, params: TranslationParams): string {
    return text.replace(/\{(\w+)\}/g, (match, name: string) =>
        name in params ? String(params[name] ?? '') : match,
    );
}
