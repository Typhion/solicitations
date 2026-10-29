import { Routes } from '@angular/router';
import { Login } from './features/auth/login/login';
import { adminGuard, authGuard } from './core/auth/auth.guard';

// Feature pages are lazy-loaded so each one only downloads when first visited
// (e.g. the QR code library only ships with the profile page).
const solicitationForm = () =>
    import('./features/solicitations/solicitation-form/solicitation-form').then(
        (m) => m.SolicitationForm,
    );

export const routes: Routes = [
    { path: 'login', component: Login },
    {
        path: 'register',
        loadComponent: () => import('./features/auth/register/register').then((m) => m.Register),
    },
    {
        path: 'solicitations',
        canActivate: [authGuard],
        children: [
            {
                path: '',
                loadComponent: () =>
                    import('./features/solicitations/solicitation-list/solicitation-list').then(
                        (m) => m.SolicitationList,
                    ),
            },
            { path: 'new', loadComponent: solicitationForm },
            // before ':id', or "calendar" would be read as a solicitation id
            {
                path: 'calendar',
                loadComponent: () =>
                    import('./features/calendar/meeting-calendar/meeting-calendar').then(
                        (m) => m.MeetingCalendar,
                    ),
            },
            {
                path: ':id',
                loadComponent: () =>
                    import('./features/solicitations/solicitation-detail/solicitation-detail').then(
                        (m) => m.SolicitationDetail,
                    ),
            },
            { path: ':id/edit', loadComponent: solicitationForm },
        ],
    },
    {
        path: 'profile',
        canActivate: [authGuard],
        loadComponent: () => import('./features/profile/profile').then((m) => m.ProfilePage),
    },
    {
        path: 'admin/users',
        canActivate: [authGuard, adminGuard],
        loadComponent: () => import('./features/admin/users/users').then((m) => m.Users),
    },
    { path: '', redirectTo: 'solicitations', pathMatch: 'full' },
];
