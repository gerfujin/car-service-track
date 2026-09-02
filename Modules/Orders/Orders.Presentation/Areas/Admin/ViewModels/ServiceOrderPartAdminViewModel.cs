using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebApp.Areas.Admin.ViewModels;

public class ServiceOrderPartAdminViewModel : AdminPageViewModelBase
{
    public Guid Id { get; set; }

    [Required]
    public Guid ServiceOrderId { get; set; }

    [Required]
    public Guid SparePartId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    public string? SparePartName { get; set; }
    public IEnumerable<SelectListItem> ServiceOrderOptions { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> SparePartOptions { get; set; } = new List<SelectListItem>();
}

public class ServiceOrderPartAdminListViewModel : AdminPageViewModelBase
{
    public Guid? ServiceOrderId { get; set; }
    public List<ServiceOrderPartAdminViewModel> Parts { get; set; } = new();
}
