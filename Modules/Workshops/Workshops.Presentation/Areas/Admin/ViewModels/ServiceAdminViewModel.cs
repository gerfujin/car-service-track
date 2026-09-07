using System.ComponentModel.DataAnnotations;

namespace Workshops.Presentation.Areas.Admin.ViewModels;

public class ServiceAdminViewModel : AdminPageViewModelBase
{
    public Guid Id { get; set; }

    [Required]
    [Display(Name = "Service Name")]
    public string Name { get; set; } = default!;

    [Display(Name = "Description")]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Range(0, 99999.99)]
    [Display(Name = "Base Price (€)")]
    public decimal BasePrice { get; set; }

    [Required]
    [Range(0, int.MaxValue)]
    [Display(Name = "Estimated Time (minutes)")]
    public int EstimatedTimeMinutes { get; set; }
}

public class ServiceAdminListViewModel : AdminPageViewModelBase
{
    public List<ServiceAdminViewModel> Services { get; set; } = new();
}
