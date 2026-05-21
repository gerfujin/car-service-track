using App.DAL.Contracts;
using App.Domain;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories;

public class ServiceOrderStatusHistoryRepository
    : BaseRepository<ServiceOrderStatusHistory>, IServiceOrderStatusHistoryRepository
{
    public ServiceOrderStatusHistoryRepository(AppDbContext context) : base(context)
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
