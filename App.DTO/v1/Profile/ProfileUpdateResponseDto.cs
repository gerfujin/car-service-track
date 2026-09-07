namespace App.DTO.v1.Profile;

/// <summary>
/// Returned by PUT profile: the updated profile plus a freshly issued JWT carrying the
/// updated name claims, so the caller can update the navbar without a full re-login.
/// </summary>
public class ProfileUpdateResponseDto
{
    public ProfileDto Profile { get; set; } = default!;
    public string Jwt { get; set; } = default!;
}
