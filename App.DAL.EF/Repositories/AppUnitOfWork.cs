using App.DAL.Contracts;

namespace App.DAL.EF.Repositories;

public class AppUnitOfWork : IAppUnitOfWork
{
    private readonly AppDbContext _context;

    private IServiceOrderRepository? _serviceOrders;
    private IVehicleRepository? _vehicles;
    private IServiceRepository? _services;
    private IWorkshopRepository? _workshops;
    private ISparePartRepository? _spareParts;
    private IServiceOrderStatusHistoryRepository? _statusHistories;
    private IPaymentRepository? _payments;
    private IOwnerRepository? _owners;
    private IMechanicRepository? _mechanics;
    private IServiceOrderPartRepository? _serviceOrderParts;
    private IRepairPhotoRepository? _repairPhotos;

    public AppUnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public IServiceOrderRepository ServiceOrders =>
        _serviceOrders ??= new ServiceOrderRepository(_context);

    public IVehicleRepository Vehicles =>
        _vehicles ??= new VehicleRepository(_context);

    public IServiceRepository Services =>
        _services ??= new ServiceRepository(_context);

    public IWorkshopRepository Workshops =>
        _workshops ??= new WorkshopRepository(_context);

    public ISparePartRepository SpareParts =>
        _spareParts ??= new SparePartRepository(_context);

    public IServiceOrderStatusHistoryRepository StatusHistories =>
        _statusHistories ??= new ServiceOrderStatusHistoryRepository(_context);

    public IPaymentRepository Payments =>
        _payments ??= new PaymentRepository(_context);

    public IOwnerRepository Owners =>
        _owners ??= new OwnerRepository(_context);

    public IMechanicRepository Mechanics =>
        _mechanics ??= new MechanicRepository(_context);

    public IServiceOrderPartRepository ServiceOrderParts =>
        _serviceOrderParts ??= new ServiceOrderPartRepository(_context);

    public IRepairPhotoRepository RepairPhotos =>
        _repairPhotos ??= new RepairPhotoRepository(_context);

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}
