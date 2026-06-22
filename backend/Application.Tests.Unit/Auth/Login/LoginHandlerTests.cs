using System.Threading;
using System.Threading.Tasks;
using Application.Auth;
using Application.Auth.Login;
using FluentAssertions;
using NSubstitute;

namespace Application.Tests.Unit.Auth.Login;

public class LoginHandlerTests
{
    private readonly IAuthenticationService _auth = Substitute.For<IAuthenticationService>();
    private readonly LoginHandler _sut;

    public LoginHandlerTests() => _sut = new LoginHandler(_auth);

    [Fact]
    public async Task Handle_ForwardsCredentialsToAuthenticationService()
    {
        var command = new LoginCommand("admin", "admin");
        _auth.LoginAsync("admin", "admin", Arg.Any<CancellationToken>())
            .Returns(LoginResult.Success("token", DateTime.UtcNow));

        await _sut.Handle(command, CancellationToken.None);

        await _auth.Received(1).LoginAsync("admin", "admin", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReturnsResult_WhenAuthenticationSucceeds()
    {
        var expiresAt = DateTime.UtcNow.AddHours(1);
        _auth.LoginAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(LoginResult.Success("jwt-token", expiresAt));

        var result = await _sut.Handle(new LoginCommand("admin", "admin"), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Token.Should().Be("jwt-token");
        result.ExpiresAt.Should().Be(expiresAt);
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ReturnsFailure_WhenAuthenticationFails()
    {
        _auth.LoginAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(LoginResult.Fail("Invalid username or password."));

        var result = await _sut.Handle(new LoginCommand("admin", "wrong"), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Token.Should().BeNull();
        result.ExpiresAt.Should().BeNull();
        result.Error.Should().Be("Invalid username or password.");
    }
}
