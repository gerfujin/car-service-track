using Base.Contracts;

namespace App.DAL.Contracts;

public interface IAppUnitOfWork : IBaseUnitOfWork
{
    public IServiceOrderRepository ServiceOrders { get; }
    public IVehicleRepository Vehicles { get; }
    public IServiceRepository Services { get; }
    public IWorkshopRepository Workshops { get; }
    public ISparePartRepository SpareParts { get; }
    public IServiceOrderStatusHistoryRepository StatusHistories { get; }
    public IPaymentRepository Payments { get; }
    public IOwnerRepository Owners { get; }
    public IMechanicRepository Mechanics { get; }
    public IServiceOrderPartRepository ServiceOrderParts { get; }
    public IRepairPhotoRepository RepairPhotos { get; }
}
