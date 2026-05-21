using App.BLL;
using App.BLL.DTO;
using App.DTO.v1;
using App.DTO.v1.RepairPhoto;
using Asp.Versioning;
using Base.Helpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Mappers;

namespace WebApp.ApiControllers;

[ApiVersion("1.0")]
[ApiController]
[Route("/api/v{version:apiVersion}/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class RepairPhotosController : ControllerBase
{
    private readonly IAppBll _bll;
    private readonly IWebHostEnvironment _env;

    public RepairPhotosController(IAppBll bll, IWebHostEnvironment env)
    {
        _bll = bll;
        _env = env;
    }

    private Guid GetCurrentUserId()
    {
        var userId = IdentityHelpers.GetUserId(User);
        if (userId == null) throw new UnauthorizedAccessException();
        return userId.Value;
    }

    private bool IsAdmin() => User.IsInRole("admin");
    private bool IsMechanic() => User.IsInRole("mechanic");

    private static RestApiErrorResponse ErrorResponse(string error) => new()
    {
        Status = System.Net.HttpStatusCode.BadRequest,
        Error = error
    };

    /// <summary>
    /// Get photos for a service order.
    /// Admin/Mechanic: any order.
    /// Owner: only own order (IDOR via Vehicle.OwnerId).
    /// </summary>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType<IEnumerable<RepairPhotoDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<RepairPhotoDto>>> GetPhotos([FromQuery] Guid serviceOrderId)
    {
        if (serviceOrderId == Guid.Empty)
        {
            return BadRequest(ErrorResponse("serviceOrderId is required."));
        }

        var isAdmin = IsAdmin();
        var isMechanic = IsMechanic();
        var appUserId = isAdmin || isMechanic ? Guid.Empty : GetCurrentUserId();

        var photos = (await _bll.RepairPhotos.AllForApiAsync(serviceOrderId, appUserId, isAdmin, isMechanic))
            .Select(RepairPhotoApiMapper.ToApiDto)
            .ToList();

        return Ok(photos);
    }

    /// <summary>
    /// Get a single repair photo by ID.
    /// Admin/Mechanic: any photo.
    /// Owner: only own (IDOR).
    /// </summary>
    [HttpGet("{id:guid}")]
    [Produces("application/json")]
    [ProducesResponseType<RepairPhotoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RepairPhotoDto>> GetPhoto(Guid id)
    {
        var isAdmin = IsAdmin();
        var isMechanic = IsMechanic();
        var appUserId = isAdmin || isMechanic ? Guid.Empty : GetCurrentUserId();

        var photo = await _bll.RepairPhotos.FindForApiAsync(id, appUserId, isAdmin, isMechanic);
        if (photo == null) return NotFound();
        return Ok(RepairPhotoApiMapper.ToApiDto(photo));
    }

    /// <summary>
    /// Upload a new repair photo for a service order.
    /// Admin/Mechanic only. Max 15 MB, JPG/PNG, max 20 photos per order.
    /// </summary>
    /// <summary>
    /// Upload a new repair photo for a service order.
    /// Admin/Mechanic only. Max 15 MB, JPG/PNG, max 20 photos per order.
    /// </summary>
    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "admin,mechanic")]
    [Produces("application/json")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<RepairPhotoDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<RestApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(16 * 1024 * 1024)]
    public async Task<ActionResult<RepairPhotoDto>> UploadPhoto([FromForm] RepairPhotoUploadRequest request)
    {
        var file = request.File;
        var serviceOrderId = request.ServiceOrderId;
        var description = request.Description;

        // --- Validate file presence and size ---
        if (file == null || file.Length == 0)
        {
            return BadRequest(ErrorResponse("No file provided."));
        }

        if (file.Length > 15 * 1024 * 1024)
        {
            return BadRequest(ErrorResponse("File exceeds maximum size of 15 MB."));
        }

        // --- Validate MIME type ---
        var allowedMimeTypes = new[] { "image/jpeg", "image/png" };
        if (!allowedMimeTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest(ErrorResponse("Only JPEG and PNG images are allowed."));
        }

        // --- Validate extension ---
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
        if (!allowedExtensions.Contains(extension))
        {
            return BadRequest(ErrorResponse("Invalid file extension. Allowed: .jpg, .jpeg, .png"));
        }

        // --- Validate service order exists and photo count limit ---
        var validation = await _bll.RepairPhotos.ValidateUploadAsync(serviceOrderId);
        if (validation.Error != null)
        {
            return BadRequest(ErrorResponse(validation.Error));
        }

        // --- Save file to disk ---
        var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "repair-photos");
        Directory.CreateDirectory(uploadsFolder);

        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // --- Persist DB record ---
        var relativeUrl = $"/uploads/repair-photos/{fileName}";
        var photo = _bll.RepairPhotos.AddUploaded(new BllRepairPhoto
        {
            FilePath = relativeUrl,
            Description = description,
            ServiceOrderId = serviceOrderId
        });

        await _bll.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPhoto), new { id = photo.Id }, RepairPhotoApiMapper.ToApiDto(photo));
    }

    /// <summary>
    /// Delete a repair photo.
    /// Admin/Mechanic only. Removes file from disk and DB record.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "admin,mechanic")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePhoto(Guid id)
    {
        var result = await _bll.RepairPhotos.RemoveForApiAsync(id);
        if (result.NotFound) return NotFound();

        var photo = result.Entity!;

        // --- Try to delete file from disk (non-fatal if missing) ---
        if (!string.IsNullOrEmpty(photo.FilePath))
        {
            try
            {
                // FilePath is relative like "/uploads/repair-photos/{file}"
                var absolutePath = Path.Combine(_env.WebRootPath,
                    photo.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(absolutePath))
                {
                    System.IO.File.Delete(absolutePath);
                }
            }
            catch (Exception)
            {
                // Swallow - disk errors should not block DB delete
            }
        }

        await _bll.SaveChangesAsync();

        return NoContent();
    }
}
