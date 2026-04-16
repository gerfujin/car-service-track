using System.ComponentModel.DataAnnotations;

namespace WebApp.Areas.Admin.ViewModels;

public class ServiceAdminViewModel
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
}

public class ServiceAdminListViewModel
{
    public List<ServiceAdminViewModel> Services { get; set; } = new();
}
