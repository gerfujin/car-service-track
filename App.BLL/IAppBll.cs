using App.BLL.Services;

namespace App.BLL;

public interface IAppBll
{
    public IVehicleService Vehicles { get; }
    public IServiceOrderService ServiceOrders { get; }
    public IServiceService Services { get; }
    public IWorkshopService Workshops { get; }
    public ISparePartService SpareParts { get; }
    public IStatusHistoryService StatusHistories { get; }
    public IPaymentService Payments { get; }
    public IOwnerService Owners { get; }
    public IMechanicService Mechanics { get; }
    public IServiceOrderPartService ServiceOrderParts { get; }
    public IRepairPhotoService RepairPhotos { get; }
    public IRefreshTokenService RefreshTokens { get; }
    public IListItemService ListItems { get; }

    /// <summary>
    /// Persists all staged changes. The services only stage via the repositories;
    /// this is the single place changes are written (delegates to the DAL UnitOfWork).
    /// </summary>
    public Task<int> SaveChangesAsync();
}
