using Base.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Orders.Domain;
using Orders.Domain.Enums;
using Orders.Infrastructure;
using Users.Domain.Identity;
using Users.Infrastructure;
using Workshops.Domain;
using Workshops.Infrastructure;

namespace CarServiceTrack.Tests.Integration.Fixtures;

/// <summary>
/// Seeds two independent users (A and B) with full data so that IDOR tests can
/// verify cross-user isolation. Also seeds reference data (workshop, services).
///
/// Identity (users/roles) goes into AppDbContext via UserManager so that
/// JWT authentication works during tests.  All other domain data goes into
/// the corresponding module DbContexts (WorkshopsDbContext, UsersDbContext,
/// OrdersDbContext) that the module Application Services read from.
///
/// EF Core SQLite enforces FK constraints (PRAGMA foreign_keys = ON). Because
/// Owner.AppUserId references the AspNetUsers table *inside UsersDbContext*,
/// we must mirror the identity users there too, even though the canonical user
/// lives in AppDbContext.
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
        // ── Identity (AppDbContext) ────────────────────────────────────────────
        // UserManager / RoleManager are backed by AppDbContext in the test host.
        // We keep this as-is so that JWT bearer tokens issued by the test helpers
        // are accepted by the running application.
        var userManager = sp.GetRequiredService<UserManager<AppUser>>();
        var roleManager = sp.GetRequiredService<RoleManager<AppRole>>();

        foreach (var role in new[] { "admin", "mechanic", "client" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new AppRole { Name = role });
        }

        var userA = await CreateUserAsync(userManager, "usera@test.com", Password, "client");
        var userB = await CreateUserAsync(userManager, "userb@test.com", Password, "client");
        var admin = await CreateUserAsync(userManager, "admin@test.com", Password, "admin");

        // ── Workshops module ──────────────────────────────────────────────────
        var workshopsDb = sp.GetRequiredService<WorkshopsDbContext>();

        var workshop = new Workshop
        {
            Name = new LangStr("Test Workshop", "en"),
            Address = new LangStr("Test Street 1", "en"),
            Phone = "+372 555 1234"
        };
        workshopsDb.Workshops.Add(workshop);

        var service = new Service
        {
            Name = new LangStr("Oil Change", "en"),
            Description = new LangStr("Full oil change", "en"),
            BasePrice = 49.99m
        };
        workshopsDb.Services.Add(service);

        await workshopsDb.SaveChangesAsync();

        // ── Users module ──────────────────────────────────────────────────────
        // FK enforcement is disabled on UsersDbContext's SQLite connection via
        // DisableForeignKeysInterceptor (see CustomWebApplicationFactory), so we can
        // insert Owners directly without mirroring users into users.AspNetUsers.
        var usersDb = sp.GetRequiredService<UsersDbContext>();

        var ownerA = new Users.Domain.Owner { AppUserId = userA.Id, FirstName = "Alice", LastName = "A" };
        usersDb.Owners.Add(ownerA);
        await usersDb.SaveChangesAsync();   // flush so ownerA.Id is populated

        var vehicleA = new Users.Domain.Vehicle
        {
            OwnerId = ownerA.Id,
            Make = "Toyota",
            Model = "Camry",
            Year = 2022,
            LicensePlate = "CTA001"
        };
        usersDb.Vehicles.Add(vehicleA);

        var ownerB = new Users.Domain.Owner { AppUserId = userB.Id, FirstName = "Bob", LastName = "B" };
        usersDb.Owners.Add(ownerB);
        await usersDb.SaveChangesAsync();   // flush so ownerB.Id is populated

        var vehicleB = new Users.Domain.Vehicle
        {
            OwnerId = ownerB.Id,
            Make = "Honda",
            Model = "Civic",
            Year = 2021,
            LicensePlate = "CTB002"
        };
        usersDb.Vehicles.Add(vehicleB);

        await usersDb.SaveChangesAsync();

        // ── Orders module ─────────────────────────────────────────────────────
        // ServiceOrder.VehicleId / WorkshopId are cross-module plain-id references
        // stored without a FK in the Orders schema — no constraint problem here.
        var ordersDb = sp.GetRequiredService<OrdersDbContext>();

        var orderA = new ServiceOrder
        {
            AppUserId = userA.Id,
            VehicleId = vehicleA.Id,
            WorkshopId = workshop.Id,
            Status = ServiceOrderStatus.Pending,
            OrderDate = DateTime.UtcNow,
            Description = "First service order"
        };
        ordersDb.ServiceOrders.Add(orderA);
        await ordersDb.SaveChangesAsync();   // flush so orderA.Id is populated

        var paymentA = new Payment
        {
            ServiceOrderId = orderA.Id,
            Amount = 149.99m,
            Status = PaymentStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        ordersDb.Payments.Add(paymentA);
        await ordersDb.SaveChangesAsync();

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
            throw new InvalidOperationException(
                $"Failed to create test user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");

        await userManager.AddToRoleAsync(user, role);
        return user;
    }
}
