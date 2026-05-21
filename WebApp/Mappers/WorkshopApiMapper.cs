using App.BLL.DTO;
using App.DTO.v1.Workshop;

namespace WebApp.Mappers;

/// <summary>Maps the BLL DTO (BllWorkshop) to the API DTO (App.DTO.v1.Workshop.WorkshopDto).</summary>
public static class WorkshopApiMapper
{
    public static WorkshopDto ToApiDto(BllWorkshop w) => new()
    {
        Id = w.Id,
        Name = w.Name,
        Address = w.Address,
        Phone = w.Phone,
        Email = w.Email,
    };
}
