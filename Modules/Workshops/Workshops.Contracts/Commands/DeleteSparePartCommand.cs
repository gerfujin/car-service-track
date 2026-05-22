using MediatR;

namespace Workshops.Contracts.Commands;

public record DeleteSparePartCommand(Guid Id) : IRequest<bool>;
