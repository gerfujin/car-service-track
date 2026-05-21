using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using App.DTO.v1.Payment;
using CarServiceTrack.Tests.Integration.Fixtures;
using CarServiceTrack.Tests.Integration.Helpers;
using FluentAssertions;

namespace CarServiceTrack.Tests.Integration.Controllers;

/// <summary>
/// Integration tests for <c>/api/v1/payments</c>.
/// Covers: authentication enforcement, IDOR isolation, and happy paths.
/// </summary>
[Collection("Integration")]
public class PaymentsControllerTests : IAsyncLifetime
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
    public async Task GetPayments_WithoutToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync("/api/v1/payments");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Client sees own payments only ─────────────────────────────────────────
    [Fact]
    public async Task GetPayments_AsClientUserA_ReturnsOwnPaymentsOnly()
    {
        SetBearerToken(TestJwtHelper.ClientBearer(_seed.UserAId, _seed.UserAEmail));

        var response = await _client.GetAsync("/api/v1/payments");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payments = await response.Content.ReadFromJsonAsync<List<PaymentDto>>(TestJson.Options);
        payments.Should().NotBeNull();
        payments!.Should().OnlyContain(p => p.Id == _seed.UserAPaymentId);
    }

    [Fact]
    public async Task GetPayments_AsClientUserB_ReturnsEmptyList()
    {
        // UserB has no payments seeded
        SetBearerToken(TestJwtHelper.ClientBearer(_seed.UserBId, _seed.UserBEmail));

        var response = await _client.GetAsync("/api/v1/payments");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payments = await response.Content.ReadFromJsonAsync<List<PaymentDto>>(TestJson.Options);
        payments.Should().BeEmpty();
    }

    // ── IDOR ──────────────────────────────────────────────────────────────────
    [Fact]
    public async Task GetPayment_AsClientUserB_WithUserAPaymentId_Returns404()
    {
        SetBearerToken(TestJwtHelper.ClientBearer(_seed.UserBId, _seed.UserBEmail));

        var response = await _client.GetAsync($"/api/v1/payments/{_seed.UserAPaymentId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPayment_AsClientUserA_WithOwnPayment_Returns200()
    {
        SetBearerToken(TestJwtHelper.ClientBearer(_seed.UserAId, _seed.UserAEmail));

        var response = await _client.GetAsync($"/api/v1/payments/{_seed.UserAPaymentId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payment = await response.Content.ReadFromJsonAsync<PaymentDto>(TestJson.Options);
        payment!.Id.Should().Be(_seed.UserAPaymentId);
    }

    // ── Admin sees all ────────────────────────────────────────────────────────
    [Fact]
    public async Task GetPayments_AsAdmin_ReturnsAllPayments()
    {
        SetBearerToken(TestJwtHelper.AdminBearer(_seed.AdminId, _seed.AdminEmail));

        var response = await _client.GetAsync("/api/v1/payments");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payments = await response.Content.ReadFromJsonAsync<List<PaymentDto>>(TestJson.Options);
        payments!.Count.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetPayment_AsAdmin_WithUserAPaymentId_Returns200()
    {
        SetBearerToken(TestJwtHelper.AdminBearer(_seed.AdminId, _seed.AdminEmail));

        var response = await _client.GetAsync($"/api/v1/payments/{_seed.UserAPaymentId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Create payment (duplicate protection) ─────────────────────────────────
    [Fact]
    public async Task CreatePayment_AsAdmin_WhenPaymentAlreadyExists_Returns400()
    {
        SetBearerToken(TestJwtHelper.AdminBearer(_seed.AdminId, _seed.AdminEmail));
        // A payment for UserA's order was already seeded
        var dto = new PaymentCreateDto { ServiceOrderId = _seed.UserAOrderId, Amount = 99m };

        var response = await _client.PostAsJsonAsync("/api/v1/payments", dto);

        // Duplicate payment → 400 Bad Request
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Nonexistent payment ───────────────────────────────────────────────────
    [Fact]
    public async Task GetPayment_WithNonexistentId_Returns404()
    {
        SetBearerToken(TestJwtHelper.AdminBearer(_seed.AdminId, _seed.AdminEmail));

        var response = await _client.GetAsync($"/api/v1/payments/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private void SetBearerToken(string bearerValue) =>
        _client.DefaultRequestHeaders.Authorization =
            AuthenticationHeaderValue.Parse(bearerValue);
}
