using Application.Account;
using Application.Auth;
using Application.Auth.Login;
using Application.Common;
using FluentAssertions;
using NSubstitute;

namespace Application.Tests.Unit.Account;

public class AccountServiceTests
{
    private readonly IAccountManager _accounts = Substitute.For<IAccountManager>();
    private readonly IAuthenticationService _auth = Substitute.For<IAuthenticationService>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly AccountService _sut;

    public AccountServiceTests()
    {
        _currentUser.Id.Returns(_userId);
        _sut = new AccountService(_accounts, _auth, _currentUser);
    }

    [Fact]
    public async Task ChangePasswordAsync_WhenChanged_ReissuesSessionForCurrentUser()
    {
        _accounts.ChangePasswordAsync(_userId, "old", "new", Arg.Any<CancellationToken>())
            .Returns(AccountResult.Success());
        var session = LoginResult.Success("token", DateTime.UtcNow.AddMinutes(15), "refresh");
        _auth.ReissueAsync(_userId, Arg.Any<CancellationToken>()).Returns(session);

        var result = await _sut.ChangePasswordAsync(new ChangePasswordRequest("old", "new"), CancellationToken.None);

        // all other sessions are revoked by ReissueAsync; the caller gets the fresh one
        result.Succeeded.Should().BeTrue();
        result.Value.Should().Be(session);
        await _auth.Received(1).ReissueAsync(_userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangePasswordAsync_WhenRejected_ReturnsErrors_AndKeepsSessions()
    {
        _accounts.ChangePasswordAsync(_userId, "wrong", "new", Arg.Any<CancellationToken>())
            .Returns(AccountResult.Fail("Incorrect password."));

        var result = await _sut.ChangePasswordAsync(new ChangePasswordRequest("wrong", "new"), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Equal("Incorrect password.");
        await _auth.DidNotReceive().ReissueAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnableTwoFactorAsync_ActsOnCurrentUser()
    {
        IReadOnlyList<string> codes = ["AAAAA-BBBBB"];
        _accounts.EnableTwoFactorAsync(_userId, "123456", Arg.Any<CancellationToken>())
            .Returns(AccountResult<IReadOnlyList<string>>.Success(codes));

        var result = await _sut.EnableTwoFactorAsync(new EnableTwoFactorRequest("123456"), CancellationToken.None);

        result.Value.Should().Equal(codes);
    }
}
