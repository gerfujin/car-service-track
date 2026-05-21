namespace Workshops.Contracts.Queries;

public record WorkshopDto(
    Guid Id,
    string Name,
    string Address,
    string? Phone,
    string? Email
);
