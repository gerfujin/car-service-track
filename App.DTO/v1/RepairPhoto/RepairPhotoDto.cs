namespace App.DTO.v1.RepairPhoto;

public class RepairPhotoDto
{
    public Guid Id { get; set; }
    public string? Description { get; set; }
    
    /// <summary>
    /// Relative URL to the photo file, e.g. "/uploads/repair-photos/{filename}".
    /// Security note: files in wwwroot/uploads are publicly accessible by URL without auth.
    /// File names are GUIDs (unguessable), providing security through obscurity.
    /// In a production system, serve files through a protected endpoint with role checks.
    /// </summary>
    public string? PhotoUrl { get; set; }
    
    public DateTime UploadedAt { get; set; }
    public Guid ServiceOrderId { get; set; }
}
