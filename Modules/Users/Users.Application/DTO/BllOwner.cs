using Base.Contracts;

namespace Users.Application.DTO;

public class BllOwner : IBaseEntity
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid AppUserId { get; set; }
}
