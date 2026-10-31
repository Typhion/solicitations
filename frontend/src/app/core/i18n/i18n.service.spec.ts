import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { AuthService } from '../auth/auth.service';
import { I18nService } from './i18n.service';
import { detectBrowserLanguage } from './language';
import { LanguagePreference } from './language-preference';

describe('detectBrowserLanguage', () => {
    it('matches on the language prefix, in preference order', () => {
        expect(detectBrowserLanguage(['nl-BE', 'en-US'])).toBe('nl');
        expect(detectBrowserLanguage(['fr-BE', 'nl'])).toBe('nl');
        expect(detectBrowserLanguage(['en-GB'])).toBe('en');
    });

    it('falls back to English when nothing matches', () => {
        expect(detectBrowserLanguage(['fr', 'de'])).toBe('en');
        expect(detectBrowserLanguage([])).toBe('en');
    });
});

// The test runner shares one jsdom (and its localStorage) across spec files, so never leave a
// stored language behind for the next spec.
afterEach(() => localStorage.clear());

describe('I18nService', () => {
    beforeEach(() => localStorage.clear());

    function create(): I18nService {
        return TestBed.inject(I18nService);
    }

    it("uses this device's stored choice over the browser language", () => {
        localStorage.setItem('language', 'nl');
        expect(create().language()).toBe('nl');
    });

    it('translates and interpolates in the active language', () => {
        const i18n = create();
        i18n.set('en');
        expect(i18n.t('common.openItem', { name: 'Acme' })).toBe('Open Acme');
        i18n.set('nl');
        expect(i18n.t('common.openItem', { name: 'Acme' })).toBe('Acme openen');
        expect(localStorage.getItem('language')).toBe('nl');
    });

    it('picks the plural form from count', () => {
        const i18n = create();
        i18n.set('en');
        expect(i18n.t('solicitations.list.count', { count: 1 })).toBe('1 application');
        expect(i18n.t('solicitations.list.count', { count: 3 })).toBe('3 applications');
        i18n.set('nl');
        expect(i18n.t('solicitations.list.count', { count: 1 })).toBe('1 sollicitatie');
        expect(i18n.t('solicitations.list.count', { count: 0 })).toBe('0 sollicitaties');
    });
});

describe('LanguagePreference', () => {
    let locale: string | null;
    let loggedIn: boolean;
    let http: HttpTestingController;

    beforeEach(() => {
        localStorage.clear();
        locale = null;
        loggedIn = true;
        TestBed.configureTestingModule({
            providers: [
                provideHttpClient(),
                provideHttpClientTesting(),
                {
                    provide: AuthService,
                    useValue: { locale: () => locale, isLoggedIn: () => loggedIn },
                },
            ],
        });
        http = TestBed.inject(HttpTestingController);
    });

    afterEach(() => http.verify());

    it("applies the account's saved language on sign-in", () => {
        TestBed.inject(I18nService).set('en');
        locale = 'nl';
        TestBed.inject(LanguagePreference).signedIn();
        expect(TestBed.inject(I18nService).language()).toBe('nl');
    });

    it('keeps the current language when the account has none, without saving', () => {
        TestBed.inject(I18nService).set('nl');
        TestBed.inject(LanguagePreference).signedIn();
        expect(TestBed.inject(I18nService).language()).toBe('nl');
    });

    it('saves a choice made while signed in', () => {
        TestBed.inject(LanguagePreference).choose('nl');
        const req = http.expectOne((r) => r.url.endsWith('/account/language'));
        expect(req.request.method).toBe('PUT');
        expect(req.request.body).toEqual({ language: 'nl' });
        req.flush(null);
    });

    it('pushes a choice made while signed out to the account on sign-in', () => {
        loggedIn = false;
        const preference = TestBed.inject(LanguagePreference);
        preference.choose('nl');
        locale = 'en'; // the account still says English: the fresh choice wins
        loggedIn = true;
        preference.signedIn();

        expect(TestBed.inject(I18nService).language()).toBe('nl');
        http.expectOne((r) => r.url.endsWith('/account/language')).flush(null);
    });
});
