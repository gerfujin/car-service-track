using MediatR;
using Users.Application.Services;
using Users.Contracts.Queries;

namespace Users.Application.Queries;

internal sealed class GetVehicleByIdForOwnerQueryHandler : IRequestHandler<GetVehicleByIdForOwnerQuery, VehicleDto?>
{
    private readonly IVehicleService _vehicleService;

    public GetVehicleByIdForOwnerQueryHandler(IVehicleService vehicleService)
    {
        _vehicleService = vehicleService;
    }

    public async Task<VehicleDto?> Handle(GetVehicleByIdForOwnerQuery request, CancellationToken cancellationToken)
    {
        var vehicle = await _vehicleService.FindByOwnerAsync(request.VehicleId, request.OwnerId);
        if (vehicle == null)
            return null;

        return new VehicleDto(
            vehicle.Id,
            vehicle.Make,
            vehicle.Model,
            vehicle.Year,
            vehicle.LicensePlate,
            vehicle.Vin,
            vehicle.Mileage,
            vehicle.Color,
            vehicle.OwnerId
        );
    }
}
