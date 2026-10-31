import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { App } from './app';

describe('App', () => {
    afterEach(() => localStorage.clear());

    beforeEach(async () => {
        // jsdom has no matchMedia; the theme service reads prefers-color-scheme on startup.
        vi.stubGlobal(
            'matchMedia',
            vi.fn().mockReturnValue({
                matches: false,
                addEventListener: vi.fn(),
                removeEventListener: vi.fn(),
            }),
        );

        // Pin the language: otherwise it depends on the runner's locale or a value another spec stored.
        localStorage.setItem('language', 'en');

        await TestBed.configureTestingModule({
            imports: [App],
            providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
        }).compileComponents();
    });

    it('should create the app', () => {
        const fixture = TestBed.createComponent(App);
        expect(fixture.componentInstance).toBeTruthy();
    });

    it('should render the brand link', async () => {
        const fixture = TestBed.createComponent(App);
        await fixture.whenStable();
        const compiled = fixture.nativeElement as HTMLElement;
        expect(compiled.querySelector('nav a')?.textContent).toContain('Solicitations');
    });
});
