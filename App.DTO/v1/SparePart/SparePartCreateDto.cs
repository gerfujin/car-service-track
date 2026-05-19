using System.ComponentModel.DataAnnotations;

namespace App.DTO.v1.SparePart;

public class SparePartCreateDto
{
    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = default!;

    public string? Manufacturer { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    public string? Country { get; set; }

    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }
}
