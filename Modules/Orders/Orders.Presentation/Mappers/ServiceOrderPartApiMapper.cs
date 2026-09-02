using App.DTO.v1.ServiceOrderPart;
using Orders.Application.DTO;

namespace WebApp.Mappers;

public static class ServiceOrderPartApiMapper
{
    public static ServiceOrderPartDto ToApiDto(BllServiceOrderPart part) => new()
    {
        Id = part.Id,
        ServiceOrderId = part.ServiceOrderId,
        SparePartId = part.SparePartId,
        SparePartName = part.SparePartName,
        SparePartPartNumber = part.SparePartPartNumber,
        Quantity = part.Quantity,
        Price = part.UnitPrice,
        LineTotal = part.Quantity * part.UnitPrice,
    };

    public static BllServiceOrderPart ToBll(ServiceOrderPartCreateDto dto) => new()
    {
        ServiceOrderId = dto.ServiceOrderId,
        SparePartId = dto.SparePartId,
        Quantity = dto.Quantity,
        UnitPrice = dto.Price,
    };

    public static BllServiceOrderPart ToBll(ServiceOrderPartUpdateDto dto, Guid id) => new()
    {
        Id = id,
        ServiceOrderId = dto.ServiceOrderId,
        SparePartId = dto.SparePartId,
        Quantity = dto.Quantity,
        UnitPrice = dto.Price,
    };
}
