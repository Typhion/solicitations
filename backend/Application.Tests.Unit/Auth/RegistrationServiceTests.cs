using Application.Auth;
using Application.Common;
using Application.Invites;
using Domain.Invites;
using FluentAssertions;
using NSubstitute;

namespace Application.Tests.Unit.Auth;

public class RegistrationServiceTests
{
    private readonly IInviteRepository _invites = Substitute.For<IInviteRepository>();
    private readonly ISecureTokenService _tokens = Substitute.For<ISecureTokenService>();
    private readonly IUserRegistrar _registrar = Substitute.For<IUserRegistrar>();
    private readonly RegistrationService _sut;

    public RegistrationServiceTests()
    {
        _tokens.Hash("token").Returns("hash");
        _invites.GetByTokenHashAsync("hash", Arg.Any<CancellationToken>())
            .Returns(new Invite("hash", Roles.Member, DateTime.UtcNow.AddDays(1), Guid.NewGuid(), "Invited@Example.com"));
        _registrar.CreateAsync(default!, default!, default!, default!, default).ReturnsForAnyArgs(new UserRegistrationResult(true, []));
        _sut = new RegistrationService(_invites, _tokens, _registrar);
    }

    [Fact]
    public async Task RegisterAsync_WithTheInvitedEmail_IgnoringCase_Succeeds()
    {
        var result = await _sut.RegisterAsync(
            new RegisterRequest("token", "newbie", "invited@example.com", "Password#123456"), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterAsync_WithADifferentEmail_FailsWithoutCreatingAUser()
    {
        var result = await _sut.RegisterAsync(
            new RegisterRequest("token", "newbie", "someone-else@example.com", "Password#123456"), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        await _registrar.DidNotReceiveWithAnyArgs().CreateAsync(default!, default!, default!, default!, default);
    }
}
