using System.ComponentModel.DataAnnotations;

namespace WebApp.Areas.Admin.ViewModels;

public class MechanicViewModel
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

public class MechanicListViewModel
{
    public List<MechanicViewModel> Mechanics { get; set; } = new();
}
