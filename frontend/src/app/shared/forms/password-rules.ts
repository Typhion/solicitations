import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';
import { TranslationKey } from '../../core/i18n/translation.types';

// Mirrors the Identity password policy in backend Infrastructure/Security/Security.Startup.cs.
export const PASSWORD_MIN_LENGTH = 12;
/** Translation key of the policy hint (shown under the field and as the validation message). */
export const PASSWORD_HINT = 'validation.passwordHint' satisfies TranslationKey;

export const passwordStrength: ValidatorFn = (c: AbstractControl): ValidationErrors | null => {
    const v = c.value as string;
    if (!v) return null; // `required` reports empty
    const ok =
        v.length >= PASSWORD_MIN_LENGTH &&
        /[a-z]/.test(v) &&
        /[A-Z]/.test(v) &&
        /\d/.test(v) &&
        /[^a-zA-Z0-9]/.test(v);
    return ok ? null : { passwordStrength: PASSWORD_HINT };
};

/** Confirm field must equal the sibling control named `other`. */
export const matches =
    (other: string): ValidatorFn =>
    (c: AbstractControl): ValidationErrors | null =>
        c.parent && c.value !== c.parent.get(other)?.value
            ? { mismatch: 'validation.passwordMismatch' satisfies TranslationKey }
            : null;
