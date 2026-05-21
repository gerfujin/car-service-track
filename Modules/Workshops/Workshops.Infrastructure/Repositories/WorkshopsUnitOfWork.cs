using Workshops.Contracts;
using Workshops.Contracts.Repositories;

namespace Workshops.Infrastructure.Repositories;

public class WorkshopsUnitOfWork : IWorkshopsUnitOfWork
{
    private readonly WorkshopsDbContext _context;

    private IWorkshopRepository? _workshops;
    private IMechanicRepository? _mechanics;
    private IMechanicInWorkshopRepository? _mechanicsInWorkshops;
    private IServiceRepository? _services;
    private ISparePartRepository? _spareParts;

    public WorkshopsUnitOfWork(WorkshopsDbContext context)
    {
        _context = context;
    }

    public IWorkshopRepository Workshops =>
        _workshops ??= new WorkshopRepository(_context);

    public IMechanicRepository Mechanics =>
        _mechanics ??= new MechanicRepository(_context);

    public IMechanicInWorkshopRepository MechanicsInWorkshops =>
        _mechanicsInWorkshops ??= new MechanicInWorkshopRepository(_context);

    public IServiceRepository Services =>
        _services ??= new ServiceRepository(_context);

    public ISparePartRepository SpareParts =>
        _spareParts ??= new SparePartRepository(_context);

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}
