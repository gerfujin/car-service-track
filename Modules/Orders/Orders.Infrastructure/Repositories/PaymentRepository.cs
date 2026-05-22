using Microsoft.EntityFrameworkCore;
using Orders.Contracts.Repositories;
using Orders.Domain;
using Orders.Domain.Enums;

namespace Orders.Infrastructure.Repositories;

public class PaymentRepository : BaseRepository<Payment>, IPaymentRepository
{
    public PaymentRepository(OrdersDbContext context) : base(context)
    {
    }

    private IQueryable<Payment> WithDetails() =>
        _context.Payments
            .Include(p => p.ServiceOrder);

    public async Task<IEnumerable<Payment>> AllWithDetailsAsync(Guid? serviceOrderId = null)
    {
        var query = WithDetails();
        if (serviceOrderId.HasValue)
            query = query.Where(p => p.ServiceOrderId == serviceOrderId.Value);
        return await query.ToListAsync();
    }

    public async Task<IEnumerable<Payment>> AllByUserAsync(Guid appUserId, Guid? serviceOrderId = null)
    {
        var query = WithDetails()
            .Where(p => p.ServiceOrder!.AppUserId == appUserId);
        if (serviceOrderId.HasValue)
            query = query.Where(p => p.ServiceOrderId == serviceOrderId.Value);
        return await query.ToListAsync();
    }

    public async Task<Payment?> FindWithDetailsAsync(Guid id)
    {
        return await WithDetails().FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Payment?> FindWithDetailsForUserAsync(Guid id, Guid appUserId)
    {
        return await WithDetails()
            .FirstOrDefaultAsync(p => p.Id == id && p.ServiceOrder!.AppUserId == appUserId);
    }

    public async Task<Payment?> FindByServiceOrderAsync(Guid serviceOrderId)
    {
        return await WithDetails().FirstOrDefaultAsync(p => p.ServiceOrderId == serviceOrderId);
    }

    public async Task<Payment?> FindByServiceOrderForUserAsync(Guid serviceOrderId, Guid appUserId)
    {
        return await WithDetails()
            .FirstOrDefaultAsync(p =>
                p.ServiceOrderId == serviceOrderId &&
                p.ServiceOrder!.AppUserId == appUserId);
    }

    public async Task<bool> AnyByServiceOrderAsync(Guid serviceOrderId)
    {
        return await _context.Payments.AnyAsync(p => p.ServiceOrderId == serviceOrderId);
    }

    public async Task<decimal> SumPaidAmountAsync()
    {
        return await _context.Payments
            .Where(p => p.Status == PaymentStatus.Paid)
            .SumAsync(p => p.Amount);
    }
}
