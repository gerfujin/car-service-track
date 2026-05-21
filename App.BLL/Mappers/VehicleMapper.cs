using App.BLL.DTO;
using App.Domain;

namespace App.BLL.Mappers;

public static class VehicleMapper
{
    public static BllVehicle? ToBll(Vehicle? entity)
    {
        if (entity == null) return null;

        return new BllVehicle
        {
            Id = entity.Id,
            Make = entity.Make,
            Model = entity.Model,
            Year = entity.Year,
            LicensePlate = entity.LicensePlate,
            Vin = entity.Vin,
            Mileage = entity.Mileage,
            Color = entity.Color,
            OwnerId = entity.OwnerId,
        };
    }

    public static Vehicle? ToDomain(BllVehicle? bll)
    {
        if (bll == null) return null;

        return new Vehicle
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
    }
}
