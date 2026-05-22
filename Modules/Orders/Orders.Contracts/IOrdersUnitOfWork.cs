using Base.Contracts;
using Orders.Contracts.Repositories;

namespace Orders.Contracts;

public interface IOrdersUnitOfWork : IBaseUnitOfWork
{
    IServiceOrderRepository ServiceOrders { get; }
    IServiceOrderPartRepository ServiceOrderParts { get; }
    IServiceOrderStatusHistoryRepository StatusHistories { get; }
    IPaymentRepository Payments { get; }
    IRepairPhotoRepository RepairPhotos { get; }
}
