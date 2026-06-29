import { Routes } from '@angular/router';
import {SolicitationList} from './features/solicitations/solicitation-list/solicitation-list';
import {Login} from './features/auth/login/login';
import {authGuard} from './core/auth/auth.guard';

export const routes: Routes = [
    { path: 'login', component: Login },
    { path: 'solicitations', component: SolicitationList, canActivate: [authGuard] },
    { path: '', redirectTo: 'solicitations', pathMatch: 'full' },
];
