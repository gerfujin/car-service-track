using Orders.Domain.Enums;

namespace App.DTO.v1.ServiceOrder;

public class UpdateOrderStatusDto
{
    public ServiceOrderStatus Status { get; set; }
    public string? Notes { get; set; }
}
