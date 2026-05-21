namespace Users.Contracts.Queries;

public record VehicleDto(
    Guid Id,
    string Make,
    string Model,
    int Year,
    string LicensePlate,
    string? Vin,
    int? Mileage,
    string? Color,
    Guid OwnerId
);
