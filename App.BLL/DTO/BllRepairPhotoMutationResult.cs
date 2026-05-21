namespace App.BLL.DTO;

public class BllRepairPhotoMutationResult
{
    public BllRepairPhoto? Entity { get; set; }
    public bool NotFound { get; set; }
    public string? Error { get; set; }
}
