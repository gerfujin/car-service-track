using MediatR;

namespace Orders.Contracts.Commands;

public record DeleteServiceOrderPartCommand(Guid Id) : IRequest<bool>;
