using System.ComponentModel.DataAnnotations;

namespace Workshops.Presentation.Areas.Admin.ViewModels;

public class MechanicViewModel : AdminPageViewModelBase
{
    public Guid Id { get; set; }

    [Required]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = default!;

    [Required]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = default!;

    [Display(Name = "Phone")]
    public string? Phone { get; set; }

    [EmailAddress]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [Display(Name = "Specialization")]
    public string? Specialization { get; set; }

    public string FullName => $"{FirstName} {LastName}";
}

public class MechanicListViewModel : AdminPageViewModelBase
{
    public List<MechanicViewModel> Mechanics { get; set; } = new();
}
