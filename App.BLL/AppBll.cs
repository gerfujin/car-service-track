using App.BLL.Services;
using App.DAL.Contracts;

namespace App.BLL;

public class AppBll : IAppBll
{
    private readonly IAppUnitOfWork _uow;

    private IVehicleService? _vehicles;
    private IServiceOrderService? _serviceOrders;
    private IServiceService? _services;
    private IWorkshopService? _workshops;
    private ISparePartService? _spareParts;
    private IStatusHistoryService? _statusHistories;
    private IPaymentService? _payments;
    private IOwnerService? _owners;
    private IMechanicService? _mechanics;
    private IServiceOrderPartService? _serviceOrderParts;
    private IRepairPhotoService? _repairPhotos;

    public AppBll(IAppUnitOfWork uow)
    {
        _uow = uow;
    }

    public IVehicleService Vehicles =>
        _vehicles ??= new VehicleService(_uow);

    public IServiceOrderService ServiceOrders =>
        _serviceOrders ??= new ServiceOrderService(_uow);

    public IServiceService Services =>
        _services ??= new ServiceService(_uow);

    public IWorkshopService Workshops =>
        _workshops ??= new WorkshopService(_uow);

    public ISparePartService SpareParts =>
        _spareParts ??= new SparePartService(_uow);

    public IStatusHistoryService StatusHistories =>
        _statusHistories ??= new StatusHistoryService(_uow);

    public IPaymentService Payments =>
        _payments ??= new PaymentService(_uow);

    public IOwnerService Owners =>
        _owners ??= new OwnerService(_uow);

    public IMechanicService Mechanics =>
        _mechanics ??= new MechanicService(_uow);

    public IServiceOrderPartService ServiceOrderParts =>
        _serviceOrderParts ??= new ServiceOrderPartService(_uow);

    public IRepairPhotoService RepairPhotos =>
        _repairPhotos ??= new RepairPhotoService(_uow);

    public async Task<int> SaveChangesAsync()
    {
        return await _uow.SaveChangesAsync();
    }
}
