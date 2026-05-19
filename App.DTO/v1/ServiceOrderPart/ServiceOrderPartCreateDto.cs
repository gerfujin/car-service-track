using System.ComponentModel.DataAnnotations;

namespace App.DTO.v1.ServiceOrderPart;

public class ServiceOrderPartCreateDto
{
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    [Required]
    public Guid SparePartId { get; set; }

    [Required]
    public Guid ServiceOrderId { get; set; }
}
