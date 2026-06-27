import {Component, inject, signal} from '@angular/core';
import {SolicitationService} from '../solicitation.service';
import {PagedResult} from '../../../core/models/paged-result';
import {Solicitation} from '../solicitation.models';

@Component({
    selector: 'solicitations',
    imports: [],
    template: '@if (result(); as r) {\n' +
        '    @for (s of r.items; track s.id) {\n' +
        '      <article>{{ s.jobName }} — status {{ s.status }}</article>\n' +
        '    } @empty {\n' +
        '      <p>No solicitations yet.</p>\n' +
        '    } \n' +
        '  }',
})
export class SolicitationList {
    private readonly service = inject(SolicitationService);
    readonly result = signal<PagedResult<Solicitation> | null>(null);

    ngOnInit() {
        this.service.list().subscribe(r => this.result.set(r));
    }
}
