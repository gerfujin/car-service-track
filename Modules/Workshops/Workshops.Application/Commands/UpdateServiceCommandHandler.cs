using MediatR;
using Workshops.Application.DTO;
using Workshops.Application.Services;
using Workshops.Contracts;
using Workshops.Contracts.Commands;
using Workshops.Contracts.Queries;

namespace Workshops.Application.Commands;

internal sealed class UpdateServiceCommandHandler : IRequestHandler<UpdateServiceCommand, ServiceDto?>
{
    private readonly IServiceService _serviceService;
    private readonly IWorkshopsUnitOfWork _uow;

    public UpdateServiceCommandHandler(IServiceService serviceService, IWorkshopsUnitOfWork uow)
    {
        _serviceService = serviceService;
        _uow = uow;
    }

    public async Task<ServiceDto?> Handle(UpdateServiceCommand request, CancellationToken cancellationToken)
    {
        var bll = new BllService
        {
            Id = request.Id,
            Name = request.Name,
            Description = request.Description,
            BasePrice = request.BasePrice,
            EstimatedTimeMinutes = request.EstimatedTimeMinutes
        };

        var updated = await _serviceService.UpdateAsync(bll);
        if (updated == null) return null;

        await _uow.SaveChangesAsync();

        return new ServiceDto(updated.Id, updated.Name, updated.Description, updated.BasePrice, updated.EstimatedTimeMinutes);
    }
}
