using App.DAL.EF;
using App.Domain;
using App.Domain.Identity;
using Base.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace CarServiceTrack.Tests.Integration.Fixtures;

/// <summary>
/// Seeds two independent users (A and B) with full data so that IDOR tests can
/// verify cross-user isolation. Also seeds reference data (workshop, services).
/// </summary>
public class SeedResult
{
    public Guid UserAId { get; init; }
    public string UserAEmail { get; init; } = default!;
    public Guid UserBId { get; init; }
    public string UserBEmail { get; init; } = default!;
    public Guid AdminId { get; init; }
    public string AdminEmail { get; init; } = default!;
    public Guid WorkshopId { get; init; }
    public Guid UserAVehicleId { get; init; }
    public Guid UserBVehicleId { get; init; }
    public Guid UserAOrderId { get; init; }
    public Guid UserAPaymentId { get; init; }
}

public static class TestDataSeeder
{
    private const string Password = "Test123!";

    public static async Task<SeedResult> SeedAsync(IServiceProvider sp)
    {
        var db = sp.GetRequiredService<AppDbContext>();
        var userManager = sp.GetRequiredService<UserManager<AppUser>>();
        var roleManager = sp.GetRequiredService<RoleManager<AppRole>>();

        // ── Roles ─────────────────────────────────────────────────────────────
        // Lowercase "client" matches InitialData.cs and the API [Authorize] attrs.
        foreach (var role in new[] { "admin", "mechanic", "client" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new AppRole { Name = role });
        }

        // ── Users ─────────────────────────────────────────────────────────────
        var userA = await CreateUserAsync(userManager, "usera@test.com", Password, "client");
        var userB = await CreateUserAsync(userManager, "userb@test.com", Password, "client");
        var admin = await CreateUserAsync(userManager, "admin@test.com", Password, "admin");

        // ── Reference data ────────────────────────────────────────────────────
        var workshop = new Workshop
        {
            Name = new LangStr("Test Workshop", "en"),
            Address = new LangStr("Test Street 1", "en"),
            Phone = "+372 555 1234"
        };
        db.Workshops.Add(workshop);

        var service = new Service
        {
            Name = new LangStr("Oil Change", "en"),
            Description = new LangStr("Full oil change", "en"),
            BasePrice = 49.99m
        };
        db.Services.Add(service);

        // ── Owner + Vehicle for User A ─────────────────────────────────────────
        var ownerA = new Owner { AppUserId = userA.Id, FirstName = "Alice", LastName = "A" };
        db.Owners.Add(ownerA);

        var vehicleA = new Vehicle
        {
            OwnerId = ownerA.Id,
            Make = "Toyota",
            Model = "Camry",
            Year = 2022,
            LicensePlate = "CTA001"
        };
        db.Vehicles.Add(vehicleA);

        // ── Owner + Vehicle for User B ─────────────────────────────────────────
        var ownerB = new Owner { AppUserId = userB.Id, FirstName = "Bob", LastName = "B" };
        db.Owners.Add(ownerB);

        var vehicleB = new Vehicle
        {
            OwnerId = ownerB.Id,
            Make = "Honda",
            Model = "Civic",
            Year = 2021,
            LicensePlate = "CTB002"
        };
        db.Vehicles.Add(vehicleB);

        await db.SaveChangesAsync();

        // ── Service Order for User A ──────────────────────────────────────────
        var orderA = new ServiceOrder
        {
            VehicleId = vehicleA.Id,
            WorkshopId = workshop.Id,
            Status = App.Domain.Enums.ServiceOrderStatus.Pending,
            OrderDate = DateTime.UtcNow,
            Description = "First service order"
        };
        db.ServiceOrders.Add(orderA);

        await db.SaveChangesAsync();

        // ── Payment for User A's order ────────────────────────────────────────
        var paymentA = new Payment
        {
            ServiceOrderId = orderA.Id,
            Amount = 149.99m,
            Status = App.Domain.Enums.PaymentStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Payments.Add(paymentA);

        await db.SaveChangesAsync();

        return new SeedResult
        {
            UserAId = userA.Id,
            UserAEmail = "usera@test.com",
            UserBId = userB.Id,
            UserBEmail = "userb@test.com",
            AdminId = admin.Id,
            AdminEmail = "admin@test.com",
            WorkshopId = workshop.Id,
            UserAVehicleId = vehicleA.Id,
            UserBVehicleId = vehicleB.Id,
            UserAOrderId = orderA.Id,
            UserAPaymentId = paymentA.Id
        };
    }

    private static async Task<AppUser> CreateUserAsync(
        UserManager<AppUser> userManager,
        string email,
        string password,
        string role)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing != null) return existing;

        var user = new AppUser { Email = email, UserName = email, EmailConfirmed = true };
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException($"Failed to create test user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");

        await userManager.AddToRoleAsync(user, role);
        return user;
    }
}
