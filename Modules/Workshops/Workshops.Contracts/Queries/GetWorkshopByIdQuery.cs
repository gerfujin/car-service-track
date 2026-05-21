using MediatR;

namespace Workshops.Contracts.Queries;

public record GetWorkshopByIdQuery(Guid WorkshopId) : IRequest<WorkshopDto?>;
