using App.DTO.v1.Workshop;
using Workshops.Application.DTO;

namespace Workshops.Presentation.Mappers;

/// <summary>Maps the module BLL DTO (Workshops.Application.DTO.BllWorkshop) to the API DTO (App.DTO.v1.Workshop.WorkshopDto).</summary>
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
