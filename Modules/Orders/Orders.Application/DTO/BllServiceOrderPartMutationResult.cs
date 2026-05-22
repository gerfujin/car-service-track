namespace Orders.Application.DTO;

public class BllServiceOrderPartMutationResult
{
    public BllServiceOrderPart? Entity { get; set; }
    public bool NotFound { get; set; }
    public string? Error { get; set; }
    public Guid? RecalculateServiceOrderId { get; set; }
}
