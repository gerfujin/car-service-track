namespace Orders.Contracts.Queries;

public record ServiceOrderPartDto(
    Guid Id,
    Guid ServiceOrderId,
    Guid SparePartId,
    string? SparePartName,
    int Quantity,
    decimal UnitPrice
);
