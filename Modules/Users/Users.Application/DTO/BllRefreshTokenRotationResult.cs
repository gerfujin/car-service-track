namespace Users.Application.DTO;

public class BllRefreshTokenRotationResult
{
    public int MatchingTokenCount { get; set; }
    public string EmptyCollectionCountText { get; set; } = "";
    public bool Rotated { get; set; }
    public string? RefreshToken { get; set; }
}
