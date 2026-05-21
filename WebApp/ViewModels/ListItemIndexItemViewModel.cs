namespace WebApp.ViewModels;

public class ListItemIndexItemViewModel
{
    public Guid Id { get; set; }
    public string ItemDescription { get; set; } = default!;
    public string Summary { get; set; } = default!;
    public bool IsDone { get; set; }
    public string? AppUserEmail { get; set; }
}
