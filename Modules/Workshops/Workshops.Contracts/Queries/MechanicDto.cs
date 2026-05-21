namespace Workshops.Contracts.Queries;

public record MechanicDto(
    Guid Id,
    string FirstName,
    string LastName,
    string? Phone,
    string? Email,
    string? Specialization,
    Guid AppUserId
);
