using Base.Contracts;

namespace App.BLL.DTO;

public class BllMechanic : IBaseEntity
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Specialization { get; set; }
}
