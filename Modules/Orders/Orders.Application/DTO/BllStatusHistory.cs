using Base.Contracts;
using Orders.Domain.Enums;

namespace Orders.Application.DTO;

public class BllStatusHistory : IBaseEntity
{
    public Guid Id { get; set; }
    public Guid ServiceOrderId { get; set; }
    public ServiceOrderStatus Status { get; set; }
    public string? Notes { get; set; }
    public DateTime ChangedAt { get; set; }
}
