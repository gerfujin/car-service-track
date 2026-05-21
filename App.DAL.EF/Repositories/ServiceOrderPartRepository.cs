using App.DAL.Contracts;
using App.Domain;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories;

public class ServiceOrderPartRepository : BaseRepository<ServiceOrderPart>, IServiceOrderPartRepository
{
    public ServiceOrderPartRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<ServiceOrderPart>> AllWithDetailsAsync(Guid? serviceOrderId)
    {
        var query = _context.ServiceOrderParts
            .Include(sop => sop.ServiceOrder)
                .ThenInclude(so => so!.Vehicle)
            .Include(sop => sop.SparePart)
            .AsNoTracking()
            .AsQueryable();

        if (serviceOrderId.HasValue)
        {
            query = query.Where(sop => sop.ServiceOrderId == serviceOrderId.Value);
        }

        return await query.ToListAsync();
    }

    public async Task<ServiceOrderPart?> FindWithDetailsAsync(Guid id)
    {
        return await _context.ServiceOrderParts
            .Include(x => x.ServiceOrder)
                .ThenInclude(so => so!.Vehicle)
            .Include(x => x.SparePart)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task RecalculateOrderTotalAsync(Guid serviceOrderId)
    {
        var order = await _context.ServiceOrders
            .Include(so => so.ServiceOrderItems)
            .Include(so => so.ServiceOrderParts)
            .FirstOrDefaultAsync(so => so.Id == serviceOrderId);

        if (order == null) return;

        var itemsTotal = order.ServiceOrderItems?.Sum(i => i.Quantity * i.UnitPrice) ?? 0;
        var partsTotal = order.ServiceOrderParts?.Sum(p => p.Quantity * p.UnitPrice) ?? 0;
        order.FinalPrice = itemsTotal + partsTotal;
        order.UpdatedAt = DateTime.UtcNow;

        _context.Entry(order).State = EntityState.Modified;
    }
}
