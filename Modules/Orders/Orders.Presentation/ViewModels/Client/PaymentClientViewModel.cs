using Orders.Domain.Enums;

namespace WebApp.ViewModels.Client;

public class PaymentClientViewModel
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? PaymentMethod { get; set; }
    public string? Notes { get; set; }
    public Guid ServiceOrderId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PaymentClientListViewModel
{
    public List<PaymentClientViewModel> Payments { get; set; } = new();
}
