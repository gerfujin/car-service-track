using MediatR;
using Users.Application.Services;
using Users.Contracts.Queries;

namespace Users.Application.Queries;

internal sealed class GetVehicleByIdQueryHandler : IRequestHandler<GetVehicleByIdQuery, VehicleDto?>
{
    private readonly IVehicleService _vehicleService;

    public GetVehicleByIdQueryHandler(IVehicleService vehicleService)
    {
        _vehicleService = vehicleService;
    }

    public async Task<VehicleDto?> Handle(GetVehicleByIdQuery request, CancellationToken cancellationToken)
    {
        var vehicle = await _vehicleService.FindAsync(request.VehicleId);
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
