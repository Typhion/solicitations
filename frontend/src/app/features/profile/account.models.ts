export interface Profile {
    id: string;
    username: string;
    email: string | null;
    roles: string[];
    twoFactorEnabled: boolean;
    recoveryCodesLeft: number;
}

export interface ChangePasswordRequest {
    currentPassword: string;
    newPassword: string;
}

export interface AuthenticatorSetup {
    /** Grouped, lower-case key for typing in by hand. */
    sharedKey: string;
    /** otpauth://totp/… — encoded in the QR code. */
    authenticatorUri: string;
}

export interface RecoveryCodes {
    recoveryCodes: string[];
}
