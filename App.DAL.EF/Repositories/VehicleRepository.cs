using App.DAL.Contracts;
using App.Domain;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories;

public class VehicleRepository : BaseRepository<Vehicle>, IVehicleRepository
{
    public VehicleRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Vehicle>> AllByUserAsync(Guid appUserId)
    {
        return await _context.Vehicles
            .Where(v => v.Owner!.AppUserId == appUserId)
            .ToListAsync();
    }

    public async Task<Vehicle?> FindByUserAsync(Guid id, Guid appUserId)
    {
        return await _context.Vehicles
            .FirstOrDefaultAsync(v => v.Id == id && v.Owner!.AppUserId == appUserId);
    }
}
