using System.Net;
using System.Security.Claims;
using App.DTO.v1;
using App.DTO.v1.Profile;
using Asp.Versioning;
using Base.Helpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Users.Application.Services;
using Users.Domain.Identity;
using Users.Presentation.Mappers;

namespace Users.Presentation.ApiControllers;

/// <summary>
/// Current-user profile endpoints. Always operates on the caller resolved from the JWT
/// (ClaimTypes.NameIdentifier via IdentityHelpers.GetUserId) — never on an id from the client.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Route("/api/v{version:apiVersion}/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profile;
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly IConfiguration _configuration;

    public ProfileController(IProfileService profile, UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager, IConfiguration configuration)
    {
        _profile = profile;
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
    }

    private Guid GetCurrentUserId()
    {
        var userId = IdentityHelpers.GetUserId(User);
        if (userId == null) throw new UnauthorizedAccessException();
        return userId.Value;
    }

    /// <summary>
    /// Get the authenticated user's own profile (AppUser email + Owner name/contact fields).
    /// </summary>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType<ProfileDto>((int) HttpStatusCode.OK)]
    [ProducesResponseType((int) HttpStatusCode.Unauthorized)]
    [ProducesResponseType((int) HttpStatusCode.NotFound)]
    public async Task<ActionResult<ProfileDto>> GetProfile()
    {
        var profile = await _profile.GetProfileAsync(GetCurrentUserId());
        if (profile == null) return NotFound();

        return Ok(ProfileApiMapper.ToApiDto(profile));
    }

    /// <summary>
    /// Update the authenticated user's own profile.
    /// Whitelisted fields only: FirstName, LastName, Address, Phone.
    /// Email, Role, Id and password are never touched here.
    /// Returns a freshly issued JWT carrying the updated name claims.
    /// </summary>
    [HttpPut]
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType<ProfileUpdateResponseDto>((int) HttpStatusCode.OK)]
    [ProducesResponseType<RestApiErrorResponse>((int) HttpStatusCode.BadRequest)]
    [ProducesResponseType((int) HttpStatusCode.Unauthorized)]
    [ProducesResponseType((int) HttpStatusCode.NotFound)]
    public async Task<ActionResult<ProfileUpdateResponseDto>> UpdateProfile([FromBody] ProfileUpdateDto dto)
    {
        var userId = GetCurrentUserId();

        var updated = await _profile.UpdateProfileAsync(userId, ProfileApiMapper.ToBll(dto));
        if (updated == null) return NotFound();

        var appUser = await _userManager.FindByIdAsync(userId.ToString());
        if (appUser == null) return NotFound();

        var claimsPrincipal = await _signInManager.CreateUserPrincipalAsync(appUser);
        if (claimsPrincipal.Identity is ClaimsIdentity identity)
        {
            identity.AddClaim(new Claim(ClaimTypes.GivenName, updated.FirstName));
            identity.AddClaim(new Claim(ClaimTypes.Surname, updated.LastName));
        }

        var jwt = IdentityHelpers.GenerateJwt(
            claimsPrincipal.Claims,
            _configuration.GetValue<string>("JWT:Key")!,
            _configuration.GetValue<string>("JWT:Issuer")!,
            _configuration.GetValue<string>("JWT:Audience")!,
            _configuration.GetValue<int>("JWT:ExpiresInSeconds")
        );

        return Ok(new ProfileUpdateResponseDto
        {
            Profile = ProfileApiMapper.ToApiDto(updated),
            Jwt = jwt,
        });
    }
}
