using App.Domain.Enums;
using Base.Contracts;

namespace App.BLL.DTO;

public class BllStatusHistory : IBaseEntity
{
    public Guid Id { get; set; }
    public Guid ServiceOrderId { get; set; }
    public ServiceOrderStatus Status { get; set; }
    public string? Notes { get; set; }
    public DateTime ChangedAt { get; set; }
}
