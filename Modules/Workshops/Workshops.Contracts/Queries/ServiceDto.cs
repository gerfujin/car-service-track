namespace Workshops.Contracts.Queries;

public record ServiceDto(
    Guid Id,
    string Name,
    string? Description,
    decimal BasePrice,
    int EstimatedTimeMinutes
);
