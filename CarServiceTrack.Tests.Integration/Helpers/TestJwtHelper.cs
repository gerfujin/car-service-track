using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace CarServiceTrack.Tests.Integration.Helpers;

/// <summary>
/// Generates signed JWT tokens for use in integration tests.
/// The key/issuer/audience must match what CustomWebApplicationFactory injects
/// into the application configuration so bearer validation accepts the tokens.
/// </summary>
public static class TestJwtHelper
{
    // These constants are re-used by CustomWebApplicationFactory to override appsettings.
    public const string Key = "test_integration_secret_key_min32chars!!";
    public const string Issuer = "carservicetrack.test";
    public const string Audience = "carservicetrack.test";

    /// <summary>
    /// Build a signed JWT with the supplied identity claims.
    /// </summary>
    /// <param name="userId">The user's Guid (becomes the NameIdentifier claim).</param>
    /// <param name="email">The user's email (becomes the Email claim).</param>
    /// <param name="roles">Role names (e.g. "admin", "client", "mechanic").</param>
    /// <param name="expiresInSeconds">Token lifetime in seconds; defaults to 1 hour.</param>
    public static string GenerateToken(
        Guid userId,
        string email,
        IEnumerable<string> roles,
        int expiresInSeconds = 3600)
    {
        var claims = new List<Claim>
        {
            // ClaimTypes.NameIdentifier / ClaimTypes.Email map to the full URI forms
            // that ASP.NET Identity uses and that IdentityHelpers.GetUserId() expects.
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email),
        };

        foreach (var role in roles)
        {
            // Role claim type must be the full URI used by ASP.NET Identity so that
            // User.IsInRole() and [Authorize(Roles = "...")] resolve correctly when
            // DefaultInboundClaimTypeMap is cleared (as in Program.cs).
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key));
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddSeconds(expiresInSeconds),
            signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Authorization header value for a client-role user.
    /// Role value MUST be lowercase "client" to match the canonical seed data
    /// (InitialData.cs) and every API controller's [Authorize(Roles = "...,client")].
    /// Role matching is case-sensitive, so "Client" would be rejected with 403.
    /// </summary>
    public static string ClientBearer(Guid userId, string email) =>
        "Bearer " + GenerateToken(userId, email, new[] { "client" });

    /// <summary>Authorization header value for an admin-role user.</summary>
    public static string AdminBearer(Guid userId, string email) =>
        "Bearer " + GenerateToken(userId, email, new[] { "admin" });
}
