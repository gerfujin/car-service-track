namespace Orders.Domain.Enums;

public enum ServiceOrderStatus
{
    Pending = 0,
    Accepted = 1,
    InProgress = 2,
    WaitingForParts = 3,
    Completed = 4,
    Cancelled = 5
}
