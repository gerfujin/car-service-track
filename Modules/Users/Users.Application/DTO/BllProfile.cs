namespace Users.Application.DTO;

public class BllProfile
{
    public Guid AppUserId { get; set; }
    public string Email { get; set; } = default!;
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string? Address { get; set; }
    public string? Phone { get; set; }
}
