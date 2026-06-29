import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { CdkMenuTrigger } from '@angular/cdk/menu';
import { AuthService } from './core/auth/auth.service';
import { ThemeService } from './core/theme/theme.service';
import { Menu, MenuItem, MenuSeparator } from './shared/ui/menu/menu';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, CdkMenuTrigger, Menu, MenuItem, MenuSeparator],
  templateUrl: './app.html',
})
export class App {
  protected readonly title = signal('frontend');
  protected readonly auth = inject(AuthService);
  protected readonly theme = inject(ThemeService);
  private readonly router = inject(Router);

  logout(): void {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
