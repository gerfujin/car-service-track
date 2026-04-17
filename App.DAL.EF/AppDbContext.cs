using System.Text.Json;
using App.Domain;
using App.Domain.Identity;
using Base.Domain;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF;

public class AppDbContext : IdentityDbContext<AppUser, AppRole, Guid>, IDataProtectionKeyContext
{
    // Legacy
    public DbSet<ListItem> ListItems { get; set; }

    public DbSet<AppRefreshToken> RefreshTokens { get; set; } = default!;

    // This maps to the table that stores data protection keys.
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = default!;

    // CarServiceTrack entities
    public DbSet<Owner> Owners { get; set; } = default!;
    public DbSet<Vehicle> Vehicles { get; set; } = default!;
    public DbSet<Workshop> Workshops { get; set; } = default!;
    public DbSet<Mechanic> Mechanics { get; set; } = default!;
    public DbSet<MechanicInWorkshop> MechanicsInWorkshop { get; set; } = default!;
    public DbSet<Service> Services { get; set; } = default!;
    public DbSet<SparePart> SpareParts { get; set; } = default!;
    public DbSet<ServiceOrder> ServiceOrders { get; set; } = default!;
    public DbSet<ServiceOrderItem> ServiceOrderItems { get; set; } = default!;
    public DbSet<ServiceOrderPart> ServiceOrderParts { get; set; } = default!;
    public DbSet<ServiceOrderStatusHistory> ServiceOrderStatusHistories { get; set; } = default!;
    public DbSet<Payment> Payments { get; set; } = default!;
    public DbSet<RepairPhoto> RepairPhotos { get; set; } = default!;

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // disable cascade delete globally
        foreach (var relationship in builder.Model
                     .GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            relationship.DeleteBehavior = DeleteBehavior.Restrict;
        }

        // LangStr JSON conversions for ListItem (legacy)
        builder.Entity<ListItem>().Property(e => e.Summary)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<LangStr>(v, (JsonSerializerOptions?)null)!
            )
            .HasColumnType("jsonb");

        // LangStr JSON conversions for Workshop
        builder.Entity<Workshop>().Property(e => e.Name)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<LangStr>(v, (JsonSerializerOptions?)null)!
            )
            .HasColumnType("jsonb");

        builder.Entity<Workshop>().Property(e => e.Address)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<LangStr>(v, (JsonSerializerOptions?)null)!
            )
            .HasColumnType("jsonb");

        // LangStr JSON conversions for Service
        builder.Entity<Service>().Property(e => e.Name)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<LangStr>(v, (JsonSerializerOptions?)null)!
            )
            .HasColumnType("jsonb");

        builder.Entity<Service>().Property(e => e.Description)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<LangStr>(v, (JsonSerializerOptions?)null)!
            )
            .HasColumnType("jsonb");

        // LangStr JSON conversions for SparePart
        builder.Entity<SparePart>().Property(e => e.Name)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<LangStr>(v, (JsonSerializerOptions?)null)!
            )
            .HasColumnType("jsonb");

        // Owner -> AppUser: one-to-one
        builder.Entity<Owner>()
            .HasOne(o => o.AppUser)
            .WithOne(u => u.Owner)
            .HasForeignKey<Owner>(o => o.AppUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.Entity<Vehicle>()
            .HasIndex(v => v.LicensePlate);

        builder.Entity<Owner>()
            .HasIndex(o => o.AppUserId)
            .IsUnique();

        // Payment -> ServiceOrder: one-to-one
        builder.Entity<Payment>()
            .HasOne(p => p.ServiceOrder)
            .WithOne(so => so.Payment)
            .HasForeignKey<Payment>(p => p.ServiceOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        // Decimal precision
        builder.Entity<Service>()
            .Property(s => s.BasePrice)
            .HasPrecision(18, 2);

        builder.Entity<SparePart>()
            .Property(sp => sp.UnitPrice)
            .HasPrecision(18, 2);

        builder.Entity<ServiceOrderItem>()
            .Property(soi => soi.UnitPrice)
            .HasPrecision(18, 2);

        builder.Entity<ServiceOrderPart>()
            .Property(sop => sop.UnitPrice)
            .HasPrecision(18, 2);

        builder.Entity<Payment>()
            .Property(p => p.Amount)
            .HasPrecision(18, 2);
    }
}
