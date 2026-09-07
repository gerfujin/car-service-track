using System.ComponentModel.DataAnnotations;

namespace Workshops.Presentation.Areas.Admin.ViewModels;

public class SparePartAdminViewModel : AdminPageViewModelBase
{
    public Guid Id { get; set; }

    [Required]
    [Display(Name = "Part Name")]
    public string Name { get; set; } = default!;

    [Display(Name = "Part Number")]
    public string? PartNumber { get; set; }

    [Required]
    [Range(0, 99999.99)]
    [Display(Name = "Unit Price (€)")]
    public decimal UnitPrice { get; set; }

    [Required]
    [Range(0, int.MaxValue)]
    [Display(Name = "Stock Quantity")]
    public int StockQuantity { get; set; }
}

public class SparePartAdminListViewModel : AdminPageViewModelBase
{
    public List<SparePartAdminViewModel> SpareParts { get; set; } = new();
}
