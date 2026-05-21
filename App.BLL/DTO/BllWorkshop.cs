using Base.Contracts;

namespace App.BLL.DTO;

public class BllWorkshop : IBaseEntity
{
    public Guid Id { get; set; }

    // Multilingual Name/Address flattened to current-culture strings (LangStr.ToString());
    // ToDomain re-wraps them in LangStr.
    public string Name { get; set; } = default!;
    public string Address { get; set; } = default!;
    public string? Phone { get; set; }
    public string? Email { get; set; }
}
