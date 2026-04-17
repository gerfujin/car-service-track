using System.ComponentModel.DataAnnotations;
using App.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebApp.Areas.Admin.ViewModels;

public class ServiceOrderAdminViewModel
{
    public Guid Id { get; set; }
    public string? Description { get; set; }
    public ServiceOrderStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public DateTime OrderDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string VehicleDisplay { get; set; } = default!;
    public string OwnerName { get; set; } = default!;
    public string WorkshopName { get; set; } = default!;
    public string? MechanicName { get; set; }
    public decimal TotalAmount { get; set; }
    /// <summary>Admin-set final price. When set, overrides the calculated TotalAmount.</summary>
    public decimal? FinalPrice { get; set; }
    public bool HasPayment { get; set; }
}

public class ServiceOrderAdminListViewModel
{
    public IEnumerable<ServiceOrderAdminViewModel> Orders { get; set; } = new List<ServiceOrderAdminViewModel>();
}

public class ServiceOrderStatusUpdateViewModel
{
    public Guid Id { get; set; }
    public ServiceOrderStatus CurrentStatus { get; set; }
    public ServiceOrderStatus NewStatus { get; set; }
    public string? Notes { get; set; }
    public Guid? MechanicId { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Final price must be 0 or greater.")]
    public decimal? FinalPrice { get; set; }

    public IEnumerable<SelectListItem> StatusOptions { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> MechanicOptions { get; set; } = new List<SelectListItem>();
}
