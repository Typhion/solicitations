export interface LoginRequest {
    username: string;
    password: string;
}

// The login/refresh endpoints now return only the access token in the body.
// The refresh token lives in an httpOnly cookie the browser handles for us.
export interface AccessToken {
    token: string;
    expiresAt: string;
}

// Returned by /login instead of a token when the account has two-factor authentication on.
export interface TwoFactorChallenge {
    requiresTwoFactor: true;
    challengeToken: string;
}

export type LoginResponse = AccessToken | TwoFactorChallenge;

export interface TwoFactorLoginRequest {
    challengeToken: string;
    code: string;
    useRecoveryCode: boolean;
}

export interface InviteLookup {
    email: string | null;
    expiresAtUtc: string;
}

export interface RegisterRequest {
    token: string;
    username: string;
    email: string;
    password: string;
}

export function isTwoFactorChallenge(r: LoginResponse): r is TwoFactorChallenge {
    return 'requiresTwoFactor' in r && r.requiresTwoFactor;
}
