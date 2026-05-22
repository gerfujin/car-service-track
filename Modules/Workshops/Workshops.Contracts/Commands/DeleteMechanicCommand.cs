using MediatR;

namespace Workshops.Contracts.Commands;

public record DeleteMechanicCommand(Guid Id) : IRequest<bool>;
