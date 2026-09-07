namespace Users.Application.DTO;

public class BllProfileUpdate
{
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string? Address { get; set; }
    public string? Phone { get; set; }
}
