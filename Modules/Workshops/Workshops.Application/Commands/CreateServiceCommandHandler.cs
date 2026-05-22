using MediatR;
using Workshops.Application.DTO;
using Workshops.Application.Services;
using Workshops.Contracts;
using Workshops.Contracts.Commands;
using Workshops.Contracts.Queries;

namespace Workshops.Application.Commands;

internal sealed class CreateServiceCommandHandler : IRequestHandler<CreateServiceCommand, ServiceDto>
{
    private readonly IServiceService _serviceService;
    private readonly IWorkshopsUnitOfWork _uow;

    public CreateServiceCommandHandler(IServiceService serviceService, IWorkshopsUnitOfWork uow)
    {
        _serviceService = serviceService;
        _uow = uow;
    }

    public async Task<ServiceDto> Handle(CreateServiceCommand request, CancellationToken cancellationToken)
    {
        var bll = new BllService
        {
            Name = request.Name,
            Description = request.Description,
            BasePrice = request.BasePrice,
            EstimatedTimeMinutes = request.EstimatedTimeMinutes
        };

        var added = _serviceService.Add(bll);
        await _uow.SaveChangesAsync();

        return new ServiceDto(added.Id, added.Name, added.Description, added.BasePrice, added.EstimatedTimeMinutes);
    }
}
