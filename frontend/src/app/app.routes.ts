import { Routes } from '@angular/router';
import {SolicitationList} from './features/solicitations/solicitation-list/solicitation-list';

export const routes: Routes = [
    { path: 'solicitations', component: SolicitationList },
    { path: '', redirectTo: 'solicitations', pathMatch: 'full' },
];
