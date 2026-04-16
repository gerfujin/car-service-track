using System.ComponentModel.DataAnnotations;

namespace App.DTO.v1.Vehicle;

public class VehicleCreateDto
{
    [Required, MaxLength(64)]
    public string Make { get; set; } = default!;

    [Required, MaxLength(64)]
    public string Model { get; set; } = default!;

    [Range(1900, 2100)]
    public int Year { get; set; }

    [Required, MaxLength(32)]
    public string LicensePlate { get; set; } = default!;

    [MaxLength(17)]
    public string? Vin { get; set; }

    public int? Mileage { get; set; }

    [MaxLength(32)]
    public string? Color { get; set; }
}
