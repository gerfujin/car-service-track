namespace Users.Contracts.Queries;

public record UserDto(Guid Id, string Email, string? FirstName, string? LastName);
