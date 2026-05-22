using App.Domain.Enums;
using App.DTO.v1.Service;
using App.DTO.v1.ServiceOrder;
using Orders.Application.DTO;

namespace WebApp.Mappers;

/// <summary>
/// Maps the module BLL DTO (Orders.Application.DTO.BllServiceOrder) to the API DTO (ServiceOrderDto).
/// Mirrors the two shapes the old controller produced: the list omits the Services
/// detail section, the detail (get-by-id) includes it.
/// Note: Orders.Domain.Enums.ServiceOrderStatus and App.Domain.Enums.ServiceOrderStatus share
/// identical numeric values (0–5), so the int-cast is lossless.
/// </summary>
public static class ServiceOrderApiMapper
{
    /// <summary>List shape — same fields as the old list projection, Services left null.</summary>
    public static ServiceOrderDto ToApiSummaryDto(BllServiceOrder o) => new()
    {
        Id = o.Id,
        Description = o.Description,
        Status = (ServiceOrderStatus)(int)o.Status,
        OrderDate = o.OrderDate,
        CompletedDate = o.CompletedDate,
        VehicleId = o.VehicleId,
        VehicleDisplay = o.VehicleDisplay,
        WorkshopId = o.WorkshopId,
        WorkshopName = o.WorkshopName,
        MechanicId = o.MechanicId,
        MechanicName = o.MechanicName,
        TotalAmount = o.TotalAmount,
        FinalPrice = o.FinalPrice,
        // Services intentionally left null — matches the old list endpoint.
    };

    /// <summary>Detail shape — summary plus the Services section (matches the old get-by-id).</summary>
    public static ServiceOrderDto ToApiDto(BllServiceOrder o)
    {
        var dto = ToApiSummaryDto(o);
        dto.Services = o.Services?.Select(ToServiceDto).ToList() ?? new List<ServiceDto>();
        return dto;
    }

    private static ServiceDto ToServiceDto(BllServiceLine s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        Description = s.Description,
        BasePrice = s.BasePrice,
        EstimatedTimeMinutes = s.EstimatedTimeMinutes,
    };
}
