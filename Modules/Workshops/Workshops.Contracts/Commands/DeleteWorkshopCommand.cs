using MediatR;

namespace Workshops.Contracts.Commands;

public record DeleteWorkshopCommand(Guid Id) : IRequest<bool>;
