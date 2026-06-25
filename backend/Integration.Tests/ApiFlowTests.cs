using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Integration.Tests;

public class ApiFlowTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Login_WithSeededAdmin_ReturnsToken()
    {
        var token = await LoginAsync("admin", ApiFactory.AdminPassword);
        token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Solicitations_WithoutToken_ReturnsUnauthorized()
    {
        var res = await _client.GetAsync("/api/solicitations");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Admin_CanCreateAndFetchSolicitation()
    {
        var token = await LoginAsync("admin", ApiFactory.AdminPassword);

        using var createReq = Authorized(HttpMethod.Post, "/api/solicitations", token, NewSolicitation("Integration Job"));
        var create = await _client.SendAsync(createReq);
        create.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await create.Content.ReadFromJsonAsync<SolicitationDto>();
        created!.JobName.Should().Be("Integration Job");

        using var getReq = Authorized(HttpMethod.Get, $"/api/solicitations/{created.Id}", token);
        var get = await _client.SendAsync(getReq);
        get.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task InviteRegisterLogin_NewUserIsIsolatedFromOthersData()
    {
        var adminToken = await LoginAsync("admin", ApiFactory.AdminPassword);

        // admin owns a solicitation
        using var createReq = Authorized(HttpMethod.Post, "/api/solicitations", adminToken, NewSolicitation("Admin Owned"));
        (await _client.SendAsync(createReq)).EnsureSuccessStatusCode();

        // admin issues a Member invite
        using var inviteReq = Authorized(HttpMethod.Post, "/api/invites", adminToken, new { role = "Member" });
        var inviteRes = await _client.SendAsync(inviteReq);
        inviteRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var invite = await inviteRes.Content.ReadFromJsonAsync<CreatedInviteDto>();

        // a brand-new user registers with that invite
        var username = $"friend-{Guid.NewGuid():N}";
        const string password = "Friend#Test12345";
        var register = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            token = invite!.Token,
            username,
            email = $"{username}@example.com",
            password
        });
        register.StatusCode.Should().Be(HttpStatusCode.Created);

        // and sees none of the admin's solicitations
        var friendToken = await LoginAsync(username, password);
        using var listReq = Authorized(HttpMethod.Get, "/api/solicitations", friendToken);
        var page = await (await _client.SendAsync(listReq)).Content.ReadFromJsonAsync<PagedDto>();
        page!.Items.Should().BeEmpty();
        page.TotalCount.Should().Be(0);
    }

    // ---------- helpers ----------

    private async Task<string> LoginAsync(string username, string password)
    {
        var res = await _client.PostAsJsonAsync("/api/auth/login", new { username, password });
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<LoginDto>();
        return body!.Token;
    }

    private static HttpRequestMessage Authorized(HttpMethod method, string url, string token, object? body = null)
    {
        var req = new HttpRequestMessage(method, url);
        req.Headers.Authorization = new("Bearer", token);
        if (body is not null) req.Content = JsonContent.Create(body);
        return req;
    }

    private static object NewSolicitation(string jobName) => new
    {
        jobName,
        location = new { country = "BE", city = "Gent", zipCode = "9000", street = "X", streetNumber = "1" },
        website = new { name = "LI", link = "https://li.com" },
        contact = new { name = "A", phoneNumber = "1", email = "a@a.com" }
    };

    private sealed record LoginDto(string Token, DateTime ExpiresAt, string RefreshToken);
    private sealed record SolicitationDto(Guid Id, string JobName);
    private sealed record CreatedInviteDto(Guid Id, string Token);
    private sealed record PagedDto(List<SolicitationDto> Items, int TotalCount);
}
