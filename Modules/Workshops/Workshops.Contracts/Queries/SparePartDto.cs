namespace Workshops.Contracts.Queries;

public record SparePartDto(
    Guid Id,
    string Name,
    string? PartNumber,
    string? Country,
    decimal UnitPrice,
    int StockQuantity
);
