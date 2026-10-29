using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.Tests;

public class ApiFlowTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Login_WithSeededAdmin_ReturnsToken()
    {
        var token = await AdminTokenAsync();
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
        var token = await AdminTokenAsync();

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
        var adminToken = await AdminTokenAsync();

        // admin owns a solicitation
        using var createReq = Authorized(HttpMethod.Post, "/api/solicitations", adminToken, NewSolicitation("Admin Owned"));
        (await _client.SendAsync(createReq)).EnsureSuccessStatusCode();

        // a brand-new user registers through an emailed invite
        var (username, password) = await RegisterNewUserAsync();

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
        var secret = SecretFrom(setup!.AuthenticatorUri);

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

    [Fact]
    public async Task Invite_ByEmail_SendsLink_AndOnlyThatEmailCanRegister()
    {
        var adminToken = await AdminTokenAsync();
        var email = $"invitee-{Guid.NewGuid():N}@example.com";

        using var inviteReq = Authorized(HttpMethod.Post, "/api/invites", adminToken, new { email });
        var inviteRes = await _client.SendAsync(inviteReq);
        inviteRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var invite = (await inviteRes.Content.ReadFromJsonAsync<CreatedInviteDto>())!;
        invite.EmailSent.Should().BeTrue();

        // the email contains the same register link the admin sees
        var mail = factory.Emails.Sent.Single(m => m.To == email);
        mail.TextBody.Should().Contain(invite.Link);
        var token = TokenFrom(invite.Link);

        // a second invite for the same address is refused
        using var duplicate = Authorized(HttpMethod.Post, "/api/invites", adminToken, new { email });
        (await _client.SendAsync(duplicate)).StatusCode.Should().Be(HttpStatusCode.Conflict);

        // the register page can look the invite up anonymously
        var lookup = await _client.GetFromJsonAsync<InviteLookupDto>($"/api/auth/invites/{token}");
        lookup!.Email.Should().Be(email);

        var username = $"invitee-{Guid.NewGuid():N}";
        const string password = "Invitee#Test12345";
        (await _client.PostAsJsonAsync("/api/auth/register",
                new { token, username, email = "someone-else@example.com", password }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await _client.PostAsJsonAsync("/api/auth/register", new { token, username, email, password }))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        // invited users are plain members: they can't invite
        var memberToken = await LoginAsync(username, password);
        using var memberInvite = Authorized(HttpMethod.Post, "/api/invites", memberToken, new { email = "x@example.com" });
        (await _client.SendAsync(memberInvite)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminWithoutTwoFactor_CanListButNotManageUsers()
    {
        var adminToken = await AdminTokenAsync();
        var (username, password) = await RegisterNewUserAsync();
        var userId = await UserIdAsync(adminToken, username);

        using var promote = Authorized(HttpMethod.Post, $"/api/users/{userId}/roles", adminToken, new { role = "Admin" });
        (await _client.SendAsync(promote)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        // new admin, no 2FA yet (roles are read at login)
        var newAdminToken = await LoginAsync(username, password);
        using var list = Authorized(HttpMethod.Get, "/api/users", newAdminToken);
        (await _client.SendAsync(list)).StatusCode.Should().Be(HttpStatusCode.OK);

        using var invite = Authorized(HttpMethod.Post, "/api/invites", newAdminToken, new { email = $"{Guid.NewGuid():N}@example.com" });
        var res = await _client.SendAsync(invite);
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("two-factor");
    }

    [Fact]
    public async Task DeleteUser_RequiresUsernameAndCode_AndRemovesTheirData()
    {
        var adminToken = await AdminTokenAsync();
        var (username, password) = await RegisterNewUserAsync();
        var userToken = await LoginAsync(username, password);
        using var create = Authorized(HttpMethod.Post, "/api/solicitations", userToken, NewSolicitation("Doomed"));
        (await _client.SendAsync(create)).EnsureSuccessStatusCode();
        var userId = await UserIdAsync(adminToken, username);

        using var wrongName = Authorized(HttpMethod.Post, $"/api/users/{userId}/delete", adminToken,
            new { username = "not-them", code = Totp.Compute(factory.AdminTotpSecret!) });
        (await _client.SendAsync(wrongName)).StatusCode.Should().Be(HttpStatusCode.Conflict);

        using var wrongCode = Authorized(HttpMethod.Post, $"/api/users/{userId}/delete", adminToken,
            new { username, code = "000000" });
        (await _client.SendAsync(wrongCode)).StatusCode.Should().Be(HttpStatusCode.Conflict);

        using var delete = Authorized(HttpMethod.Post, $"/api/users/{userId}/delete", adminToken,
            new { username, code = Totp.Compute(factory.AdminTotpSecret!) });
        (await _client.SendAsync(delete)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await _client.PostAsJsonAsync("/api/auth/login", new { username, password }))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SolicitationsDbContext>();
        (await db.Solicitations.CountAsync(s => s.OwnerId == userId)).Should().Be(0);
    }

    // ---------- helpers ----------

    private async Task<(string Username, string Password)> RegisterNewUserAsync()
    {
        var adminToken = await AdminTokenAsync();
        var username = $"user-{Guid.NewGuid():N}";
        var email = $"{username}@example.com";
        using var inviteReq = Authorized(HttpMethod.Post, "/api/invites", adminToken, new { email });
        var inviteRes = await _client.SendAsync(inviteReq);
        inviteRes.EnsureSuccessStatusCode();
        var invite = await inviteRes.Content.ReadFromJsonAsync<CreatedInviteDto>();

        const string password = "Member#Test12345";
        (await _client.PostAsJsonAsync("/api/auth/register", new
        {
            token = TokenFrom(invite!.Link),
            username,
            email,
            password
        })).EnsureSuccessStatusCode();
        return (username, password);
    }

    /// <summary>Signs in as the seeded admin, enabling 2FA on first use (admin actions require it).</summary>
    private async Task<string> AdminTokenAsync()
    {
        if (factory.AdminTotpSecret is null)
        {
            var token = await LoginAsync("admin", ApiFactory.AdminPassword);
            using var setupReq = Authorized(HttpMethod.Post, "/api/account/2fa/setup", token);
            var setup = await (await _client.SendAsync(setupReq)).Content.ReadFromJsonAsync<SetupDto>();
            var secret = SecretFrom(setup!.AuthenticatorUri);
            using var enableReq = Authorized(HttpMethod.Post, "/api/account/2fa/enable", token, new { code = Totp.Compute(secret) });
            (await _client.SendAsync(enableReq)).EnsureSuccessStatusCode();
            factory.AdminTotpSecret = secret;
            return token;
        }

        var login = await (await _client.PostAsJsonAsync("/api/auth/login",
            new { username = "admin", password = ApiFactory.AdminPassword })).Content.ReadFromJsonAsync<ChallengeDto>();
        var res = await _client.PostAsJsonAsync("/api/auth/login/2fa",
            new { challengeToken = login!.ChallengeToken, code = Totp.Compute(factory.AdminTotpSecret), useRecoveryCode = false });
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<LoginDto>())!.Token;
    }

    private async Task<Guid> UserIdAsync(string adminToken, string username)
    {
        using var req = Authorized(HttpMethod.Get, "/api/users", adminToken);
        var users = await (await _client.SendAsync(req)).Content.ReadFromJsonAsync<List<UserDto>>();
        return users!.Single(u => u.Username == username).Id;
    }

    private static string TokenFrom(string link) => link[(link.IndexOf("token=", StringComparison.Ordinal) + "token=".Length)..];

    private static string SecretFrom(string authenticatorUri) => new Uri(authenticatorUri).Query.TrimStart('?').Split('&')
        .Single(p => p.StartsWith("secret=")).Substring("secret=".Length);

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
    private sealed record CreatedInviteDto(Guid Id, string Link, DateTime ExpiresAtUtc, bool EmailSent);
    private sealed record InviteLookupDto(string Email, DateTime ExpiresAtUtc);
    private sealed record UserDto(Guid Id, string Username);
    private sealed record PagedDto(List<SolicitationDto> Items, int TotalCount);
    private sealed record SetupDto(string SharedKey, string AuthenticatorUri);
    private sealed record RecoveryCodesDto(List<string> RecoveryCodes);
    private sealed record ProfileDto(string Username, bool TwoFactorEnabled, int RecoveryCodesLeft);
    private sealed record ChallengeDto(bool RequiresTwoFactor, string ChallengeToken);
}
