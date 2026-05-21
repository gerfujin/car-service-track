using App.BLL.DTO;
using App.DTO.v1.Service;

namespace WebApp.Mappers;

/// <summary>Maps between the BLL DTO (BllService) and the API DTOs (App.DTO.v1.Service.*).</summary>
public static class ServiceApiMapper
{
    public static ServiceDto ToApiDto(BllService s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        Description = s.Description,
        BasePrice = s.BasePrice,
        EstimatedTimeMinutes = s.EstimatedTimeMinutes,
    };

    public static BllService ToBll(ServiceCreateDto dto) => new()
    {
        Name = dto.Name,
        Description = dto.Description,
        BasePrice = dto.BasePrice,
        EstimatedTimeMinutes = dto.EstimatedTimeMinutes,
    };

    public static BllService ToBll(ServiceUpdateDto dto, Guid id) => new()
    {
        Id = id,
        Name = dto.Name,
        Description = dto.Description,
        BasePrice = dto.BasePrice,
        EstimatedTimeMinutes = dto.EstimatedTimeMinutes,
    };
}
