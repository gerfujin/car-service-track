using Orders.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Orders.Contracts.Commands;
using Orders.Contracts.Queries;
using Orders.Presentation.Areas.Admin.ViewModels;
using Workshops.Contracts.Queries;

namespace Orders.Presentation.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "admin,mechanic")]
public class ServiceOrdersController : Controller
{
    private readonly IMediator _mediator;

    public ServiceOrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IActionResult> Index()
    {
        var orders = await _mediator.Send(new GetAllServiceOrdersQuery());
        var vm = new ServiceOrderAdminListViewModel
        {
            PageTitle = "Service Orders",
            Orders = orders.Select(so => new ServiceOrderAdminViewModel
            {
                Id = so.Id,
                Description = so.Description,
                Status = so.Status,
                OrderDate = so.OrderDate,
                CompletedDate = so.CompletedDate,
                VehicleDisplay = so.VehicleDisplay ?? "N/A",
                OwnerName = so.OwnerName ?? "N/A",
                WorkshopName = so.WorkshopName ?? "N/A",
                MechanicName = so.MechanicName,
                TotalAmount = so.TotalAmount,
                FinalPrice = so.FinalPrice,
                HasPayment = so.HasPayment
            }).ToList()
        };

        return View(vm);
    }

    public async Task<IActionResult> UpdateStatus(Guid id)
    {
        var order = await _mediator.Send(new GetServiceOrderByIdQuery(id));
        if (order == null) return NotFound();

        var vm = new ServiceOrderStatusUpdateViewModel
        {
            PageTitle = "Update Order Status",
            Id = order.Id,
            CurrentStatus = order.Status,
            NewStatus = order.Status,
            MechanicId = order.MechanicId,
            FinalPrice = order.FinalPrice,
            StatusOptions = Enum.GetValues<ServiceOrderStatus>()
                .Select(s => new SelectListItem { Value = ((int)s).ToString(), Text = s.ToString() })
                .ToList(),
            MechanicOptions = await BuildMechanicSelectListAsync()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(Guid id, ServiceOrderStatusUpdateViewModel vm)
    {
        if (id != vm.Id) return BadRequest();

        if (!ModelState.IsValid)
        {
            vm.PageTitle = "Update Order Status";
            vm.StatusOptions = Enum.GetValues<ServiceOrderStatus>()
                .Select(s => new SelectListItem { Value = ((int)s).ToString(), Text = s.ToString() })
                .ToList();
            vm.MechanicOptions = await BuildMechanicSelectListAsync();
            return View(vm);
        }

        var moduleStatus = (Orders.Domain.Enums.ServiceOrderStatus)(int)vm.NewStatus;
        var result = await _mediator.Send(new UpdateServiceOrderStatusAdminCommand(
            id, moduleStatus, vm.MechanicId, vm.FinalPrice, vm.Notes));

        if (!result) return NotFound();

        return RedirectToAction(nameof(Index));
    }

    private async Task<List<SelectListItem>> BuildMechanicSelectListAsync()
    {
        var mechanics = await _mediator.Send(new GetAllMechanicsQuery());
        return mechanics
            .Select(m => new SelectListItem
            {
                Value = m.Id.ToString(),
                Text = $"{m.FirstName} {m.LastName}"
            })
            .ToList();
    }
}
