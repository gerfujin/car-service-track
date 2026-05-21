using System.Text.Json;
using Base.Domain;
using Microsoft.EntityFrameworkCore;
using Workshops.Domain;

namespace Workshops.Infrastructure;

public class WorkshopsDbContext : DbContext
{
    public const string SchemaName = "workshops";

    public DbSet<Workshop> Workshops { get; set; } = default!;
    public DbSet<Mechanic> Mechanics { get; set; } = default!;
    public DbSet<MechanicInWorkshop> MechanicsInWorkshop { get; set; } = default!;
    public DbSet<Service> Services { get; set; } = default!;
    public DbSet<SparePart> SpareParts { get; set; } = default!;

    public WorkshopsDbContext(DbContextOptions<WorkshopsDbContext> options)
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

        builder.Entity<SparePart>().Property(e => e.Name)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<LangStr>(v, (JsonSerializerOptions?)null)!
            )
            .HasColumnType("jsonb");

        builder.Entity<Service>()
            .Property(s => s.BasePrice)
            .HasPrecision(18, 2);

        builder.Entity<SparePart>()
            .Property(sp => sp.UnitPrice)
            .HasPrecision(18, 2);
    }
}
