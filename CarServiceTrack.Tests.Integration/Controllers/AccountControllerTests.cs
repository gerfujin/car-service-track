using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using App.DTO.v1.Identity;
using CarServiceTrack.Tests.Integration.Fixtures;
using FluentAssertions;

namespace CarServiceTrack.Tests.Integration.Controllers;

/// <summary>
/// Integration tests for <c>/api/v1/identity/account/*</c> endpoints.
/// Tests register, login, refresh-token and logout flows.
/// </summary>
[Collection("Integration")]
public class AccountControllerTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory = new();
    private HttpClient _client = null!;
    private SeedResult _seed = null!;

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        _seed = await _factory.InitializeDbAsync(TestDataSeeder.SeedAsync);
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    // ── Register ──────────────────────────────────────────────────────────────
    [Fact]
    public async Task Register_WithNewEmail_Returns200WithJwtAndRefreshToken()
    {
        var dto = new RegisterInfo { Email = "newuser@test.com", Password = "Test123!", Firstname = "New", Lastname = "User" };

        var response = await _client.PostAsJsonAsync("/api/v1/identity/account/register", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JWTResponse>();
        body.Should().NotBeNull();
        body!.Jwt.Should().NotBeNullOrWhiteSpace();
        body.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Register_WithAlreadyRegisteredEmail_Returns400()
    {
        var dto = new RegisterInfo { Email = _seed.UserAEmail, Password = "Test123!", Firstname = "Alice", Lastname = "A" };

        var response = await _client.PostAsJsonAsync("/api/v1/identity/account/register", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithFirstAndLastName_IssuesJwtCarryingNameClaims()
    {
        var dto = new RegisterInfo
            { Email = "newuser2@test.com", Password = "Test123!", Firstname = "New", Lastname = "Person" };

        var response = await _client.PostAsJsonAsync("/api/v1/identity/account/register", dto);
        var body = await response.Content.ReadFromJsonAsync<JWTResponse>();

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(body!.Jwt);
        jwt.Claims.First(c => c.Type == ClaimTypes.GivenName).Value.Should().Be("New");
        jwt.Claims.First(c => c.Type == ClaimTypes.Surname).Value.Should().Be("Person");
    }

    // ── Login ─────────────────────────────────────────────────────────────────
    [Fact]
    public async Task Login_WithValidCredentials_Returns200WithJwtAndRefreshToken()
    {
        var dto = new LoginInfo { Email = _seed.UserAEmail, Password = "Test123!" };

        var response = await _client.PostAsJsonAsync("/api/v1/identity/account/login", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JWTResponse>();
        body!.Jwt.Should().NotBeNullOrWhiteSpace();
        body.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_ForSeededOwner_IssuesJwtCarryingNameClaims()
    {
        var dto = new LoginInfo { Email = _seed.UserAEmail, Password = "Test123!" };

        var response = await _client.PostAsJsonAsync("/api/v1/identity/account/login", dto);
        var body = await response.Content.ReadFromJsonAsync<JWTResponse>();

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(body!.Jwt);
        jwt.Claims.First(c => c.Type == ClaimTypes.GivenName).Value.Should().Be("Alice");
        jwt.Claims.First(c => c.Type == ClaimTypes.Surname).Value.Should().Be("A");
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns404()
    {
        var dto = new LoginInfo { Email = _seed.UserAEmail, Password = "WrongPassword!" };

        var response = await _client.PostAsJsonAsync("/api/v1/identity/account/login", dto);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Login_WithNonexistentEmail_Returns404()
    {
        var dto = new LoginInfo { Email = "nobody@test.com", Password = "Test123!" };

        var response = await _client.PostAsJsonAsync("/api/v1/identity/account/login", dto);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── RefreshToken ──────────────────────────────────────────────────────────
    [Fact]
    public async Task RefreshToken_WithValidTokenPair_Returns200WithNewTokens()
    {
        // Obtain a real JWT + refresh token by logging in
        var loginDto = new LoginInfo { Email = _seed.UserAEmail, Password = "Test123!" };
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/identity/account/login", loginDto);
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<JWTResponse>();
        loginBody.Should().NotBeNull();

        // Use the real refresh token to get new tokens
        var refreshDto = new TokenRefreshInfo { Jwt = loginBody!.Jwt, RefreshToken = loginBody.RefreshToken };
        var refreshResponse = await _client.PostAsJsonAsync("/api/v1/identity/account/refreshtokendata", refreshDto);

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshBody = await refreshResponse.Content.ReadFromJsonAsync<JWTResponse>();
        refreshBody!.Jwt.Should().NotBeNullOrWhiteSpace();
        // The refresh token must be rotated (new value)
        refreshBody.RefreshToken.Should().NotBe(loginBody.RefreshToken);
    }

    [Fact]
    public async Task RefreshToken_WithInvalidRefreshToken_Returns404()
    {
        var loginDto = new LoginInfo { Email = _seed.UserAEmail, Password = "Test123!" };
        var loginBody = await (await _client.PostAsJsonAsync("/api/v1/identity/account/login", loginDto))
            .Content.ReadFromJsonAsync<JWTResponse>();

        var refreshDto = new TokenRefreshInfo { Jwt = loginBody!.Jwt, RefreshToken = "totally-invalid-token" };
        var response = await _client.PostAsJsonAsync("/api/v1/identity/account/refreshtokendata", refreshDto);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Logout ────────────────────────────────────────────────────────────────
    [Fact]
    public async Task Logout_WithValidAuthenticatedUser_Returns200()
    {
        var loginDto = new LoginInfo { Email = _seed.UserAEmail, Password = "Test123!" };
        var loginBody = await (await _client.PostAsJsonAsync("/api/v1/identity/account/login", loginDto))
            .Content.ReadFromJsonAsync<JWTResponse>();

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginBody!.Jwt);

        var response = await _client.PostAsJsonAsync(
            "/api/v1/identity/account/logout",
            new LogoutInfo { RefreshToken = loginBody.RefreshToken });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Logout_WithoutAuthentication_Returns401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/identity/account/logout",
            new LogoutInfo { RefreshToken = "any" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
