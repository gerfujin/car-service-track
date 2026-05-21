using App.Domain;
using Base.Contracts;

namespace App.DAL.Contracts;

public interface IServiceOrderPartRepository : IBaseRepository<ServiceOrderPart>
{
    Task<IEnumerable<ServiceOrderPart>> AllWithDetailsAsync(Guid? serviceOrderId);
    Task<ServiceOrderPart?> FindWithDetailsAsync(Guid id);

    /// <summary>
    /// Recalculates ServiceOrder.FinalPrice from service items and spare parts, and stamps UpdatedAt.
    /// Stages only - caller persists via SaveChangesAsync.
    /// </summary>
    Task RecalculateOrderTotalAsync(Guid serviceOrderId);
}
