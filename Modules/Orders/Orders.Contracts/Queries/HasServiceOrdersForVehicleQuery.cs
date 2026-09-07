using MediatR;

namespace Orders.Contracts.Queries;

public record HasServiceOrdersForVehicleQuery(Guid VehicleId) : IRequest<bool>;
