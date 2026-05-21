using MediatR;

namespace Users.Contracts.Queries;

public record GetVehicleByIdQuery(Guid VehicleId) : IRequest<VehicleDto?>;
