using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CarServiceTrack.Tests.Integration.Fixtures;
using CarServiceTrack.Tests.Integration.Helpers;
using FluentAssertions;

namespace CarServiceTrack.Tests.Integration.Controllers;

/// <summary>
/// Dedicated IDOR + role-enforcement regression tests.
/// Every scenario that could expose another user's data or allow privilege escalation
/// is collected here so they are clearly visible in the test report.
/// </summary>
[Collection("Integration")]
public class IborSecurityTests : IAsyncLifetime
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

    // ── No token → 401 on every protected endpoint ────────────────────────────

    [Fact]
    public async Task GetVehicles_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        (await _client.GetAsync("/api/v1/vehicles")).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetServiceOrders_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        (await _client.GetAsync("/api/v1/serviceorders")).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPayments_NoToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        (await _client.GetAsync("/api/v1/payments")).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── IDOR: UserA cannot read UserB's resources ─────────────────────────────

    [Fact]
    public async Task GetVehicle_UserAAccessesUserBVehicle_Returns404()
    {
        SetBearerToken(TestJwtHelper.ClientBearer(_seed.UserAId, _seed.UserAEmail));
        (await _client.GetAsync($"/api/v1/vehicles/{_seed.UserBVehicleId}")).StatusCode
            .Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetServiceOrder_UserAAccessesUserBOrder_Returns404()
    {
        // UserB has no order seeded, so we use the Admin's perspective for setup
        // and UserA trying to access UserA's own order from UserB's credentials.
        SetBearerToken(TestJwtHelper.ClientBearer(_seed.UserBId, _seed.UserBEmail));
        (await _client.GetAsync($"/api/v1/serviceorders/{_seed.UserAOrderId}")).StatusCode
            .Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPayment_UserAAccessesUserBPayment_Returns404()
    {
        // UserB tries to access UserA's payment
        SetBearerToken(TestJwtHelper.ClientBearer(_seed.UserBId, _seed.UserBEmail));
        (await _client.GetAsync($"/api/v1/payments/{_seed.UserAPaymentId}")).StatusCode
            .Should().Be(HttpStatusCode.NotFound);
    }

    // ── IDOR mutation: UserA cannot modify UserB's resources ──────────────────

    [Fact]
    public async Task UpdateVehicle_UserAAttemptsUserBVehicle_Returns404()
    {
        SetBearerToken(TestJwtHelper.ClientBearer(_seed.UserAId, _seed.UserAEmail));
        var dto = new { make = "Hacked", model = "Car", year = 2023, licensePlate = "HACK" };
        (await _client.PutAsJsonAsync($"/api/v1/vehicles/{_seed.UserBVehicleId}", dto)).StatusCode
            .Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteVehicle_UserAAttemptsUserBVehicle_Returns404()
    {
        SetBearerToken(TestJwtHelper.ClientBearer(_seed.UserAId, _seed.UserAEmail));
        (await _client.DeleteAsync($"/api/v1/vehicles/{_seed.UserBVehicleId}")).StatusCode
            .Should().Be(HttpStatusCode.NotFound);
    }

    // ── Role enforcement: client on admin-only endpoints → 403 ───────────────

    [Fact]
    public async Task CreatePayment_AsClientRole_Returns403()
    {
        SetBearerToken(TestJwtHelper.ClientBearer(_seed.UserAId, _seed.UserAEmail));
        var dto = new { serviceOrderId = _seed.UserAOrderId, amount = 50.0 };
        // POST /api/v1/payments is [Authorize(Roles = "admin")]
        (await _client.PostAsJsonAsync("/api/v1/payments", dto)).StatusCode
            .Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateOrderStatus_AsClientRole_Returns403()
    {
        SetBearerToken(TestJwtHelper.ClientBearer(_seed.UserAId, _seed.UserAEmail));
        var dto = new { status = "Accepted", notes = "Trying to accept own order" };
        // PATCH /api/v1/serviceorders/{id}/status is [Authorize(Roles = "admin,mechanic")]
        (await _client.PatchAsJsonAsync($"/api/v1/serviceorders/{_seed.UserAOrderId}/status", dto)).StatusCode
            .Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateVehicle_AsMechanicRole_Returns403()
    {
        // Mechanic role is not allowed to create vehicles
        var mechanicToken = TestJwtHelper.GenerateToken(Guid.NewGuid(), "mech@test.com", new[] { "mechanic" });
        SetBearerToken("Bearer " + mechanicToken);
        var dto = new { make = "X", model = "Y", year = 2020, licensePlate = "MEC01" };
        (await _client.PostAsJsonAsync("/api/v1/vehicles", dto)).StatusCode
            .Should().Be(HttpStatusCode.Forbidden);
    }

    private void SetBearerToken(string bearerValue) =>
        _client.DefaultRequestHeaders.Authorization =
            AuthenticationHeaderValue.Parse(bearerValue);
}
