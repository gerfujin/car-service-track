using App.DAL.Contracts;
using App.Domain;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories;

public class RepairPhotoRepository : BaseRepository<RepairPhoto>, IRepairPhotoRepository
{
    public RepairPhotoRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<RepairPhoto>> AllByServiceOrderWithDetailsAsync(Guid serviceOrderId)
    {
        return await _context.RepairPhotos
            .Include(p => p.ServiceOrder)
                .ThenInclude(so => so!.Vehicle)
            .Where(p => p.ServiceOrderId == serviceOrderId)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<RepairPhoto?> FindWithDetailsAsync(Guid id)
    {
        return await _context.RepairPhotos
            .Include(p => p.ServiceOrder)
                .ThenInclude(so => so!.Vehicle)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<int> CountByServiceOrderAsync(Guid serviceOrderId)
    {
        return await _context.RepairPhotos.CountAsync(p => p.ServiceOrderId == serviceOrderId);
    }
}
