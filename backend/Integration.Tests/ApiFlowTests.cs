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

    [Fact]
    public async Task TwoFactor_EnableThenLogin_RequiresAuthenticatorOrRecoveryCode()
    {
        var (username, password) = await RegisterNewUserAsync();
        var token = await LoginAsync(username, password);

        // setup returns a key + otpauth URI for the authenticator app
        using var setupReq = Authorized(HttpMethod.Post, "/api/account/2fa/setup", token);
        var setup = await (await _client.SendAsync(setupReq)).Content.ReadFromJsonAsync<SetupDto>();
        var secret = new Uri(setup!.AuthenticatorUri).Query.TrimStart('?').Split('&')
            .Single(p => p.StartsWith("secret=")).Substring("secret=".Length);

        // a wrong code does not enable 2FA
        using var badEnable = Authorized(HttpMethod.Post, "/api/account/2fa/enable", token, new { code = "000000" });
        (await _client.SendAsync(badEnable)).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var enableReq = Authorized(HttpMethod.Post, "/api/account/2fa/enable", token, new { code = Totp.Compute(secret) });
        var enable = await _client.SendAsync(enableReq);
        enable.StatusCode.Should().Be(HttpStatusCode.OK);
        var recoveryCodes = (await enable.Content.ReadFromJsonAsync<RecoveryCodesDto>())!.RecoveryCodes;
        recoveryCodes.Should().HaveCount(10);

        using var profileReq = Authorized(HttpMethod.Get, "/api/account", token);
        var profile = await (await _client.SendAsync(profileReq)).Content.ReadFromJsonAsync<ProfileDto>();
        profile!.TwoFactorEnabled.Should().BeTrue();

        // password alone now only yields a challenge, not a session
        var login = await (await _client.PostAsJsonAsync("/api/auth/login", new { username, password }))
            .Content.ReadFromJsonAsync<ChallengeDto>();
        login!.RequiresTwoFactor.Should().BeTrue();

        // the challenge is not an access token
        using var challengeAsBearer = Authorized(HttpMethod.Get, "/api/account", login.ChallengeToken);
        (await _client.SendAsync(challengeAsBearer)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var wrong = await _client.PostAsJsonAsync("/api/auth/login/2fa",
            new { challengeToken = login.ChallengeToken, code = "000000", useRecoveryCode = false });
        wrong.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var right = await _client.PostAsJsonAsync("/api/auth/login/2fa",
            new { challengeToken = login.ChallengeToken, code = Totp.Compute(secret), useRecoveryCode = false });
        right.StatusCode.Should().Be(HttpStatusCode.OK);
        (await right.Content.ReadFromJsonAsync<LoginDto>())!.Token.Should().NotBeNullOrWhiteSpace();

        // a recovery code works once (input is case-insensitive)
        var recovery = new { challengeToken = login.ChallengeToken, code = recoveryCodes[0].ToLowerInvariant(), useRecoveryCode = true };
        (await _client.PostAsJsonAsync("/api/auth/login/2fa", recovery)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await _client.PostAsJsonAsync("/api/auth/login/2fa", recovery)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangePassword_RequiresCurrentPassword_AndReplacesIt()
    {
        var (username, password) = await RegisterNewUserAsync();
        var token = await LoginAsync(username, password);
        const string newPassword = "Changed#Test12345";

        using var wrongReq = Authorized(HttpMethod.Post, "/api/account/password", token,
            new { currentPassword = "Nope#Test123456", newPassword });
        (await _client.SendAsync(wrongReq)).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var changeReq = Authorized(HttpMethod.Post, "/api/account/password", token,
            new { currentPassword = password, newPassword });
        var change = await _client.SendAsync(changeReq);
        change.StatusCode.Should().Be(HttpStatusCode.OK);
        (await change.Content.ReadFromJsonAsync<LoginDto>())!.Token.Should().NotBeNullOrWhiteSpace();

        (await _client.PostAsJsonAsync("/api/auth/login", new { username, password }))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await LoginAsync(username, newPassword)).Should().NotBeNullOrWhiteSpace();
    }

    // ---------- helpers ----------

    private async Task<(string Username, string Password)> RegisterNewUserAsync()
    {
        var adminToken = await LoginAsync("admin", ApiFactory.AdminPassword);
        using var inviteReq = Authorized(HttpMethod.Post, "/api/invites", adminToken, new { role = "Member" });
        var invite = await (await _client.SendAsync(inviteReq)).Content.ReadFromJsonAsync<CreatedInviteDto>();

        var username = $"user-{Guid.NewGuid():N}";
        const string password = "Member#Test12345";
        (await _client.PostAsJsonAsync("/api/auth/register", new
        {
            token = invite!.Token,
            username,
            email = $"{username}@example.com",
            password
        })).EnsureSuccessStatusCode();
        return (username, password);
    }

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
    private sealed record SetupDto(string SharedKey, string AuthenticatorUri);
    private sealed record RecoveryCodesDto(List<string> RecoveryCodes);
    private sealed record ProfileDto(string Username, bool TwoFactorEnabled, int RecoveryCodesLeft);
    private sealed record ChallengeDto(bool RequiresTwoFactor, string ChallengeToken);
}
