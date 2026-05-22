using App.DTO.v1.RepairPhoto;
using Orders.Application.DTO;

namespace WebApp.Mappers;

public static class RepairPhotoApiMapper
{
    public static RepairPhotoDto ToApiDto(BllRepairPhoto photo) => new()
    {
        Id = photo.Id,
        Description = photo.Description,
        PhotoUrl = photo.FilePath,
        UploadedAt = photo.UploadedAt,
        ServiceOrderId = photo.ServiceOrderId,
    };
}
