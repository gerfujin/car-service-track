using MediatR;

namespace Workshops.Contracts.Queries;

public record GetAllWorkshopsQuery : IRequest<IEnumerable<WorkshopDto>>;
