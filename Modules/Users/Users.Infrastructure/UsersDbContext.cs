using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Users.Domain;
using Users.Domain.Identity;

namespace Users.Infrastructure;

public class UsersDbContext : IdentityDbContext<AppUser, AppRole, Guid>
{
    public const string SchemaName = "users";

    public DbSet<AppRefreshToken> RefreshTokens { get; set; } = default!;
    public DbSet<Owner> Owners { get; set; } = default!;
    public DbSet<Vehicle> Vehicles { get; set; } = default!;

    public UsersDbContext(DbContextOptions<UsersDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasDefaultSchema(SchemaName);

        foreach (var relationship in builder.Model
                     .GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            relationship.DeleteBehavior = DeleteBehavior.Restrict;
        }

        builder.Entity<Owner>()
            .HasOne(o => o.AppUser)
            .WithOne(u => u.Owner)
            .HasForeignKey<Owner>(o => o.AppUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Vehicle>()
            .HasIndex(v => v.LicensePlate);

        builder.Entity<Owner>()
            .HasIndex(o => o.AppUserId)
            .IsUnique();
    }
}
