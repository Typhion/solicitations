import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { AuthService } from './auth.service';

// Builds an unsigned JWT-shaped token with the given payload (only the payload is read client-side).
const tokenWith = (payload: object) =>
    `x.${btoa(JSON.stringify(payload)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '')}.y`;

describe('AuthService claims', () => {
    let auth: AuthService;

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [provideHttpClient(), provideHttpClientTesting()],
        });
        auth = TestBed.inject(AuthService);
    });

    it('reads roles from the full .NET role claim type the API issues', () => {
        auth.setSession({
            token: tokenWith({
                unique_name: 'admin',
                'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': 'Admin',
            }),
            expiresAt: '',
        });

        expect(auth.username()).toBe('admin');
        expect(auth.roles()).toEqual(['Admin']);
        expect(auth.isAdmin()).toBe(true);
    });

    it('handles several roles and the short claim name', () => {
        auth.setSession({ token: tokenWith({ role: ['Member', 'Admin'] }), expiresAt: '' });

        expect(auth.roles()).toEqual(['Member', 'Admin']);
        expect(auth.isAdmin()).toBe(true);
    });

    it('is not admin without a role claim', () => {
        auth.setSession({ token: tokenWith({ unique_name: 'bob' }), expiresAt: '' });

        expect(auth.isAdmin()).toBe(false);
    });
});
