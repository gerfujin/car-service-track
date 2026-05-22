using MediatR;

namespace Workshops.Contracts.Commands;

public record DeleteServiceCommand(Guid Id) : IRequest<bool>;
