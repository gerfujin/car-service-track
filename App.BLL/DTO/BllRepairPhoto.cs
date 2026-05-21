using Base.Contracts;

namespace App.BLL.DTO;

public class BllRepairPhoto : IBaseEntity
{
    public Guid Id { get; set; }
    public string? FilePath { get; set; }
    public string? Description { get; set; }
    public DateTime UploadedAt { get; set; }
    public Guid ServiceOrderId { get; set; }

    // Read-side ownership projection used for API IDOR checks. Not exposed by API DTOs.
    public Guid? OwnerId { get; set; }
}
