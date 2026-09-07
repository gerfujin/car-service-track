using System.ComponentModel.DataAnnotations;

namespace App.DTO.v1.Profile;

public class ProfileUpdateDto
{
    [Required, MaxLength(128)]
    public string FirstName { get; set; } = default!;

    [Required, MaxLength(128)]
    public string LastName { get; set; } = default!;

    [MaxLength(256)]
    public string? Address { get; set; }

    [MaxLength(32)]
    public string? Phone { get; set; }
}
