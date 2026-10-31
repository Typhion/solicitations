using System.Globalization;
using Application.Common;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Security;

/// <summary>
/// Dutch texts for the Identity errors users actually run into (password policy, taken
/// username/email), picked by the request's UI culture. Everything else stays English.
/// </summary>
internal sealed class LocalizedIdentityErrorDescriber : IdentityErrorDescriber
{
    private static bool Dutch => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == Languages.Nl;

    private IdentityError Pick(IdentityError english, string dutch)
        => Dutch ? new IdentityError { Code = english.Code, Description = dutch } : english;

    public override IdentityError PasswordTooShort(int length)
        => Pick(base.PasswordTooShort(length), $"Wachtwoorden moeten minstens {length} tekens lang zijn.");

    public override IdentityError PasswordRequiresNonAlphanumeric()
        => Pick(base.PasswordRequiresNonAlphanumeric(), "Wachtwoorden moeten minstens één speciaal teken bevatten.");

    public override IdentityError PasswordRequiresDigit()
        => Pick(base.PasswordRequiresDigit(), "Wachtwoorden moeten minstens één cijfer ('0'-'9') bevatten.");

    public override IdentityError PasswordRequiresLower()
        => Pick(base.PasswordRequiresLower(), "Wachtwoorden moeten minstens één kleine letter ('a'-'z') bevatten.");

    public override IdentityError PasswordRequiresUpper()
        => Pick(base.PasswordRequiresUpper(), "Wachtwoorden moeten minstens één hoofdletter ('A'-'Z') bevatten.");

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars)
        => Pick(base.PasswordRequiresUniqueChars(uniqueChars), $"Wachtwoorden moeten minstens {uniqueChars} verschillende tekens bevatten.");

    public override IdentityError PasswordMismatch()
        => Pick(base.PasswordMismatch(), "Onjuist wachtwoord.");

    public override IdentityError DuplicateUserName(string userName)
        => Pick(base.DuplicateUserName(userName), $"Gebruikersnaam '{userName}' is al in gebruik.");

    public override IdentityError DuplicateEmail(string email)
        => Pick(base.DuplicateEmail(email), $"E-mailadres '{email}' is al in gebruik.");

    public override IdentityError InvalidUserName(string? userName)
        => Pick(base.InvalidUserName(userName), $"Gebruikersnaam '{userName}' is ongeldig; gebruik alleen letters en cijfers.");

    public override IdentityError InvalidEmail(string? email)
        => Pick(base.InvalidEmail(email), $"E-mailadres '{email}' is ongeldig.");
}
