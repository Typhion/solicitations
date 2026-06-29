export interface LoginRequest { username: string; password: string; }

// The login/refresh endpoints now return only the access token in the body.
// The refresh token lives in an httpOnly cookie the browser handles for us.
export interface AccessToken {
    token: string;
    expiresAt: string;
}
