using Application.Common;
using Application.Meetings;
using Application.Solicitations;
using Domain.Meeting;
using Domain.Solicitation;
using FluentAssertions;
using NSubstitute;

namespace Application.Tests.Unit.Meetings;

public class MeetingCalendarServiceTests
{
    private readonly ISolicitationRepository _repository = Substitute.For<ISolicitationRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly MeetingCalendarService _sut;

    public MeetingCalendarServiceTests()
    {
        _currentUser.Id.Returns(_ownerId);
        _sut = new MeetingCalendarService(_repository, _currentUser);
    }

    [Fact]
    public async Task ListAsync_ScopesToCurrentUser_AndPassesFilterThrough()
    {
        var solicitationId = Guid.NewGuid();
        var meeting = new CalendarMeetingDto(Guid.NewGuid(), solicitationId, "Dev", "Acme", SolicitationStatus.Interviewing,
            new DateTime(2026, 11, 3, 9, 0, 0, DateTimeKind.Utc), new DateTime(2026, 11, 3, 10, 0, 0, DateTimeKind.Utc),
            MeetingType.PhoneScreen, true, "Teams", "Ghent");
        _repository.ListMeetingsAsync(_ownerId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), solicitationId, Arg.Any<CancellationToken>())
            .Returns([meeting]);

        var result = await _sut.ListAsync(
            new MeetingCalendarQuery(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(7), solicitationId),
            CancellationToken.None);

        result.Should().ContainSingle().Which.Should().Be(meeting);
    }

    [Fact]
    public async Task ListAsync_ConvertsOffsetRangeToUtc()
    {
        // midnight in Brussels (UTC+1 in winter) is 23:00 UTC the day before
        var from = new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.FromHours(1));
        var to = from.AddDays(7);

        await _sut.ListAsync(new MeetingCalendarQuery(from, to, null), CancellationToken.None);

        await _repository.Received(1).ListMeetingsAsync(
            _ownerId,
            Arg.Is<DateTime>(d => d == new DateTime(2026, 10, 31, 23, 0, 0) && d.Kind == DateTimeKind.Utc),
            Arg.Is<DateTime>(d => d == new DateTime(2026, 11, 7, 23, 0, 0) && d.Kind == DateTimeKind.Utc),
            null,
            Arg.Any<CancellationToken>());
    }
}
