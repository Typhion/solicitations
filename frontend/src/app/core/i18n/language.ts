/** Languages the app is available in. Mirrors backend Application/Common/Languages.cs. */
export type Language = 'en' | 'nl';

export const DEFAULT_LANGUAGE: Language = 'en';

/** Each name is written in its own language, so it can be found whatever is active. */
export const LANGUAGES: readonly { code: Language; label: string }[] = [
    { code: 'en', label: 'English' },
    { code: 'nl', label: 'Nederlands' },
];

export function isLanguage(value: unknown): value is Language {
    return LANGUAGES.some((l) => l.code === value);
}

/** First of the browser's preferred languages we support (`nl-BE` → `nl`), else English. */
export function detectBrowserLanguage(
    preferred: readonly string[] = navigator.languages?.length
        ? navigator.languages
        : [navigator.language],
): Language {
    for (const tag of preferred) {
        const code = tag?.toLowerCase().split('-')[0];
        if (isLanguage(code)) return code;
    }
    return DEFAULT_LANGUAGE;
}
