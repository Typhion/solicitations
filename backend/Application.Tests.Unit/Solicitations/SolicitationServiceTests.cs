using Application.Common;
using Application.Common.Exceptions;
using Application.Places;
using Domain.Core;
using Application.Solicitations;
using Domain.Meeting;
using Domain.Solicitation;
using FluentAssertions;
using NSubstitute;

namespace Application.Tests.Unit.Solicitations;

public class SolicitationServiceTests
{
    private readonly ISolicitationRepository _repository = Substitute.For<ISolicitationRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPlacesClient _places = Substitute.For<IPlacesClient>();
    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly SolicitationService _sut;

    public SolicitationServiceTests()
    {
        _currentUser.Id.Returns(_ownerId);
        _sut = new SolicitationService(_repository, _currentUser, _places, TimeProvider.System);
    }

    // ---------- Create ----------

    [Fact]
    public async Task CreateAsync_AddsEntity_Saves_AndStampsCurrentUserAsOwner()
    {
        var request = NewCreateRequest("Backend Developer");

        var response = await _sut.CreateAsync(request, CancellationToken.None);

        // ownership: the new aggregate is stamped with the current user's id
        _repository.Received(1).Add(Arg.Is<Solicitation>(s => s.OwnerId == _ownerId));
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());

        response.JobName.Should().Be("Backend Developer");
        response.Location.Should().Be(request.Location);
        response.Website.Should().Be(request.Website);
        response.Contact.Should().Be(request.Contact);
    }

    [Fact]
    public async Task CreateAsync_DefaultsStatusToDraft()
    {
        var response = await _sut.CreateAsync(NewCreateRequest(), CancellationToken.None);

        response.Status.Should().Be(SolicitationStatus.Draft);
    }

    // ---------- Get ----------

    [Fact]
    public async Task GetAsync_WhenFound_ReturnsMappedResponse_ScopedToCurrentUser()
    {
        var solicitation = NewSolicitation("Platform Engineer");
        _repository.GetByIdAsync(solicitation.Id, _ownerId, Arg.Any<CancellationToken>()).Returns(solicitation);

        var response = await _sut.GetAsync(solicitation.Id, CancellationToken.None);

        response.Id.Should().Be(solicitation.Id);
        response.JobName.Should().Be("Platform Engineer");
        // the query is scoped to the current user's id
        await _repository.Received(1).GetByIdAsync(solicitation.Id, _ownerId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAsync_WhenMissingOrNotOwned_ThrowsNotFound()
    {
        // repo returns null both for non-existent ids and rows owned by someone else
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Solicitation?)null);

        var act = () => _sut.GetAsync(Guid.NewGuid(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ---------- List ----------

    [Fact]
    public async Task ListAsync_ReturnsCurrentUsersMappedResponses_Paged()
    {
        var items = new List<Solicitation> { NewSolicitation("One"), NewSolicitation("Two") };
        _repository.CountAsync(_ownerId, Arg.Any<CancellationToken>()).Returns(2);
        _repository.ListAsync(_ownerId, 0, 20, Arg.Any<CancellationToken>()).Returns(items);

        var result = await _sut.ListAsync(page: 1, pageSize: 20, CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.Items.Select(r => r.JobName).Should().Equal("One", "Two");
        result.TotalCount.Should().Be(2);
        result.Page.Should().Be(1);
        await _repository.Received(1).ListAsync(_ownerId, 0, 20, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListAsync_ClampsPageSizeAndComputesSkip()
    {
        _repository.CountAsync(_ownerId, Arg.Any<CancellationToken>()).Returns(0);
        _repository.ListAsync(_ownerId, Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<Solicitation>());

        await _sut.ListAsync(page: 3, pageSize: 1000, CancellationToken.None);

        // pageSize clamped to 100; skip = (3-1)*100 = 200
        await _repository.Received(1).ListAsync(_ownerId, 200, 100, Arg.Any<CancellationToken>());
    }

    // ---------- Update ----------

    [Fact]
    public async Task UpdateAsync_WhenFound_MutatesTrackedEntity_AndSaves()
    {
        var solicitation = NewSolicitation("Old Title");
        _repository.GetByIdAsync(solicitation.Id, _ownerId, Arg.Any<CancellationToken>()).Returns(solicitation);

        var request = new UpdateSolicitationRequest(
            "New Title",
            new LocationDto("BE", "Ghent", "9000", "Veldstraat", "1"),
            new WebsiteDto("LinkedIn", "https://linkedin.com"),
            new ContactDto("Jane", "0470", "jane@example.com"),
            SolicitationStatus.Applied);

        await _sut.UpdateAsync(solicitation.Id, request, CancellationToken.None);

        // No repository.Update(...) exists — EF tracks the loaded entity, so we
        // assert the instance itself was mutated in place, then saved once.
        solicitation.JobName.Should().Be("New Title");
        solicitation.Status.Should().Be(SolicitationStatus.Applied);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenMissingOrNotOwned_ThrowsNotFound_AndDoesNotSave()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Solicitation?)null);

        var act = () => _sut.UpdateAsync(Guid.NewGuid(), NewUpdateRequest(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ---------- Delete ----------

    [Fact]
    public async Task DeleteAsync_WhenFound_RemovesEntity_AndSaves()
    {
        var solicitation = NewSolicitation();
        _repository.GetByIdAsync(solicitation.Id, _ownerId, Arg.Any<CancellationToken>()).Returns(solicitation);

        await _sut.DeleteAsync(solicitation.Id, CancellationToken.None);

        _repository.Received(1).Remove(solicitation);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_WhenMissingOrNotOwned_ThrowsNotFound_AndDoesNotRemoveOrSave()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Solicitation?)null);

        var act = () => _sut.DeleteAsync(Guid.NewGuid(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _repository.DidNotReceive().Remove(Arg.Any<Solicitation>());
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ---------- Update meeting ----------

    [Fact]
    public async Task UpdateMeetingAsync_WhenFound_MutatesMeetingInPlace_AndSaves()
    {
        var solicitation = NewSolicitation();
        var meeting = solicitation.AddMeeting(new DateTime(2026, 11, 3, 9, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 11, 3, 10, 0, 0, DateTimeKind.Utc), MeetingType.PhoneScreen, isOnline: false, onlineTool: null);
        _repository.GetByIdAsync(solicitation.Id, _ownerId, Arg.Any<CancellationToken>()).Returns(solicitation);

        var scheduledAt = new DateTime(2026, 11, 5, 14, 30, 0, DateTimeKind.Utc);
        var endsAt = scheduledAt.AddMinutes(90);
        var request = new UpdateMeetingRequest(scheduledAt, endsAt, MeetingType.TechnicalInterview, IsOnline: true, OnlineTool: "Teams");

        await _sut.UpdateMeetingAsync(solicitation.Id, meeting.Id, request, CancellationToken.None);

        // same meeting instance (id preserved), updated in place
        solicitation.Meetings.Should().ContainSingle().Which.Should().BeSameAs(meeting);
        meeting.ScheduledAtUtc.Should().Be(scheduledAt);
        meeting.EndsAtUtc.Should().Be(endsAt);
        meeting.Type.Should().Be(MeetingType.TechnicalInterview);
        meeting.IsOnline.Should().BeTrue();
        meeting.OnlineTool.Should().Be("Teams");
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateMeetingAsync_WhenSwitchedToInPerson_ClearsOnlineTool()
    {
        var solicitation = NewSolicitation();
        var meeting = solicitation.AddMeeting(DateTime.UtcNow, DateTime.UtcNow.AddHours(1), MeetingType.PhoneScreen, isOnline: true, onlineTool: "Zoom");
        _repository.GetByIdAsync(solicitation.Id, _ownerId, Arg.Any<CancellationToken>()).Returns(solicitation);

        var request = new UpdateMeetingRequest(DateTime.UtcNow, DateTime.UtcNow.AddHours(1), MeetingType.OnSite, IsOnline: false, OnlineTool: "Zoom");

        await _sut.UpdateMeetingAsync(solicitation.Id, meeting.Id, request, CancellationToken.None);

        meeting.IsOnline.Should().BeFalse();
        meeting.OnlineTool.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]   // ends when it starts
    [InlineData(-30)] // ends before it starts
    public async Task UpdateMeetingAsync_WhenEndNotAfterStart_ThrowsDomainException_AndDoesNotSave(int endOffsetMinutes)
    {
        var solicitation = NewSolicitation();
        var start = new DateTime(2026, 11, 3, 9, 0, 0, DateTimeKind.Utc);
        var meeting = solicitation.AddMeeting(start, start.AddHours(1), MeetingType.PhoneScreen, isOnline: false, onlineTool: null);
        _repository.GetByIdAsync(solicitation.Id, _ownerId, Arg.Any<CancellationToken>()).Returns(solicitation);

        var request = new UpdateMeetingRequest(start, start.AddMinutes(endOffsetMinutes), MeetingType.PhoneScreen, IsOnline: false, OnlineTool: null);
        var act = () => _sut.UpdateMeetingAsync(solicitation.Id, meeting.Id, request, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
        meeting.EndsAtUtc.Should().Be(start.AddHours(1));
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddMeetingAsync_WhenEndNotAfterStart_ThrowsDomainException_AndDoesNotSave()
    {
        var solicitation = NewSolicitation();
        _repository.GetByIdAsync(solicitation.Id, _ownerId, Arg.Any<CancellationToken>()).Returns(solicitation);
        var start = new DateTime(2026, 11, 3, 9, 0, 0, DateTimeKind.Utc);

        var act = () => _sut.AddMeetingAsync(solicitation.Id,
            new AddMeetingRequest(start, start, MeetingType.PhoneScreen, IsOnline: false, OnlineTool: null), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
        solicitation.Meetings.Should().BeEmpty();
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateMeetingAsync_WhenMeetingNotOnSolicitation_ThrowsNotFound_AndDoesNotSave()
    {
        var solicitation = NewSolicitation();
        _repository.GetByIdAsync(solicitation.Id, _ownerId, Arg.Any<CancellationToken>()).Returns(solicitation);

        var act = () => _sut.UpdateMeetingAsync(solicitation.Id, Guid.NewGuid(), NewUpdateMeetingRequest(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateMeetingAsync_WhenSolicitationMissingOrNotOwned_ThrowsNotFound_AndDoesNotSave()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Solicitation?)null);

        var act = () => _sut.UpdateMeetingAsync(Guid.NewGuid(), Guid.NewGuid(), NewUpdateMeetingRequest(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ---------- Address verification ----------

    private const string GhentPlaceId = "ChIJ-ghent-veldstraat-1";

    private static readonly PlaceAddress Ghent = new(GhentPlaceId, "Veldstraat 1, 9000 Gent, België",
        "België", "BE", "Gent", "9000", "Veldstraat", "1", 51.05, 3.72);

    private static readonly LocationDto PickedGhent =
        new("België", "Gent", "9000", "Veldstraat", "1", Validated: true, PlaceId: GhentPlaceId);

    private static Location VerifiedGhent(DateTime verifiedAt) => Location.Verified(
        "België", "Gent", "9000", "Veldstraat", "1", GhentPlaceId, 51.05, 3.72, verifiedAt);

    [Fact]
    public async Task CreateAsync_FreeText_IsStoredAsTyped_WithoutCallingGoogle()
    {
        var request = NewCreateRequest() with { Location = new LocationDto("US", "Austin", "73301", "Congress Ave", "1") };

        var response = await _sut.CreateAsync(request, CancellationToken.None);

        response.Location.Validated.Should().BeFalse();
        response.Location.PlaceId.Should().BeNull();
        response.Location.Latitude.Should().BeNull();
        _places.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAsync_PickedPlace_StoresGooglesAddress_PlaceIdAndCoordinates()
    {
        _places.GetPlaceAsync(GhentPlaceId, null, Arg.Any<CancellationToken>()).Returns(Ghent);

        var response = await _sut.CreateAsync(NewCreateRequest() with { Location = PickedGhent }, CancellationToken.None);

        response.Location.Should().Be(
            new LocationDto("België", "Gent", "9000", "Veldstraat", "1", true, GhentPlaceId, 51.05, 3.72));
    }

    [Fact]
    public async Task CreateAsync_VerifyWithoutPick_SearchesTheTypedAddress()
    {
        _places.FindAddressAsync(Arg.Any<AddressQuery>(), Arg.Any<CancellationToken>()).Returns(Ghent);
        var typed = new LocationDto("Belgium", "gent", "9000", "veldstraat", "1", Validated: true);

        var response = await _sut.CreateAsync(NewCreateRequest() with { Location = typed }, CancellationToken.None);

        response.Location.PlaceId.Should().Be(GhentPlaceId);
        await _places.Received(1).FindAddressAsync(
            new AddressQuery("Belgium", "gent", "9000", "veldstraat", "1"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_PlaceWithoutHouseNumber_KeepsTheTypedNumber()
    {
        _places.GetPlaceAsync(GhentPlaceId, null, Arg.Any<CancellationToken>()).Returns(Ghent with { StreetNumber = "" });

        var response = await _sut.CreateAsync(
            NewCreateRequest() with { Location = PickedGhent with { StreetNumber = "12B" } }, CancellationToken.None);

        response.Location.StreetNumber.Should().Be("12B");
    }

    [Fact]
    public async Task CreateAsync_NoMatch_ThrowsDomainException_AndDoesNotSave()
    {
        _places.GetPlaceAsync(GhentPlaceId, null, Arg.Any<CancellationToken>()).Returns((PlaceAddress?)null);

        var act = () => _sut.CreateAsync(NewCreateRequest() with { Location = PickedGhent }, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*could not be verified*");
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_PlaceOutsideTheEu_ThrowsDomainException()
    {
        _places.GetPlaceAsync(GhentPlaceId, null, Arg.Any<CancellationToken>())
            .Returns(Ghent with { Country = "United States", CountryCode = "US" });

        var act = () => _sut.CreateAsync(NewCreateRequest() with { Location = PickedGhent }, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*EU*");
    }

    [Fact]
    public async Task CreateAsync_GoogleUnavailable_ThrowsDomainException()
    {
        _places.GetPlaceAsync(GhentPlaceId, null, Arg.Any<CancellationToken>())
            .Returns<PlaceAddress?>(_ => throw new PlacesUnavailableException("down"));

        var act = () => _sut.CreateAsync(NewCreateRequest() with { Location = PickedGhent }, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*unavailable*");
    }

    [Fact]
    public async Task UpdateAsync_UnchangedVerifiedAddress_IsKept_WithoutCallingGoogle()
    {
        var verifiedAt = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        var solicitation = NewSolicitation();
        solicitation.UpdateDetails("Dev", VerifiedGhent(verifiedAt), solicitation.Website, solicitation.Contact);
        _repository.GetByIdAsync(solicitation.Id, _ownerId, Arg.Any<CancellationToken>()).Returns(solicitation);

        var request = NewUpdateRequest() with { Location = PickedGhent with { City = "gent " } };
        await _sut.UpdateAsync(solicitation.Id, request, CancellationToken.None);

        _places.ReceivedCalls().Should().BeEmpty();
        solicitation.Location.VerifiedAtUtc.Should().Be(verifiedAt);
    }

    [Fact]
    public async Task UpdateAsync_SwitchingToFreeText_DropsPlaceIdAndCoordinates()
    {
        var solicitation = NewSolicitation();
        solicitation.UpdateDetails("Dev", VerifiedGhent(DateTime.UtcNow), solicitation.Website, solicitation.Contact);
        _repository.GetByIdAsync(solicitation.Id, _ownerId, Arg.Any<CancellationToken>()).Returns(solicitation);

        var request = NewUpdateRequest() with { Location = PickedGhent with { Validated = false } };
        await _sut.UpdateAsync(solicitation.Id, request, CancellationToken.None);

        solicitation.Location.IsVerified.Should().BeFalse();
        solicitation.Location.PlaceId.Should().BeNull();
        solicitation.Location.Latitude.Should().BeNull();
    }

    // ---------- Builders ----------

    private static UpdateMeetingRequest NewUpdateMeetingRequest() =>
        new(DateTime.UtcNow, DateTime.UtcNow.AddHours(1), MeetingType.PhoneScreen, IsOnline: false, OnlineTool: null);


    private static CreateSolicitationRequest NewCreateRequest(string jobName = "Developer") => new(
        jobName,
        new LocationDto("BE", "Brussels", "1000", "Main", "10"),
        new WebsiteDto("Indeed", "https://indeed.com"),
        new ContactDto("John", "0123", "john@example.com"));

    private static UpdateSolicitationRequest NewUpdateRequest(string jobName = "Developer") => new(
        jobName,
        new LocationDto("BE", "Brussels", "1000", "Main", "10"),
        new WebsiteDto("Indeed", "https://indeed.com"),
        new ContactDto("John", "0123", "john@example.com"),
        SolicitationStatus.Applied);

    private Solicitation NewSolicitation(string jobName = "Developer") => new(
        _ownerId,
        jobName,
        Location.Unverified("BE", "Brussels", "1000", "Main", "10"),
        new Website("Indeed", "https://indeed.com"),
        new Contact("John", "0123", "john@example.com"));
}
