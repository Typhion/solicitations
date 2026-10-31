import { Routes } from '@angular/router';
import { Login } from './features/auth/login/login';
import { adminGuard, authGuard } from './core/auth/auth.guard';

// Feature pages are lazy-loaded so each one only downloads when first visited
// (e.g. the QR code library only ships with the profile page).
const solicitationForm = () =>
    import('./features/solicitations/solicitation-form/solicitation-form').then(
        (m) => m.SolicitationForm,
    );

// `title` sets the browser tab via AppTitleStrategy ("Page · Solicitations"). Detail/edit pages
// refine theirs with the job name once loaded (PageTitle service).
export const routes: Routes = [
    { path: 'login', title: 'Sign in', component: Login },
    {
        path: 'register',
        title: 'Create account',
        loadComponent: () => import('./features/auth/register/register').then((m) => m.Register),
    },
    {
        path: 'solicitations',
        canActivate: [authGuard],
        children: [
            {
                path: '',
                title: 'Solicitations', // the app name alone, no suffix
                loadComponent: () =>
                    import('./features/solicitations/solicitation-list/solicitation-list').then(
                        (m) => m.SolicitationList,
                    ),
            },
            { path: 'new', title: 'New solicitation', loadComponent: solicitationForm },
            // before ':id', or "calendar" would be read as a solicitation id
            {
                path: 'calendar',
                title: 'Calendar',
                loadComponent: () =>
                    import('./features/calendar/meeting-calendar/meeting-calendar').then(
                        (m) => m.MeetingCalendar,
                    ),
            },
            {
                path: ':id',
                title: 'Solicitation',
                loadComponent: () =>
                    import('./features/solicitations/solicitation-detail/solicitation-detail').then(
                        (m) => m.SolicitationDetail,
                    ),
            },
            { path: ':id/edit', title: 'Edit solicitation', loadComponent: solicitationForm },
        ],
    },
    {
        path: 'profile',
        title: 'Profile',
        canActivate: [authGuard],
        loadComponent: () => import('./features/profile/profile').then((m) => m.ProfilePage),
    },
    {
        path: 'admin/users',
        title: 'Users',
        canActivate: [authGuard, adminGuard],
        loadComponent: () => import('./features/admin/users/users').then((m) => m.Users),
    },
    { path: '', redirectTo: 'solicitations', pathMatch: 'full' },
];
