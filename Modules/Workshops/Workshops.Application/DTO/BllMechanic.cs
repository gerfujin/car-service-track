using Base.Contracts;

namespace Workshops.Application.DTO;

public class BllMechanic : IBaseEntity
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Specialization { get; set; }
    public Guid AppUserId { get; set; }
}
