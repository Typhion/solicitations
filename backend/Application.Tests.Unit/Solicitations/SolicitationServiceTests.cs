using Application.Common.Exceptions;
using Application.Solicitations;
using Domain.Solicitation;
using FluentAssertions;
using NSubstitute;

namespace Application.Tests.Unit.Solicitations;

public class SolicitationServiceTests
{
    private readonly ISolicitationRepository _repository = Substitute.For<ISolicitationRepository>();
    private readonly SolicitationService _sut;

    public SolicitationServiceTests() => _sut = new SolicitationService(_repository);

    // ---------- Create ----------

    [Fact]
    public async Task CreateAsync_AddsEntity_Saves_AndReturnsMappedResponse()
    {
        var request = NewCreateRequest("Backend Developer");

        var response = await _sut.CreateAsync(request, CancellationToken.None);

        _repository.Received(1).Add(Arg.Any<Solicitation>());
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
    public async Task GetAsync_WhenFound_ReturnsMappedResponse()
    {
        var solicitation = NewSolicitation("Platform Engineer");
        _repository.GetByIdAsync(solicitation.Id, Arg.Any<CancellationToken>()).Returns(solicitation);

        var response = await _sut.GetAsync(solicitation.Id, CancellationToken.None);

        response.Id.Should().Be(solicitation.Id);
        response.JobName.Should().Be("Platform Engineer");
    }

    [Fact]
    public async Task GetAsync_WhenMissing_ThrowsNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Solicitation?)null);

        var act = () => _sut.GetAsync(Guid.NewGuid(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ---------- List ----------

    [Fact]
    public async Task ListAsync_ReturnsAllMappedResponses()
    {
        var items = new List<Solicitation> { NewSolicitation("One"), NewSolicitation("Two") };
        _repository.ListAsync(Arg.Any<CancellationToken>()).Returns(items);

        var responses = await _sut.ListAsync(CancellationToken.None);

        responses.Should().HaveCount(2);
        responses.Select(r => r.JobName).Should().Equal("One", "Two");
    }

    // ---------- Update ----------

    [Fact]
    public async Task UpdateAsync_WhenFound_MutatesTrackedEntity_AndSaves()
    {
        var solicitation = NewSolicitation("Old Title");
        _repository.GetByIdAsync(solicitation.Id, Arg.Any<CancellationToken>()).Returns(solicitation);

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
    public async Task UpdateAsync_WhenMissing_ThrowsNotFound_AndDoesNotSave()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Solicitation?)null);

        var act = () => _sut.UpdateAsync(Guid.NewGuid(), NewUpdateRequest(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ---------- Delete ----------

    [Fact]
    public async Task DeleteAsync_WhenFound_RemovesEntity_AndSaves()
    {
        var solicitation = NewSolicitation();
        _repository.GetByIdAsync(solicitation.Id, Arg.Any<CancellationToken>()).Returns(solicitation);

        await _sut.DeleteAsync(solicitation.Id, CancellationToken.None);

        _repository.Received(1).Remove(solicitation);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_WhenMissing_ThrowsNotFound_AndDoesNotRemoveOrSave()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Solicitation?)null);

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

    private static Solicitation NewSolicitation(string jobName = "Developer") => new(
        jobName,
        new Location("BE", "Brussels", "1000", "Main", "10"),
        new Website("Indeed", "https://indeed.com"),
        new Contact("John", "0123", "john@example.com"));
}
