namespace App.DTO.v1.Service;

public class ServiceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public decimal BasePrice { get; set; }
}
