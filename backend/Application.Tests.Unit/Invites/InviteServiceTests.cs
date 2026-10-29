using Application.Common;
using Application.Common.Exceptions;
using Application.Invites;
using Application.Users;
using Domain.Core;
using Domain.Invites;
using FluentAssertions;
using NSubstitute;

namespace Application.Tests.Unit.Invites;

public class InviteServiceTests
{
    private readonly IInviteRepository _repository = Substitute.For<IInviteRepository>();
    private readonly ISecureTokenService _tokens = Substitute.For<ISecureTokenService>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUserDirectory _users = Substitute.For<IUserDirectory>();
    private readonly IInviteNotifier _notifier = Substitute.For<IInviteNotifier>();
    private readonly Guid _adminId = Guid.NewGuid();
    private readonly InviteService _sut;

    public InviteServiceTests()
    {
        _currentUser.Id.Returns(_adminId);
        _tokens.Generate().Returns("raw-token", "second-token");
        _tokens.Hash(Arg.Any<string>()).Returns(ci => $"hash:{ci.Arg<string>()}");
        _notifier.CreateLink(Arg.Any<string>()).Returns(ci => $"https://app/register?token={ci.Arg<string>()}");
        _sut = new InviteService(_repository, _tokens, _currentUser, _users, _notifier);
    }

    [Fact]
    public async Task CreateAsync_CreatesMemberInvite_ForTheEmail_AndEmailsTheLink()
    {
        _notifier.TrySendInviteAsync(default!, default!, default, default).ReturnsForAnyArgs(true);

        var response = await _sut.CreateAsync(new CreateInviteRequest(" new@example.com ", null), CancellationToken.None);

        // invites never grant more than Member, and only the hash is stored
        _repository.Received(1).Add(Arg.Is<Invite>(i =>
            i.Role == Roles.Member && i.Email == "new@example.com" && i.TokenHash == "hash:raw-token"
            && i.CreatedByUserId == _adminId));
        await _notifier.Received(1).TrySendInviteAsync(
            "new@example.com", "https://app/register?token=raw-token", Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        response.Link.Should().Be("https://app/register?token=raw-token");
        response.EmailSent.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_WhenEmailCannotBeSent_StillCreatesInvite_AndReportsIt()
    {
        _notifier.TrySendInviteAsync(default!, default!, default, default).ReturnsForAnyArgs(false);

        var response = await _sut.CreateAsync(new CreateInviteRequest("new@example.com", 3), CancellationToken.None);

        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        response.EmailSent.Should().BeFalse();
        response.Link.Should().NotBeNullOrEmpty(); // admin can share it manually
        response.ExpiresAtUtc.Should().BeCloseTo(DateTime.UtcNow.AddDays(3), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task CreateAsync_WhenEmailAlreadyRegistered_Throws_AndCreatesNothing()
    {
        _users.ExistsByEmailAsync("taken@example.com", Arg.Any<CancellationToken>()).Returns(true);

        var act = () => _sut.CreateAsync(new CreateInviteRequest("taken@example.com", null), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
        _repository.DidNotReceive().Add(Arg.Any<Invite>());
    }

    [Fact]
    public async Task CreateAsync_WhenEmailHasOpenInvite_Throws()
    {
        _repository.HasOpenInviteForEmailAsync("pending@example.com", Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var act = () => _sut.CreateAsync(new CreateInviteRequest("pending@example.com", null), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*Resend*");
    }

    [Fact]
    public async Task ResendAsync_RotatesToken_SoTheOldLinkStopsWorking()
    {
        var invite = new Invite("hash:old", Roles.Member, DateTime.UtcNow.AddHours(-1), _adminId, "late@example.com");
        _repository.GetByIdAsync(invite.Id, Arg.Any<CancellationToken>()).Returns(invite);

        var response = await _sut.ResendAsync(invite.Id, CancellationToken.None);

        invite.TokenHash.Should().Be("hash:raw-token");
        invite.ExpiresAtUtc.Should().BeAfter(DateTime.UtcNow);
        response.Link.Should().EndWith("token=raw-token");
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LookupAsync_WhenExpired_ThrowsNotFound()
    {
        var invite = new Invite("hash:raw", Roles.Member, DateTime.UtcNow.AddMinutes(-1), _adminId, "x@example.com");
        _repository.GetByTokenHashAsync("hash:raw", Arg.Any<CancellationToken>()).Returns(invite);

        var act = () => _sut.LookupAsync("raw", CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public void Reissue_OnAcceptedInvite_Throws()
    {
        var invite = new Invite("hash", Roles.Member, DateTime.UtcNow.AddDays(1), _adminId, "x@example.com");
        invite.Redeem(DateTime.UtcNow);

        var act = () => invite.Reissue("new-hash", DateTime.UtcNow.AddDays(7));

        act.Should().Throw<DomainException>();
    }
}