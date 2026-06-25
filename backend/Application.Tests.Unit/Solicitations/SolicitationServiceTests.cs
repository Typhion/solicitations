using Application.Common;
using Application.Common.Exceptions;
using Application.Solicitations;
using Domain.Solicitation;
using FluentAssertions;
using NSubstitute;

namespace Application.Tests.Unit.Solicitations;

public class SolicitationServiceTests
{
    private readonly ISolicitationRepository _repository = Substitute.For<ISolicitationRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly SolicitationService _sut;

    public SolicitationServiceTests()
    {
        _currentUser.Id.Returns(_ownerId);
        _sut = new SolicitationService(_repository, _currentUser);
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

    // ---------- Builders ----------

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
        new Location("BE", "Brussels", "1000", "Main", "10"),
        new Website("Indeed", "https://indeed.com"),
        new Contact("John", "0123", "john@example.com"));
}
