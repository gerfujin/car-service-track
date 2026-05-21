using Base.Contracts;

namespace Workshops.Application.DTO;

public class BllWorkshop : IBaseEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = default!;
    public string Address { get; set; } = default!;
    public string? Phone { get; set; }
    public string? Email { get; set; }
}
