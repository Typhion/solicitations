export interface UserAccount {
    id: string;
    username: string;
    email: string | null;
    roles: string[];
    isLockedOut: boolean;
    twoFactorEnabled: boolean;
    solicitationCount: number;
}

// Mirrors backend Domain/Invites/InviteStatus.cs (serialized as numbers).
export enum InviteStatus {
    Pending = 1,
    Accepted = 2,
    Revoked = 3,
}

export interface Invite {
    id: string;
    email: string | null;
    role: string;
    status: InviteStatus;
    /** Pending but past its expiry date. */
    isExpired: boolean;
    expiresAtUtc: string;
    createdAtUtc: string;
}

export interface CreateInviteRequest {
    email: string;
    expiresInDays: number;
}

/** Returned by create/resend: the only time the raw link is ever available. */
export interface CreatedInvite {
    id: string;
    link: string;
    expiresAtUtc: string;
    emailSent: boolean;
}

export interface DeleteUserRequest {
    username: string;
    code: string;
}
