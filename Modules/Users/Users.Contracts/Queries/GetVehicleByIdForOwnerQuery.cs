using MediatR;

namespace Users.Contracts.Queries;

public record GetVehicleByIdForOwnerQuery(Guid VehicleId, Guid OwnerId) : IRequest<VehicleDto?>;
