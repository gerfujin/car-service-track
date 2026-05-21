using Base.Contracts;

namespace Workshops.Application.DTO;

public class BllService : IBaseEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public decimal BasePrice { get; set; }
    public int EstimatedTimeMinutes { get; set; }
}
