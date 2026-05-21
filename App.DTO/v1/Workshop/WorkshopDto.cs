namespace App.DTO.v1.Workshop;

public class WorkshopDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Address { get; set; } = default!;
    public string? Phone { get; set; }
    public string? Email { get; set; }
}
