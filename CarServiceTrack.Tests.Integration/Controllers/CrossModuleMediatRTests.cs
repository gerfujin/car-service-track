using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using App.DTO.v1.ServiceOrder;
using CarServiceTrack.Tests.Integration.Fixtures;
using CarServiceTrack.Tests.Integration.Helpers;
using FluentAssertions;

namespace CarServiceTrack.Tests.Integration.Controllers;

/// <summary>
/// Integration tests that verify cross-module MediatR communication works end-to-end.
///
/// The Orders module enriches service order responses by querying the Users and Workshops
/// modules via MediatR:
///   - <c>GetVehicleByIdQuery</c>  → Users module  → populates <c>VehicleDisplay</c>
///   - <c>GetWorkshopByIdQuery</c> → Workshops module → populates <c>WorkshopName</c>
///   - <c>GetOwnerByAppUserIdQuery</c> → Users module → populates owner info
///
/// These tests confirm that the MediatR pipeline is wired correctly in the modular
/// monolith host and that cross-module data flows through to the API response.
/// </summary>
[Collection("Integration")]
public class CrossModuleMediatRTests : IAsyncLifetime
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

    // ── Orders → Users: VehicleDisplay populated via GetVehicleByIdQuery ─────

    /// <summary>
    /// When a service order is retrieved, the Orders module queries the Users module
    /// via MediatR (<c>GetVehicleByIdQuery</c>) to resolve the vehicle display string.
    /// The response must contain a non-null <c>VehicleDisplay</c>.
    /// </summary>
    [Fact]
    public async Task GetServiceOrder_AsAdmin_VehicleDisplay_PopulatedFromUsersModule()
    {
        SetBearerToken(TestJwtHelper.AdminBearer(_seed.AdminId, _seed.AdminEmail));

        var response = await _client.GetAsync($"/api/v1/serviceorders/{_seed.UserAOrderId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var order = await response.Content.ReadFromJsonAsync<ServiceOrderDto>(TestJson.Options);
        order.Should().NotBeNull();
        // VehicleDisplay is populated by Orders.Application.ServiceOrderService.EnrichOrderAsync
        // via GetVehicleByIdQuery → Users.Application.Queries.GetVehicleByIdQueryHandler
        order!.VehicleDisplay.Should().NotBeNullOrWhiteSpace(
            "Orders module must resolve vehicle info from Users module via MediatR");
        order.VehicleDisplay.Should().Contain("Toyota");
        order.VehicleDisplay.Should().Contain("Camry");
        order.VehicleDisplay.Should().Contain("CTA001");
    }

    // ── Orders → Workshops: WorkshopName populated via GetWorkshopByIdQuery ──

    /// <summary>
    /// When a service order is retrieved, the Orders module queries the Workshops module
    /// via MediatR (<c>GetWorkshopByIdQuery</c>) to resolve the workshop name.
    /// The response must contain a non-null <c>WorkshopName</c>.
    /// </summary>
    [Fact]
    public async Task GetServiceOrder_AsAdmin_WorkshopName_PopulatedFromWorkshopsModule()
    {
        SetBearerToken(TestJwtHelper.AdminBearer(_seed.AdminId, _seed.AdminEmail));

        var response = await _client.GetAsync($"/api/v1/serviceorders/{_seed.UserAOrderId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var order = await response.Content.ReadFromJsonAsync<ServiceOrderDto>(TestJson.Options);
        order.Should().NotBeNull();
        // WorkshopName is populated by Orders.Application.ServiceOrderService.EnrichOrderAsync
        // via GetWorkshopByIdQuery → Workshops.Application.Queries.GetWorkshopByIdQueryHandler
        order!.WorkshopName.Should().NotBeNullOrWhiteSpace(
            "Orders module must resolve workshop name from Workshops module via MediatR");
        order.WorkshopName.Should().Contain("Test Workshop");
    }

    // ── Orders → Users: VehicleDisplay in list endpoint ──────────────────────

    /// <summary>
    /// The list endpoint also enriches each order via MediatR.
    /// Verifies that cross-module enrichment works for collection responses.
    /// </summary>
    [Fact]
    public async Task GetServiceOrders_AsAdmin_AllOrders_HaveVehicleDisplayFromUsersModule()
    {
        SetBearerToken(TestJwtHelper.AdminBearer(_seed.AdminId, _seed.AdminEmail));

        var response = await _client.GetAsync("/api/v1/serviceorders");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var orders = await response.Content.ReadFromJsonAsync<List<ServiceOrderDto>>(TestJson.Options);
        orders.Should().NotBeNull();
        orders!.Should().NotBeEmpty();
        // Every order in the list must have VehicleDisplay populated via cross-module MediatR
        orders.Should().OnlyContain(o => o.VehicleDisplay != null,
            "all orders must have VehicleDisplay resolved from Users module via MediatR");
    }

    // ── Orders → Workshops: WorkshopName in list endpoint ────────────────────

    [Fact]
    public async Task GetServiceOrders_AsAdmin_AllOrders_HaveWorkshopNameFromWorkshopsModule()
    {
        SetBearerToken(TestJwtHelper.AdminBearer(_seed.AdminId, _seed.AdminEmail));

        var response = await _client.GetAsync("/api/v1/serviceorders");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var orders = await response.Content.ReadFromJsonAsync<List<ServiceOrderDto>>(TestJson.Options);
        orders.Should().NotBeNull();
        orders!.Should().NotBeEmpty();
        // Every order in the list must have WorkshopName populated via cross-module MediatR
        orders.Should().OnlyContain(o => o.WorkshopName != null,
            "all orders must have WorkshopName resolved from Workshops module via MediatR");
    }

    // ── Cross-module enrichment does not break IDOR isolation ─────────────────

    /// <summary>
    /// Even with cross-module enrichment active, IDOR isolation must hold:
    /// UserB cannot see UserA's order even though the enrichment queries would succeed.
    /// </summary>
    [Fact]
    public async Task GetServiceOrder_CrossModuleEnrichment_DoesNotBypassIdorIsolation()
    {
        // UserB tries to access UserA's order — must get 404 regardless of enrichment
        SetBearerToken(TestJwtHelper.ClientBearer(_seed.UserBId, _seed.UserBEmail));

        var response = await _client.GetAsync($"/api/v1/serviceorders/{_seed.UserAOrderId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "cross-module MediatR enrichment must not bypass IDOR isolation");
    }

    // ── Create order: cross-module validation (vehicle + workshop lookup) ─────

    /// <summary>
    /// Creating a service order requires the controller to validate the vehicle (Users module)
    /// and workshop (Workshops module) via their respective services.
    /// This test verifies that cross-module service calls work during the write path.
    /// </summary>
    [Fact]
    public async Task CreateServiceOrder_CrossModuleValidation_VehicleAndWorkshopResolvedCorrectly()
    {
        SetBearerToken(TestJwtHelper.ClientBearer(_seed.UserAId, _seed.UserAEmail));
        var dto = new ServiceOrderCreateDto
        {
            VehicleId = _seed.UserAVehicleId,
            WorkshopId = _seed.WorkshopId,
            Description = "Cross-module validation test"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/serviceorders", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "vehicle (Users module) and workshop (Workshops module) must be resolved correctly");
        var order = await response.Content.ReadFromJsonAsync<ServiceOrderDto>(TestJson.Options);
        order.Should().NotBeNull();
        // The response includes VehicleDisplay and WorkshopName from cross-module lookups
        order!.VehicleDisplay.Should().NotBeNullOrWhiteSpace();
        order.WorkshopName.Should().NotBeNullOrWhiteSpace();
    }

    /// <summary>
    /// Creating a service order with a non-existent workshop (Workshops module lookup fails)
    /// must return 400 Bad Request.
    /// </summary>
    [Fact]
    public async Task CreateServiceOrder_WithNonexistentWorkshop_CrossModuleValidation_Returns400()
    {
        SetBearerToken(TestJwtHelper.ClientBearer(_seed.UserAId, _seed.UserAEmail));
        var dto = new ServiceOrderCreateDto
        {
            VehicleId = _seed.UserAVehicleId,
            WorkshopId = Guid.NewGuid(), // non-existent workshop
            Description = "Should fail"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/serviceorders", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "Workshops module lookup must fail for non-existent workshop");
    }

    private void SetBearerToken(string bearerValue) =>
        _client.DefaultRequestHeaders.Authorization =
            AuthenticationHeaderValue.Parse(bearerValue);
}
