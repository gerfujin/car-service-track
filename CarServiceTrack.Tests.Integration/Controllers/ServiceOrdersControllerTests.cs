using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using App.DTO.v1.ServiceOrder;
using CarServiceTrack.Tests.Integration.Fixtures;
using CarServiceTrack.Tests.Integration.Helpers;
using FluentAssertions;

namespace CarServiceTrack.Tests.Integration.Controllers;

/// <summary>
/// Integration tests for <c>/api/v1/serviceorders</c>.
/// Covers: happy paths, auth/role enforcement, IDOR isolation, and status update workflow.
/// </summary>
[Collection("Integration")]
public class ServiceOrdersControllerTests : IAsyncLifetime
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

    // ── Authentication ────────────────────────────────────────────────────────
    [Fact]
    public async Task GetServiceOrders_WithoutToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync("/api/v1/serviceorders");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Client sees own orders only ───────────────────────────────────────────
    [Fact]
    public async Task GetServiceOrders_AsClientUserA_ReturnsOwnOrdersOnly()
    {
        SetBearerToken(TestJwtHelper.ClientBearer(_seed.UserAId, _seed.UserAEmail));

        var response = await _client.GetAsync("/api/v1/serviceorders");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var orders = await response.Content.ReadFromJsonAsync<List<ServiceOrderDto>>(TestJson.Options);
        orders.Should().NotBeNull();
        orders!.Should().OnlyContain(o => o.Id == _seed.UserAOrderId);
    }

    // ── IDOR: UserA cannot see UserA's order detail via UserB's identity ──────
    [Fact]
    public async Task GetServiceOrder_AsClientUserB_WithUserAOrderId_Returns404()
    {
        SetBearerToken(TestJwtHelper.ClientBearer(_seed.UserBId, _seed.UserBEmail));

        var response = await _client.GetAsync($"/api/v1/serviceorders/{_seed.UserAOrderId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetServiceOrder_AsClientUserA_WithOwnOrderId_Returns200()
    {
        SetBearerToken(TestJwtHelper.ClientBearer(_seed.UserAId, _seed.UserAEmail));

        var response = await _client.GetAsync($"/api/v1/serviceorders/{_seed.UserAOrderId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var order = await response.Content.ReadFromJsonAsync<ServiceOrderDto>(TestJson.Options);
        order!.Id.Should().Be(_seed.UserAOrderId);
    }

    // ── Admin sees all ────────────────────────────────────────────────────────
    [Fact]
    public async Task GetServiceOrders_AsAdmin_ReturnsAllOrders()
    {
        SetBearerToken(TestJwtHelper.AdminBearer(_seed.AdminId, _seed.AdminEmail));

        var response = await _client.GetAsync("/api/v1/serviceorders");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var orders = await response.Content.ReadFromJsonAsync<List<ServiceOrderDto>>(TestJson.Options);
        orders!.Count.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetServiceOrder_AsAdmin_WithUserAOrderId_Returns200()
    {
        SetBearerToken(TestJwtHelper.AdminBearer(_seed.AdminId, _seed.AdminEmail));

        var response = await _client.GetAsync($"/api/v1/serviceorders/{_seed.UserAOrderId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Create ────────────────────────────────────────────────────────────────
    [Fact]
    public async Task CreateServiceOrder_AsClientUserA_WithOwnVehicle_Returns201()
    {
        SetBearerToken(TestJwtHelper.ClientBearer(_seed.UserAId, _seed.UserAEmail));
        var dto = new ServiceOrderCreateDto
        {
            VehicleId = _seed.UserAVehicleId,
            WorkshopId = _seed.WorkshopId,
            Description = "New order from test"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/serviceorders", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var order = await response.Content.ReadFromJsonAsync<ServiceOrderDto>(TestJson.Options);
        order!.VehicleId.Should().Be(_seed.UserAVehicleId);
        order.Status.Should().Be(App.Domain.Enums.ServiceOrderStatus.Pending);
    }

    [Fact]
    public async Task CreateServiceOrder_AsClientUserA_WithUserBVehicle_Returns400()
    {
        // IDOR: client cannot create order for someone else's vehicle
        SetBearerToken(TestJwtHelper.ClientBearer(_seed.UserAId, _seed.UserAEmail));
        var dto = new ServiceOrderCreateDto
        {
            VehicleId = _seed.UserBVehicleId,
            WorkshopId = _seed.WorkshopId
        };

        var response = await _client.PostAsJsonAsync("/api/v1/serviceorders", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateServiceOrder_WithoutAuthentication_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var dto = new ServiceOrderCreateDto { VehicleId = _seed.UserAVehicleId, WorkshopId = _seed.WorkshopId };

        var response = await _client.PostAsJsonAsync("/api/v1/serviceorders", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Status update: only admin/mechanic ────────────────────────────────────
    [Fact]
    public async Task UpdateOrderStatus_AsAdmin_Returns204()
    {
        SetBearerToken(TestJwtHelper.AdminBearer(_seed.AdminId, _seed.AdminEmail));
        var dto = new UpdateOrderStatusDto { Status = App.Domain.Enums.ServiceOrderStatus.Accepted, Notes = "Accepted by admin" };

        var response = await _client.PatchAsJsonAsync($"/api/v1/serviceorders/{_seed.UserAOrderId}/status", dto);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UpdateOrderStatus_AsClient_Returns403()
    {
        SetBearerToken(TestJwtHelper.ClientBearer(_seed.UserAId, _seed.UserAEmail));
        var dto = new UpdateOrderStatusDto { Status = App.Domain.Enums.ServiceOrderStatus.Completed };

        var response = await _client.PatchAsJsonAsync($"/api/v1/serviceorders/{_seed.UserAOrderId}/status", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Status history ────────────────────────────────────────────────────────
    [Fact]
    public async Task GetStatusHistory_AsClientUserA_WithOwnOrder_Returns200()
    {
        SetBearerToken(TestJwtHelper.ClientBearer(_seed.UserAId, _seed.UserAEmail));

        var response = await _client.GetAsync($"/api/v1/serviceorders/{_seed.UserAOrderId}/status-history");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetStatusHistory_AsClientUserB_WithUserAOrder_Returns404()
    {
        // IDOR: UserB should not see UserA's order history
        SetBearerToken(TestJwtHelper.ClientBearer(_seed.UserBId, _seed.UserBEmail));

        var response = await _client.GetAsync($"/api/v1/serviceorders/{_seed.UserAOrderId}/status-history");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetServiceOrder_WithNonexistentId_Returns404()
    {
        SetBearerToken(TestJwtHelper.AdminBearer(_seed.AdminId, _seed.AdminEmail));

        var response = await _client.GetAsync($"/api/v1/serviceorders/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private void SetBearerToken(string bearerValue) =>
        _client.DefaultRequestHeaders.Authorization =
            AuthenticationHeaderValue.Parse(bearerValue);
}
