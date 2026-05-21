using Base.Contracts;

namespace Users.Application.DTO;

public class BllVehicle : IBaseEntity
{
    public Guid Id { get; set; }

    public string Make { get; set; } = default!;
    public string Model { get; set; } = default!;
    public int Year { get; set; }
    public string LicensePlate { get; set; } = default!;
    public string? Vin { get; set; }
    public int? Mileage { get; set; }
    public string? Color { get; set; }

    public Guid OwnerId { get; set; }
    public int ServiceOrderCount { get; set; }
}
