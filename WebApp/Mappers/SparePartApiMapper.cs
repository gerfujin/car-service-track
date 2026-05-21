using App.BLL.DTO;
using App.DTO.v1.SparePart;

namespace WebApp.Mappers;

/// <summary>
/// Maps between BllSparePart and the API DTOs. Note the original controller's quirks that are
/// preserved here byte-for-byte:
///  - the DTO has no real "Manufacturer" column; it aliases the entity's PartNumber.
///  - GET maps BOTH PartNumber and Manufacturer from PartNumber.
///  - the Create response sets ONLY Manufacturer (leaving PartNumber null).
///  - Price (DTO) maps from UnitPrice (entity/BLL).
/// </summary>
public static class SparePartApiMapper
{
    /// <summary>GET shape — PartNumber and Manufacturer both come from PartNumber.</summary>
    public static SparePartDto ToApiDto(BllSparePart sp) => new()
    {
        Id = sp.Id,
        Name = sp.Name,
        PartNumber = sp.PartNumber,
        Manufacturer = sp.PartNumber,
        Price = sp.UnitPrice,
        Country = sp.Country,
        StockQuantity = sp.StockQuantity,
    };

    /// <summary>Create-response shape — only Manufacturer is set; PartNumber is left null.</summary>
    public static SparePartDto ToCreatedApiDto(BllSparePart sp) => new()
    {
        Id = sp.Id,
        Name = sp.Name,
        Manufacturer = sp.PartNumber,
        Price = sp.UnitPrice,
        Country = sp.Country,
        StockQuantity = sp.StockQuantity,
    };

    public static BllSparePart ToBll(SparePartCreateDto dto) => new()
    {
        Name = dto.Name,
        PartNumber = dto.Manufacturer, // DTO Manufacturer is stored in the entity's PartNumber
        Country = dto.Country,
        UnitPrice = dto.Price,
        StockQuantity = dto.StockQuantity,
    };

    public static BllSparePart ToBll(SparePartUpdateDto dto, Guid id) => new()
    {
        Id = id,
        Name = dto.Name,
        PartNumber = dto.Manufacturer,
        Country = dto.Country,
        UnitPrice = dto.Price,
        StockQuantity = dto.StockQuantity,
    };
}
