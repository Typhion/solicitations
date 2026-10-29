using Application.Auth.Login;
using FluentAssertions;

namespace Application.Tests.Unit.Auth.Login;

public class LoginResultTests
{
    [Fact]
    public void Success_SetsTokenAndExpiry_AndNoError()
    {
        var expiresAt = DateTime.UtcNow.AddHours(1);

        var result = LoginResult.Success("jwt-token", expiresAt, "refresh-token");

        result.Succeeded.Should().BeTrue();
        result.Token.Should().Be("jwt-token");
        result.ExpiresAt.Should().Be(expiresAt);
        result.RefreshToken.Should().Be("refresh-token");
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Fail_SetsError_AndNoToken()
    {
        var result = LoginResult.Fail("Invalid username or password.");

        result.Succeeded.Should().BeFalse();
        result.Token.Should().BeNull();
        result.ExpiresAt.Should().BeNull();
        result.RefreshToken.Should().BeNull();
        result.Error.Should().Be("Invalid username or password.");
        result.RequiresTwoFactor.Should().BeFalse();
    }

    [Fact]
    public void TwoFactorRequired_CarriesChallenge_AndNoSession()
    {
        var result = LoginResult.TwoFactorRequired("challenge");

        result.RequiresTwoFactor.Should().BeTrue();
        result.ChallengeToken.Should().Be("challenge");
        // not a successful sign-in yet: no tokens are issued
        result.Succeeded.Should().BeFalse();
        result.Token.Should().BeNull();
        result.RefreshToken.Should().BeNull();
    }
}
