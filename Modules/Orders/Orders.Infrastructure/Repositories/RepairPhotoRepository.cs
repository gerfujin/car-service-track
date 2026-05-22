using Microsoft.EntityFrameworkCore;
using Orders.Contracts.Repositories;
using Orders.Domain;

namespace Orders.Infrastructure.Repositories;

public class RepairPhotoRepository : BaseRepository<RepairPhoto>, IRepairPhotoRepository
{
    public RepairPhotoRepository(OrdersDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<RepairPhoto>> AllByServiceOrderWithDetailsAsync(Guid serviceOrderId)
    {
        return await _context.RepairPhotos
            .Include(p => p.ServiceOrder)
            .Where(p => p.ServiceOrderId == serviceOrderId)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<RepairPhoto?> FindWithDetailsAsync(Guid id)
    {
        return await _context.RepairPhotos
            .Include(p => p.ServiceOrder)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<int> CountByServiceOrderAsync(Guid serviceOrderId)
    {
        return await _context.RepairPhotos.CountAsync(p => p.ServiceOrderId == serviceOrderId);
    }
}
