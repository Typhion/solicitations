using Application.Account;
using Application.Common;
using Application.Solicitations;
using Application.Users;
using Domain.Core;
using FluentAssertions;
using NSubstitute;

namespace Application.Tests.Unit.Users;

public class UserManagementServiceTests
{
    private readonly IUserDirectory _directory = Substitute.For<IUserDirectory>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly ISolicitationRepository _solicitations = Substitute.For<ISolicitationRepository>();
    private readonly IAccountManager _accounts = Substitute.For<IAccountManager>();
    private readonly Guid _adminId = Guid.NewGuid();
    private readonly UserSummary _alice = new(Guid.NewGuid(), "alice", "alice@example.com", [Roles.Member], false, false);
    private readonly UserManagementService _sut;

    public UserManagementServiceTests()
    {
        _currentUser.Id.Returns(_adminId);
        _directory.FindAsync(_alice.Id, Arg.Any<CancellationToken>()).Returns(_alice);
        _accounts.VerifyAuthenticatorCodeAsync(_adminId, "123456", Arg.Any<CancellationToken>()).Returns(true);
        _sut = new UserManagementService(_directory, _currentUser, _solicitations, _accounts);
    }

    [Fact]
    public async Task DeleteAsync_WithMatchingUsernameAndValidCode_DeletesUserAndData()
    {
        await _sut.DeleteAsync(_alice.Id, new DeleteUserRequest("alice", "123456"), CancellationToken.None);

        await _directory.Received(1).DeleteWithDataAsync(_alice.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_WhenUsernameDoesNotMatch_Throws_WithoutUsingTheCode()
    {
        var act = () => _sut.DeleteAsync(_alice.Id, new DeleteUserRequest("bob", "123456"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*username*");
        // a typo shouldn't burn one of the admin's lockout attempts
        await _accounts.DidNotReceiveWithAnyArgs().VerifyAuthenticatorCodeAsync(default, default!, default);
        await _directory.DidNotReceiveWithAnyArgs().DeleteWithDataAsync(default, default);
    }

    [Fact]
    public async Task DeleteAsync_WithInvalidCode_Throws_AndDeletesNothing()
    {
        var act = () => _sut.DeleteAsync(_alice.Id, new DeleteUserRequest("alice", "000000"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*code*");
        await _directory.DidNotReceiveWithAnyArgs().DeleteWithDataAsync(default, default);
    }

    [Fact]
    public async Task DeleteAsync_Self_Throws()
    {
        var act = () => _sut.DeleteAsync(_adminId, new DeleteUserRequest("admin", "123456"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task DeleteAsync_LastAdmin_Throws()
    {
        var lastAdmin = _alice with { Roles = [Roles.Admin] };
        _directory.FindAsync(lastAdmin.Id, Arg.Any<CancellationToken>()).Returns(lastAdmin);
        _directory.CountInRoleAsync(Roles.Admin, Arg.Any<CancellationToken>()).Returns(1);

        var act = () => _sut.DeleteAsync(lastAdmin.Id, new DeleteUserRequest("alice", "123456"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*last administrator*");
    }

    [Fact]
    public async Task RevokeRoleAsync_OwnAdminRole_Throws()
    {
        _directory.FindAsync(_adminId, Arg.Any<CancellationToken>())
            .Returns(new UserSummary(_adminId, "admin", null, [Roles.Admin], false, true));
        _directory.CountInRoleAsync(Roles.Admin, Arg.Any<CancellationToken>()).Returns(2);

        var act = () => _sut.RevokeRoleAsync(_adminId, Roles.Admin, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task ListAsync_IncludesSolicitationCountPerUser()
    {
        _directory.ListAsync(Arg.Any<CancellationToken>()).Returns([_alice]);
        _solicitations.CountAsync(_alice.Id, Arg.Any<CancellationToken>()).Returns(12);

        var users = await _sut.ListAsync(CancellationToken.None);

        users.Should().ContainSingle().Which.SolicitationCount.Should().Be(12);
    }
}
