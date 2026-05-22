namespace Orders.Application.DTO;

/// <summary>
/// Read-side projection of a Service attached to an order (via ServiceOrderItem),
/// carrying the details the API needs to render the order's services section.
/// </summary>
public class BllServiceLine
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public decimal BasePrice { get; set; }
    public int EstimatedTimeMinutes { get; set; }
}
