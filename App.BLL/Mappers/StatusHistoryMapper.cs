using App.BLL.DTO;
using App.Domain;

namespace App.BLL.Mappers;

public static class StatusHistoryMapper
{
    public static BllStatusHistory? ToBll(ServiceOrderStatusHistory? entity)
    {
        if (entity == null) return null;
        return new BllStatusHistory
        {
            Id = entity.Id,
            ServiceOrderId = entity.ServiceOrderId,
            Status = entity.Status,
            Notes = entity.Notes,
            ChangedAt = entity.ChangedAt
        };
    }

    public static ServiceOrderStatusHistory? ToDomain(BllStatusHistory? bll)
    {
        if (bll == null) return null;
        return new ServiceOrderStatusHistory
        {
            Id = bll.Id,
            ServiceOrderId = bll.ServiceOrderId,
            Status = bll.Status,
            Notes = bll.Notes,
            ChangedAt = bll.ChangedAt
        };
    }
}
