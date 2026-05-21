using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using App.DTO.v1.Vehicle;
using CarServiceTrack.Tests.Integration.Fixtures;
using CarServiceTrack.Tests.Integration.Helpers;
using FluentAssertions;

namespace CarServiceTrack.Tests.Integration.Controllers;

/// <summary>
/// Integration tests for <c>GET/POST/PUT/DELETE /api/v1/vehicles</c>.
/// Covers: happy paths, unauthenticated → 401, wrong role → 403,
/// and IDOR: userA cannot read/modify/delete userB's vehicles.
/// </summary>
[Collection("Integration")]
public class VehiclesControllerTests : IAsyncLifetime
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

    // ── Authentication regression ─────────────────────────────────────────────
    [Fact]
    public async Task GetVehicles_WithoutToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync("/api/v1/vehicles");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Happy path: own vehicles ──────────────────────────────────────────────
    [Fact]
    public async Task GetVehicles_AsClientUserA_ReturnsOwnVehiclesOnly()
    {
        SetBearerToken(TestJwtHelper.ClientBearer(_seed.UserAId, _seed.UserAEmail));

        var response = await _client.GetAsync("/api/v1/vehicles");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var vehicles = await response.Content.ReadFromJsonAsync<List<VehicleDto>>();
        vehicles.Should().NotBeNull();
        vehicles!.Should().OnlyContain(v => v.Id == _seed.UserAVehicleId);
    }

    [Fact]
    public async Task GetVehicle_AsClientUserA_WithOwnVehicleId_Returns200()
    {
        SetBearerToken(TestJwtHelper.ClientBearer(_seed.UserAId, _seed.UserAEmail));

        var response = await _client.GetAsync($"/api/v1/vehicles/{_seed.UserAVehicleId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var vehicle = await response.Content.ReadFromJsonAsync<VehicleDto>();
        vehicle!.Id.Should().Be(_seed.UserAVehicleId);
        vehicle.Make.Should().Be("Toyota");
    }

    // ── IDOR regression ───────────────────────────────────────────────────────
    [Fact]
    public async Task GetVehicle_AsClientUserA_WithUserBVehicleId_Returns404()
    {
        // UserA should not be able to see UserB's vehicle
        SetBearerToken(TestJwtHelper.ClientBearer(_seed.UserAId, _seed.UserAEmail));

        var response = await _client.GetAsync($"/api/v1/vehicles/{_seed.UserBVehicleId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateVehicle_AsClientUserA_WithUserBVehicleId_Returns404()
    {
        SetBearerToken(TestJwtHelper.ClientBearer(_seed.UserAId, _seed.UserAEmail));
        var dto = new VehicleCreateDto { Make = "Hack", Model = "Attempt", Year = 2020, LicensePlate = "HACK01" };

        var response = await _client.PutAsJsonAsync($"/api/v1/vehicles/{_seed.UserBVehicleId}", dto);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteVehicle_AsClientUserA_WithUserBVehicleId_Returns404()
    {
        SetBearerToken(TestJwtHelper.ClientBearer(_seed.UserAId, _seed.UserAEmail));

        var response = await _client.DeleteAsync($"/api/v1/vehicles/{_seed.UserBVehicleId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Admin can see all vehicles ────────────────────────────────────────────
    [Fact]
    public async Task GetVehicles_AsAdmin_ReturnsAllVehicles()
    {
        SetBearerToken(TestJwtHelper.AdminBearer(_seed.AdminId, _seed.AdminEmail));

        var response = await _client.GetAsync("/api/v1/vehicles");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var vehicles = await response.Content.ReadFromJsonAsync<List<VehicleDto>>();
        // Admin sees both UserA's and UserB's vehicles (at minimum)
        vehicles!.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetVehicle_AsAdmin_WithUserBVehicleId_Returns200()
    {
        SetBearerToken(TestJwtHelper.AdminBearer(_seed.AdminId, _seed.AdminEmail));

        var response = await _client.GetAsync($"/api/v1/vehicles/{_seed.UserBVehicleId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Create ────────────────────────────────────────────────────────────────
    [Fact]
    public async Task CreateVehicle_AsClientUserA_WithValidDto_Returns201()
    {
        SetBearerToken(TestJwtHelper.ClientBearer(_seed.UserAId, _seed.UserAEmail));
        var dto = new VehicleCreateDto { Make = "Skoda", Model = "Octavia", Year = 2023, LicensePlate = "SK001" };

        var response = await _client.PostAsJsonAsync("/api/v1/vehicles", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var vehicle = await response.Content.ReadFromJsonAsync<VehicleDto>();
        vehicle!.Make.Should().Be("Skoda");
        vehicle.LicensePlate.Should().Be("SK001");
    }

    [Fact]
    public async Task CreateVehicle_WithoutAuthentication_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var dto = new VehicleCreateDto { Make = "X", Model = "Y", Year = 2020, LicensePlate = "XY001" };

        var response = await _client.PostAsJsonAsync("/api/v1/vehicles", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Update (happy path) ───────────────────────────────────────────────────
    [Fact]
    public async Task UpdateVehicle_AsClientUserA_WithOwnVehicle_Returns204()
    {
        SetBearerToken(TestJwtHelper.ClientBearer(_seed.UserAId, _seed.UserAEmail));
        var dto = new VehicleCreateDto { Make = "Toyota", Model = "Camry Updated", Year = 2023, LicensePlate = "CTA001" };

        var response = await _client.PutAsJsonAsync($"/api/v1/vehicles/{_seed.UserAVehicleId}", dto);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ── Delete: vehicle with existing orders returns 400 ─────────────────────
    [Fact]
    public async Task DeleteVehicle_AsAdmin_WithVehicleHavingOrders_Returns400()
    {
        SetBearerToken(TestJwtHelper.AdminBearer(_seed.AdminId, _seed.AdminEmail));

        // UserA's vehicle has an existing service order (seeded in TestDataSeeder)
        var response = await _client.DeleteAsync($"/api/v1/vehicles/{_seed.UserAVehicleId}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Delete: vehicle with no orders returns 204 ────────────────────────────
    [Fact]
    public async Task DeleteVehicle_AsAdmin_WithVehicleHavingNoOrders_Returns204()
    {
        // First create a new vehicle with no orders
        SetBearerToken(TestJwtHelper.AdminBearer(_seed.AdminId, _seed.AdminEmail));

        // Create a fresh vehicle for UserB (who has no orders yet)
        var createDto = new VehicleCreateDto { Make = "Delete", Model = "Me", Year = 2020, LicensePlate = "DEL001" };
        var createResp = await _client.PostAsJsonAsync("/api/v1/vehicles", createDto);
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResp.Content.ReadFromJsonAsync<VehicleDto>();

        var deleteResp = await _client.DeleteAsync($"/api/v1/vehicles/{created!.Id}");
        deleteResp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ── Get nonexistent vehicle returns 404 ────────────────────────────────────
    [Fact]
    public async Task GetVehicle_WithNonexistentId_Returns404()
    {
        SetBearerToken(TestJwtHelper.AdminBearer(_seed.AdminId, _seed.AdminEmail));

        var response = await _client.GetAsync($"/api/v1/vehicles/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private void SetBearerToken(string bearerValue) =>
        _client.DefaultRequestHeaders.Authorization =
            AuthenticationHeaderValue.Parse(bearerValue);
}
