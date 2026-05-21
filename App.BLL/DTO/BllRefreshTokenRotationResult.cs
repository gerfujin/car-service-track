namespace App.BLL.DTO;

public class BllRefreshTokenRotationResult
{
    public int MatchingTokenCount { get; set; }
    public string EmptyCollectionCountText { get; set; } = "";
    public string? RefreshToken { get; set; }
    public bool Rotated { get; set; }
}
