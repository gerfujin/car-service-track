using Orders.Contracts;
using Orders.Contracts.Repositories;

namespace Orders.Infrastructure.Repositories;

public class OrdersUnitOfWork : IOrdersUnitOfWork
{
    private readonly OrdersDbContext _context;

    private IServiceOrderRepository? _serviceOrders;
    private IServiceOrderPartRepository? _serviceOrderParts;
    private IServiceOrderStatusHistoryRepository? _statusHistories;
    private IPaymentRepository? _payments;
    private IRepairPhotoRepository? _repairPhotos;

    public OrdersUnitOfWork(OrdersDbContext context)
    {
        _context = context;
    }

    public IServiceOrderRepository ServiceOrders =>
        _serviceOrders ??= new ServiceOrderRepository(_context);

    public IServiceOrderPartRepository ServiceOrderParts =>
        _serviceOrderParts ??= new ServiceOrderPartRepository(_context);

    public IServiceOrderStatusHistoryRepository StatusHistories =>
        _statusHistories ??= new ServiceOrderStatusHistoryRepository(_context);

    public IPaymentRepository Payments =>
        _payments ??= new PaymentRepository(_context);

    public IRepairPhotoRepository RepairPhotos =>
        _repairPhotos ??= new RepairPhotoRepository(_context);

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}
