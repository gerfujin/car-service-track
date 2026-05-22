using Microsoft.EntityFrameworkCore;
using Orders.Contracts.Repositories;
using Orders.Domain;

namespace Orders.Infrastructure.Repositories;

public class ServiceOrderStatusHistoryRepository
    : BaseRepository<ServiceOrderStatusHistory>, IServiceOrderStatusHistoryRepository
{
    public ServiceOrderStatusHistoryRepository(OrdersDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<ServiceOrderStatusHistory>> AllByOrderAsync(Guid serviceOrderId)
    {
        return await _context.ServiceOrderStatusHistories
            .Where(h => h.ServiceOrderId == serviceOrderId)
            .OrderBy(h => h.ChangedAt)
            .ToListAsync();
    }
}
