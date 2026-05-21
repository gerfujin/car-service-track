using App.BLL.DTO;
using App.DTO.v1.Vehicle;

namespace WebApp.Mappers;

/// <summary>
/// Maps between the BLL DTO (BllVehicle) and the API DTOs (App.DTO.v1.Vehicle.*).
/// Keeps the controller free of repetitive field-by-field mapping.
/// </summary>
public static class VehicleApiMapper
{
    public static VehicleDto ToApiDto(BllVehicle bll) => new()
    {
        Id = bll.Id,
        Make = bll.Make,
        Model = bll.Model,
        Year = bll.Year,
        LicensePlate = bll.LicensePlate,
        Vin = bll.Vin,
        Mileage = bll.Mileage,
        Color = bll.Color,
        OwnerId = bll.OwnerId,
    };

    public static BllVehicle ToBll(VehicleCreateDto dto, Guid ownerId) => new()
    {
        Make = dto.Make,
        Model = dto.Model,
        Year = dto.Year,
        LicensePlate = dto.LicensePlate,
        Vin = dto.Vin,
        Mileage = dto.Mileage,
        Color = dto.Color,
        OwnerId = ownerId,
    };

    /// <summary>
    /// Applies the editable fields from a create/update DTO onto an existing BLL DTO,
    /// preserving Id and OwnerId. Mirrors the field set the controller used to update.
    /// </summary>
    public static void ApplyUpdate(BllVehicle target, VehicleCreateDto dto)
    {
        target.Make = dto.Make;
        target.Model = dto.Model;
        target.Year = dto.Year;
        target.LicensePlate = dto.LicensePlate;
        target.Vin = dto.Vin;
        target.Mileage = dto.Mileage;
        target.Color = dto.Color;
    }
}
