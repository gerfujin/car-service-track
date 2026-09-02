using Microsoft.AspNetCore.Http;

namespace WebApp.ApiControllers;

public class RepairPhotoUploadRequest
{
    public Guid ServiceOrderId { get; set; }

    public IFormFile File { get; set; } = default!;

    public string? Description { get; set; }
}