import { Routes } from '@angular/router';
import { Login } from './features/auth/login/login';
import { adminGuard, authGuard } from './core/auth/auth.guard';

// Feature pages are lazy-loaded so each one only downloads when first visited
// (e.g. the QR code library only ships with the profile page).
const solicitationForm = () =>
    import('./features/solicitations/solicitation-form/solicitation-form').then(
        (m) => m.SolicitationForm,
    );

// `title` is a translation key; AppTitleStrategy turns it into the browser tab ("Page · Solicitations").
// Detail/edit pages refine theirs with the job name once loaded (PageTitle service).
export const routes: Routes = [
    { path: 'login', title: 'titles.signIn', component: Login },
    {
        path: 'register',
        title: 'titles.register',
        loadComponent: () => import('./features/auth/register/register').then((m) => m.Register),
    },
    {
        path: 'solicitations',
        canActivate: [authGuard],
        children: [
            {
                path: '',
                title: 'app.name', // the app name alone, no suffix
                loadComponent: () =>
                    import('./features/solicitations/solicitation-list/solicitation-list').then(
                        (m) => m.SolicitationList,
                    ),
            },
            { path: 'new', title: 'titles.newSolicitation', loadComponent: solicitationForm },
            // before ':id', or "calendar"/"map" would be read as a solicitation id
            {
                path: 'calendar',
                title: 'titles.calendar',
                loadComponent: () =>
                    import('./features/calendar/meeting-calendar/meeting-calendar').then(
                        (m) => m.MeetingCalendar,
                    ),
            },
            {
                path: 'map',
                title: 'titles.map',
                loadComponent: () =>
                    import('./features/solicitations/solicitation-map/solicitation-map').then(
                        (m) => m.SolicitationMap,
                    ),
            },
            {
                path: ':id',
                title: 'titles.solicitation',
                loadComponent: () =>
                    import('./features/solicitations/solicitation-detail/solicitation-detail').then(
                        (m) => m.SolicitationDetail,
                    ),
            },
            { path: ':id/edit', title: 'titles.editSolicitation', loadComponent: solicitationForm },
        ],
    },
    {
        path: 'profile',
        title: 'titles.profile',
        canActivate: [authGuard],
        loadComponent: () => import('./features/profile/profile').then((m) => m.ProfilePage),
    },
    {
        path: 'admin/users',
        title: 'titles.users',
        canActivate: [authGuard, adminGuard],
        loadComponent: () => import('./features/admin/users/users').then((m) => m.Users),
    },
    { path: '', redirectTo: 'solicitations', pathMatch: 'full' },
];
