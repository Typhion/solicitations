import { Injectable, inject } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { RouterStateSnapshot, TitleStrategy } from '@angular/router';

export const APP_NAME = 'Solicitations';

/** Formats and sets the browser tab title: "Page · Solicitations". */
@Injectable({ providedIn: 'root' })
export class PageTitle {
    private readonly title = inject(Title);

    /** For titles only known after loading data (e.g. a solicitation's job name). */
    set(page?: string): void {
        this.title.setTitle(page && page !== APP_NAME ? `${page} · ${APP_NAME}` : APP_NAME);
    }
}

/** Applies each route's static `title` on navigation, in the same format. */
@Injectable()
export class AppTitleStrategy extends TitleStrategy {
    private readonly pageTitle = inject(PageTitle);

    override updateTitle(snapshot: RouterStateSnapshot): void {
        this.pageTitle.set(this.buildTitle(snapshot));
    }
}
