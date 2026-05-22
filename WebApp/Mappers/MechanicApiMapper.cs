using App.DTO.v1.Mechanic;
using Workshops.Application.DTO;

namespace WebApp.Mappers;

public static class MechanicApiMapper
{
    public static MechanicDto ToApiDto(BllMechanic mechanic) => new()
    {
        Id = mechanic.Id,
        FirstName = mechanic.FirstName,
        LastName = mechanic.LastName,
        FullName = mechanic.FirstName + " " + mechanic.LastName,
        Phone = mechanic.Phone,
        Email = mechanic.Email,
        Specialization = mechanic.Specialization,
    };
}
