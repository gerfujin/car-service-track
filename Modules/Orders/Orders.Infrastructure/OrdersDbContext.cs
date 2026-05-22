using Microsoft.EntityFrameworkCore;
using Orders.Domain;

namespace Orders.Infrastructure;

public class OrdersDbContext : DbContext
{
    public const string SchemaName = "orders";

    public DbSet<ServiceOrder> ServiceOrders { get; set; } = default!;
    public DbSet<ServiceOrderItem> ServiceOrderItems { get; set; } = default!;
    public DbSet<ServiceOrderPart> ServiceOrderParts { get; set; } = default!;
    public DbSet<ServiceOrderStatusHistory> ServiceOrderStatusHistories { get; set; } = default!;
    public DbSet<Payment> Payments { get; set; } = default!;
    public DbSet<RepairPhoto> RepairPhotos { get; set; } = default!;

    public OrdersDbContext(DbContextOptions<OrdersDbContext> options)
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

        // Orders entities currently do not contain LangStr properties.

        builder.Entity<ServiceOrder>()
            .HasIndex(so => so.AppUserId);

        builder.Entity<Payment>()
            .HasOne(p => p.ServiceOrder)
            .WithOne(so => so.Payment)
            .HasForeignKey<Payment>(p => p.ServiceOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ServiceOrderItem>()
            .Property(soi => soi.UnitPrice)
            .HasPrecision(18, 2);

        builder.Entity<ServiceOrderPart>()
            .Property(sop => sop.UnitPrice)
            .HasPrecision(18, 2);

        builder.Entity<Payment>()
            .Property(p => p.Amount)
            .HasPrecision(18, 2);

        builder.Entity<ServiceOrder>()
            .Property(so => so.FinalPrice)
            .HasPrecision(18, 2);
    }
}
