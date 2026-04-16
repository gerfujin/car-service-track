using System.ComponentModel.DataAnnotations;

namespace WebApp.Areas.Admin.ViewModels;

public class WorkshopViewModel
{
    public Guid Id { get; set; }

    [Required, MaxLength(256)]
    public string Name { get; set; } = default!;

    [Required, MaxLength(512)]
    public string Address { get; set; } = default!;

    [MaxLength(32)]
    public string? Phone { get; set; }

    [MaxLength(256), EmailAddress]
    public string? Email { get; set; }
}

public class WorkshopListViewModel
{
    public IEnumerable<WorkshopViewModel> Workshops { get; set; } = new List<WorkshopViewModel>();
}
