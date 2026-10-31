import { TestBed } from '@angular/core/testing';
import { Title } from '@angular/platform-browser';
import { PageTitle } from './page-title';

describe('PageTitle', () => {
    let pageTitle: PageTitle;
    let title: Title;

    beforeEach(() => {
        pageTitle = TestBed.inject(PageTitle);
        title = TestBed.inject(Title);
    });

    it('suffixes the page with the app name', () => {
        pageTitle.set('Calendar');
        TestBed.tick();
        expect(title.getTitle()).toBe('Calendar · Solicitations');
    });

    it('shows just the app name for the app-name page or no page', () => {
        pageTitle.set('Solicitations');
        TestBed.tick();
        expect(title.getTitle()).toBe('Solicitations');

        pageTitle.set(undefined);
        TestBed.tick();
        expect(title.getTitle()).toBe('Solicitations');
    });

    it('translates route title keys', () => {
        pageTitle.setKey('titles.calendar');
        TestBed.tick();
        expect(title.getTitle()).toBe('Calendar · Solicitations');
    });
});
