import type { en } from './translations/en';

/** A message that depends on a `count` param, picked with `Intl.PluralRules`. */
export interface Plural {
    one: string;
    other: string;
}

/** Same shape as the English source, with any text: what every other language must provide. */
type Shape<T> = {
    [K in keyof T]: T[K] extends string ? string : T[K] extends Plural ? Plural : Shape<T[K]>;
};
export type Translations = Shape<typeof en>;

/** Dotted path to every message, e.g. `'solicitations.list.title'`. */
type Paths<T, Prefix extends string = ''> = {
    [K in keyof T & string]: T[K] extends string | Plural
        ? `${Prefix}${K}`
        : Paths<T[K], `${Prefix}${K}.`>;
}[keyof T & string];
export type TranslationKey = Paths<typeof en>;

/** `null`/`undefined` render as empty text (e.g. a date pipe with no value). */
export type TranslationParams = Record<string, string | number | null | undefined>;
