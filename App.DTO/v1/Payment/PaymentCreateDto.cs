using System.ComponentModel.DataAnnotations;

namespace App.DTO.v1.Payment;

public class PaymentCreateDto
{
    public Guid ServiceOrderId { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }
}
