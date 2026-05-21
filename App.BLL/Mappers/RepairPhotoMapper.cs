using App.BLL.DTO;
using App.Domain;

namespace App.BLL.Mappers;

public static class RepairPhotoMapper
{
    public static BllRepairPhoto? ToBll(RepairPhoto? entity)
    {
        if (entity == null) return null;

        return new BllRepairPhoto
        {
            Id = entity.Id,
            FilePath = entity.FilePath,
            Description = entity.Description,
            UploadedAt = entity.UploadedAt,
            ServiceOrderId = entity.ServiceOrderId,
            OwnerId = entity.ServiceOrder?.Vehicle?.OwnerId,
        };
    }

    public static RepairPhoto? ToDomain(BllRepairPhoto? bll)
    {
        if (bll == null) return null;

        return new RepairPhoto
        {
            Id = bll.Id,
            FilePath = bll.FilePath,
            Description = bll.Description,
            UploadedAt = bll.UploadedAt,
            ServiceOrderId = bll.ServiceOrderId,
        };
    }
}
