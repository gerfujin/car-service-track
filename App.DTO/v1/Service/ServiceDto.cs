namespace App.DTO.v1.Service;

public class ServiceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public decimal BasePrice { get; set; }
    public int EstimatedTimeMinutes { get; set; }
}

public class ServiceCreateDto
{
    public string Name { get; set; } = default!;
    public string Description { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public int EstimatedTimeMinutes { get; set; }
}

public class ServiceUpdateDto
{
    public string Name { get; set; } = default!;
    public string Description { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public int EstimatedTimeMinutes { get; set; }
}
