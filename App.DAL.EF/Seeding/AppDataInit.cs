using App.Domain;
using App.Domain.Identity;
using Base.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Seeding;

public static class AppDataInit
{
    public static void DeleteDatabase(AppDbContext context)
    {
        context.Database.EnsureDeleted();
    }

    public static void MigrateDatabase(AppDbContext context)
    {
        context.Database.Migrate();
    }

    public static void SeedAppData(AppDbContext context)
    {
        SeedWorkshops(context);
        SeedServices(context);
        SeedSpareParts(context);
    }

    private static void SeedWorkshops(AppDbContext context)
    {
        if (context.Workshops.Any()) return;

        var workshops = new List<Workshop>
        {
            new Workshop
            {
                Name = new LangStr("AutoFix Tallinn", "en"),
                Address = new LangStr("Pärnu mnt 12, Tallinn", "en"),
                Phone = "+372 5555 1111",
                Email = "info@autofix.ee"
            },
            new Workshop
            {
                Name = new LangStr("SpeedGarage Tartu", "en"),
                Address = new LangStr("Riia 15, Tartu", "en"),
                Phone = "+372 5555 2222",
                Email = "info@speedgarage.ee"
            }
        };

        context.Workshops.AddRange(workshops);
        context.SaveChanges();
    }

    private static void SeedServices(AppDbContext context)
    {
        if (context.Services.Any()) return;

        var services = new List<Service>
        {
            new Service
            {
                Name = new LangStr("Oil Change", "en"),
                Description = new LangStr("Full synthetic oil change with filter replacement", "en"),
                BasePrice = 49.99m
            },
            new Service
            {
                Name = new LangStr("Brake Inspection", "en"),
                Description = new LangStr("Complete brake system inspection and adjustment", "en"),
                BasePrice = 39.99m
            },
            new Service
            {
                Name = new LangStr("Tire Rotation", "en"),
                Description = new LangStr("Rotate all four tires for even wear", "en"),
                BasePrice = 29.99m
            },
            new Service
            {
                Name = new LangStr("Engine Diagnostics", "en"),
                Description = new LangStr("Full computer diagnostics scan", "en"),
                BasePrice = 59.99m
            },
            new Service
            {
                Name = new LangStr("Air Filter Replacement", "en"),
                Description = new LangStr("Replace engine air filter", "en"),
                BasePrice = 24.99m
            }
        };

        context.Services.AddRange(services);
        context.SaveChanges();
    }

    private static void SeedSpareParts(AppDbContext context)
    {
        if (context.SpareParts.Any()) return;

        var parts = new List<SparePart>
        {
            new SparePart
            {
                Name = new LangStr("Oil Filter", "en"),
                PartNumber = "OF-001",
                UnitPrice = 12.99m,
                StockQuantity = 50
            },
            new SparePart
            {
                Name = new LangStr("Brake Pad Set (Front)", "en"),
                PartNumber = "BP-F-001",
                UnitPrice = 45.99m,
                StockQuantity = 20
            },
            new SparePart
            {
                Name = new LangStr("Air Filter", "en"),
                PartNumber = "AF-001",
                UnitPrice = 18.99m,
                StockQuantity = 30
            },
            new SparePart
            {
                Name = new LangStr("Spark Plug Set", "en"),
                PartNumber = "SP-001",
                UnitPrice = 32.99m,
                StockQuantity = 40
            },
            new SparePart
            {
                Name = new LangStr("Windshield Wiper Blades", "en"),
                PartNumber = "WW-001",
                UnitPrice = 22.99m,
                StockQuantity = 25
            }
        };

        context.SpareParts.AddRange(parts);
        context.SaveChanges();
    }

    public static void SeedIdentity(UserManager<AppUser> userManager, RoleManager<AppRole> roleManager)
    {
        foreach (var roleName in InitialData.Roles)
        {
            var role = roleManager.FindByNameAsync(roleName).Result;

            if (role != null) continue;

            role = new AppRole()
            {
                Name = roleName,
            };

            var result = roleManager.CreateAsync(role).Result;
            if (!result.Succeeded)
            {
                throw new ApplicationException("Role creation failed!");
            }
        }

        foreach (var userInfo in InitialData.Users)
        {
            var user = userManager.FindByEmailAsync(userInfo.email).Result;
            if (user == null)
            {
                user = new AppUser()
                {
                    Email = userInfo.email,
                    UserName = userInfo.email,
                    EmailConfirmed = true
                };
                var result = userManager.CreateAsync(user, userInfo.password).Result;
                if (!result.Succeeded)
                {
                    throw new ApplicationException("User creation failed!");
                }
            }

            foreach (var role in userInfo.roles)
            {
                if (userManager.IsInRoleAsync(user, role).Result)
                {
                    Console.WriteLine($"User {user.UserName} already in role {role}");
                    continue;
                }

                var roleResult = userManager.AddToRoleAsync(user, role).Result;
                if (!roleResult.Succeeded)
                {
                    foreach (var error in roleResult.Errors)
                    {
                        Console.WriteLine(error.Description);
                    }
                }
                else
                {
                    Console.WriteLine($"User {user.UserName} added to role {role}");
                }
            }
        }
    }
}
