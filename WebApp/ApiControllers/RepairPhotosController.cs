using App.DAL.EF;
using App.Domain;
using App.DTO.v1;
using App.DTO.v1.RepairPhoto;
using Asp.Versioning;
using Base.Helpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebApp.ApiControllers;

[ApiVersion("1.0")]
[ApiController]
[Route("/api/v{version:apiVersion}/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class RepairPhotosController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _env;

    public RepairPhotosController(AppDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    private Guid GetCurrentUserId()
    {
        var userId = IdentityHelpers.GetUserId(User);
        if (userId == null) throw new UnauthorizedAccessException();
        return userId.Value;
    }

    private async Task<Owner?> GetCurrentOwnerAsync()
    {
        var userId = GetCurrentUserId();
        return await _context.Owners.FirstOrDefaultAsync(o => o.AppUserId == userId);
    }

    private bool IsAdmin() => User.IsInRole("admin");
    private bool IsMechanic() => User.IsInRole("mechanic");

    private static RepairPhotoDto MapToDto(RepairPhoto p) => new()
    {
        Id = p.Id,
        Description = p.Description,
        PhotoUrl = p.FilePath,
        UploadedAt = p.UploadedAt,
        ServiceOrderId = p.ServiceOrderId
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
            return BadRequest(new RestApiErrorResponse
            {
                Status = System.Net.HttpStatusCode.BadRequest,
                Error = "serviceOrderId is required."
            });
        }

        IQueryable<RepairPhoto> query = _context.RepairPhotos
            .Include(p => p.ServiceOrder)
                .ThenInclude(so => so!.Vehicle)
            .Where(p => p.ServiceOrderId == serviceOrderId);

        if (!IsAdmin() && !IsMechanic())
        {
            var owner = await GetCurrentOwnerAsync();
            if (owner == null) return Ok(new List<RepairPhotoDto>());
            query = query.Where(p => p.ServiceOrder!.Vehicle!.OwnerId == owner.Id);
        }

        var photos = await query.ToListAsync();
        return Ok(photos.Select(MapToDto).ToList());
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
        IQueryable<RepairPhoto> query = _context.RepairPhotos
            .Include(p => p.ServiceOrder)
                .ThenInclude(so => so!.Vehicle)
            .Where(p => p.Id == id);

        if (!IsAdmin() && !IsMechanic())
        {
            var owner = await GetCurrentOwnerAsync();
            if (owner == null) return NotFound();
            query = query.Where(p => p.ServiceOrder!.Vehicle!.OwnerId == owner.Id);
        }

        var photo = await query.FirstOrDefaultAsync();
        if (photo == null) return NotFound();
        return Ok(MapToDto(photo));
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
        return BadRequest(new RestApiErrorResponse
        {
            Status = System.Net.HttpStatusCode.BadRequest,
            Error = "No file provided."
        });
    }

    if (file.Length > 15 * 1024 * 1024)
    {
        return BadRequest(new RestApiErrorResponse
        {
            Status = System.Net.HttpStatusCode.BadRequest,
            Error = "File exceeds maximum size of 15 MB."
        });
    }

    // --- Validate MIME type ---
    var allowedMimeTypes = new[] { "image/jpeg", "image/png" };
    if (!allowedMimeTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
    {
        return BadRequest(new RestApiErrorResponse
        {
            Status = System.Net.HttpStatusCode.BadRequest,
            Error = "Only JPEG and PNG images are allowed."
        });
    }

    // --- Validate extension ---
    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
    if (!allowedExtensions.Contains(extension))
    {
        return BadRequest(new RestApiErrorResponse
        {
            Status = System.Net.HttpStatusCode.BadRequest,
            Error = "Invalid file extension. Allowed: .jpg, .jpeg, .png"
        });
    }

    // --- Validate service order exists ---
    var orderExists = await _context.ServiceOrders.AnyAsync(so => so.Id == serviceOrderId);
    if (!orderExists)
    {
        return BadRequest(new RestApiErrorResponse
        {
            Status = System.Net.HttpStatusCode.BadRequest,
            Error = "Service order not found."
        });
    }

    // --- Validate photo count limit (max 20) ---
    var photoCount = await _context.RepairPhotos
        .CountAsync(p => p.ServiceOrderId == serviceOrderId);

    if (photoCount >= 20)
    {
        return BadRequest(new RestApiErrorResponse
        {
            Status = System.Net.HttpStatusCode.BadRequest,
            Error = "Maximum 20 photos per order."
        });
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
    var photo = new RepairPhoto
    {
        FilePath = relativeUrl,
        Description = description,
        UploadedAt = DateTime.UtcNow,
        ServiceOrderId = serviceOrderId
    };

    _context.RepairPhotos.Add(photo);
    await _context.SaveChangesAsync();

    return CreatedAtAction(nameof(GetPhoto), new { id = photo.Id }, MapToDto(photo));
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
        var photo = await _context.RepairPhotos
            .FirstOrDefaultAsync(p => p.Id == id);

        if (photo == null) return NotFound();

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
                // Swallow — disk errors should not block DB delete
            }
        }

        _context.RepairPhotos.Remove(photo);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
