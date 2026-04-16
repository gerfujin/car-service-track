using System.ComponentModel.DataAnnotations;

namespace App.DTO.v1.ServiceOrder;

public class ServiceOrderCreateDto
{
    [MaxLength(512)]
    public string? Description { get; set; }

    [Required]
    public Guid VehicleId { get; set; }

    [Required]
    public Guid WorkshopId { get; set; }
}
