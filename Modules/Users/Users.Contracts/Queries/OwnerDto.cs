namespace Users.Contracts.Queries;

public record OwnerDto(
    Guid Id,
    string FirstName,
    string LastName,
    string? Address,
    string? Phone,
    Guid AppUserId
);
