using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using App.DTO.v1.Profile;
using CarServiceTrack.Tests.Integration.Fixtures;
using CarServiceTrack.Tests.Integration.Helpers;
using FluentAssertions;

namespace CarServiceTrack.Tests.Integration.Controllers;

/// <summary>
/// Integration tests for <c>GET/PUT /api/v1/profile</c>.
/// Covers: happy paths, unauthenticated → 401, lazy Owner creation for a user with none
/// (admin), and that the whitelist keeps Email untouched by PUT.
/// </summary>
[Collection("Integration")]
public class ProfileControllerTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory = new();
    private HttpClient _client = null!;
    private SeedResult _seed = null!;

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        _seed = await _factory.InitializeDbAsync(TestDataSeeder.SeedAsync);
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task GetProfile_WithoutToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync("/api/v1/profile");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProfile_AsClientUserA_ReturnsOwnEmailAndName()
    {
        SetBearerToken(TestJwtHelper.ClientBearer(_seed.UserAId, _seed.UserAEmail));

        var response = await _client.GetAsync("/api/v1/profile");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<ProfileDto>();
        profile.Should().NotBeNull();
        profile!.Email.Should().Be(_seed.UserAEmail);
        profile.FirstName.Should().Be("Alice");
        profile.LastName.Should().Be("A");
    }

    [Fact]
    public async Task GetProfile_AsAdminWithNoOwnerYet_LazilyCreatesProfileAndReturns200()
    {
        SetBearerToken(TestJwtHelper.AdminBearer(_seed.AdminId, _seed.AdminEmail));

        var response = await _client.GetAsync("/api/v1/profile");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<ProfileDto>();
        profile!.Email.Should().Be(_seed.AdminEmail);
        profile.AppUserId.Should().Be(_seed.AdminId);
    }

    [Fact]
    public async Task UpdateProfile_AsClientUserA_UpdatesWhitelistedFieldsAndReturnsNewJwt()
    {
        SetBearerToken(TestJwtHelper.ClientBearer(_seed.UserAId, _seed.UserAEmail));
        var dto = new ProfileUpdateDto
        {
            FirstName = "Alicia",
            LastName = "Anderson",
            Address = "123 Main St",
            Phone = "+372 555 0000"
        };

        var response = await _client.PutAsJsonAsync("/api/v1/profile", dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ProfileUpdateResponseDto>();
        body.Should().NotBeNull();
        body!.Profile.FirstName.Should().Be("Alicia");
        body.Profile.LastName.Should().Be("Anderson");
        body.Profile.Address.Should().Be("123 Main St");
        body.Profile.Phone.Should().Be("+372 555 0000");
        body.Profile.Email.Should().Be(_seed.UserAEmail); // untouched by the whitelist
        body.Jwt.Should().NotBeNullOrWhiteSpace();

        // A follow-up GET reflects the persisted change.
        var getResponse = await _client.GetAsync("/api/v1/profile");
        var getProfile = await getResponse.Content.ReadFromJsonAsync<ProfileDto>();
        getProfile!.FirstName.Should().Be("Alicia");
    }

    [Fact]
    public async Task UpdateProfile_WithoutToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var dto = new ProfileUpdateDto { FirstName = "X", LastName = "Y" };

        var response = await _client.PutAsJsonAsync("/api/v1/profile", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateProfile_WithEmptyFirstName_Returns400()
    {
        SetBearerToken(TestJwtHelper.ClientBearer(_seed.UserAId, _seed.UserAEmail));
        var dto = new ProfileUpdateDto { FirstName = "", LastName = "Y" };

        var response = await _client.PutAsJsonAsync("/api/v1/profile", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateProfile_AsClientUserA_DoesNotAffectUserB()
    {
        SetBearerToken(TestJwtHelper.ClientBearer(_seed.UserAId, _seed.UserAEmail));
        var dto = new ProfileUpdateDto { FirstName = "Alicia", LastName = "Anderson" };
        await _client.PutAsJsonAsync("/api/v1/profile", dto);

        SetBearerToken(TestJwtHelper.ClientBearer(_seed.UserBId, _seed.UserBEmail));
        var response = await _client.GetAsync("/api/v1/profile");

        var profile = await response.Content.ReadFromJsonAsync<ProfileDto>();
        profile!.FirstName.Should().Be("Bob");
        profile.LastName.Should().Be("B");
    }

    private void SetBearerToken(string bearerValue) =>
        _client.DefaultRequestHeaders.Authorization =
            AuthenticationHeaderValue.Parse(bearerValue);
}
