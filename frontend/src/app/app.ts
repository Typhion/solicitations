import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { CdkMenuTrigger } from '@angular/cdk/menu';
import { AuthService } from './core/auth/auth.service';
import { ThemeService } from './core/theme/theme.service';
import { I18nService } from './core/i18n/i18n.service';
import { LANGUAGES, Language } from './core/i18n/language';
import { LanguagePreference } from './core/i18n/language-preference';
import { TranslatePipe } from './core/i18n/translate.pipe';
import { Menu, MenuItem, MenuItemRadio, MenuSeparator } from './shared/ui/menu/menu';

@Component({
    selector: 'app-root',
    imports: [
        RouterOutlet,
        RouterLink,
        CdkMenuTrigger,
        Menu,
        MenuItem,
        MenuItemRadio,
        MenuSeparator,
        TranslatePipe,
    ],
    templateUrl: './app.html',
})
export class App {
    protected readonly auth = inject(AuthService);
    protected readonly theme = inject(ThemeService);
    protected readonly i18n = inject(I18nService);
    protected readonly languages = LANGUAGES;
    private readonly languagePreference = inject(LanguagePreference);
    private readonly router = inject(Router);

    chooseLanguage(language: Language): void {
        this.languagePreference.choose(language);
    }

    openProfile(): void {
        this.router.navigate(['/profile']);
    }

    openUsers(): void {
        this.router.navigate(['/admin/users']);
    }

    logout(): void {
        this.auth.logout();
        this.router.navigate(['/login']);
    }
}
