using Base.Contracts;
using Base.Domain;

namespace App.BLL.DTO;

public class BllListItem : IBaseEntity
{
    public Guid Id { get; set; }
    public string ItemDescription { get; set; } = default!;
    public LangStr Summary { get; set; } = default!;
    public bool IsDone { get; set; }
    public Guid AppUserId { get; set; }
    public string? AppUserEmail { get; set; }
}
