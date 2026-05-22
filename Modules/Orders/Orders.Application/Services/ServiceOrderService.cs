using MediatR;
using Orders.Application.DTO;
using Orders.Application.Mappers;
using Orders.Contracts;
using Orders.Domain;
using Orders.Domain.Enums;
using Users.Contracts.Queries;
using Workshops.Contracts.Queries;

namespace Orders.Application.Services;

public class ServiceOrderService : IServiceOrderService
{
    private readonly IOrdersUnitOfWork _uow;
    private readonly ISender _sender;

    public ServiceOrderService(IOrdersUnitOfWork uow, ISender sender)
    {
        _uow = uow;
        _sender = sender;
    }

    public async Task<IEnumerable<BllServiceOrder>> AllAsync()
    {
        var entities = await _uow.ServiceOrders.AllAsync();
        return await EnrichOrdersAsync(entities);
    }

    public async Task<IEnumerable<BllServiceOrder>> AllByUserAsync(Guid appUserId)
    {
        var entities = await _uow.ServiceOrders.AllByUserAsync(appUserId);
        return await EnrichOrdersAsync(entities);
    }

    public async Task<IEnumerable<BllServiceOrder>> AllWithDetailsAsync()
    {
        var entities = await _uow.ServiceOrders.AllWithDetailsAsync();
        return await EnrichOrdersAsync(entities);
    }

    public async Task<BllServiceOrder?> FindAsync(Guid id)
    {
        var order = ServiceOrderMapper.ToBll(await _uow.ServiceOrders.FindAsync(id));
        return order == null ? null : await EnrichOrderAsync(order);
    }

    public async Task<BllServiceOrder?> FindWithDetailsAsync(Guid id)
    {
        var order = ServiceOrderMapper.ToBll(await _uow.ServiceOrders.FindWithDetailsAsync(id));
        return order == null ? null : await EnrichOrderAsync(order);
    }

    // Staging only - no SaveChanges here.
    public BllServiceOrder Add(BllServiceOrder entity)
    {
        var added = _uow.ServiceOrders.Add(ServiceOrderMapper.ToDomain(entity)!);
        return ServiceOrderMapper.ToBll(added)!;
    }

    // Staging only - no SaveChanges here. Synchronous IBaseService.Update delegates to UpdateAsync.
    public BllServiceOrder Update(BllServiceOrder entity)
    {
        return UpdateAsync(entity).GetAwaiter().GetResult() ?? entity;
    }

    // Staging only - no SaveChanges here.
    // Load-then-merge preserves CreatedAt and child graph ownership.
    public async Task<BllServiceOrder?> UpdateAsync(BllServiceOrder entity)
    {
        var existing = await _uow.ServiceOrders.FindAsync(entity.Id);
        if (existing == null) return null;

        existing.VehicleId = entity.VehicleId;
        existing.WorkshopId = entity.WorkshopId;
        existing.MechanicId = entity.MechanicId;
        existing.Description = entity.Description;
        existing.OrderDate = entity.OrderDate;
        existing.Status = entity.Status;
        existing.UpdatedAt = DateTime.UtcNow;
        _uow.ServiceOrders.Update(existing);

        await _uow.ServiceOrders.UpdateServiceItemsAsync(entity.Id, entity.ServiceIds);

        return await EnrichOrderAsync(ServiceOrderMapper.ToBll(existing)!);
    }

    // Staging only - no SaveChanges here.
    public void Remove(BllServiceOrder entity)
    {
        _uow.ServiceOrders.Remove(ServiceOrderMapper.ToDomain(entity)!);
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _uow.ServiceOrders.ExistsAsync(id);
    }

    public async Task<bool> IsOwnedByUserAsync(Guid id, Guid appUserId)
    {
        return await _uow.ServiceOrders.IsOwnedByUserAsync(id, appUserId);
    }

    public async Task<BllServiceOrder> AddWithItemsAsync(BllServiceOrder entity)
    {
        var domain = ServiceOrderMapper.ToDomain(entity)!;
        domain.ServiceOrderItems = new List<ServiceOrderItem>();

        var added = _uow.ServiceOrders.Add(domain);

        if (entity.ServiceIds.Any())
        {
            await _uow.ServiceOrders.AddServiceItemsAsync(added.Id, entity.ServiceIds);
        }

        return await EnrichOrderAsync(ServiceOrderMapper.ToBll(added)!);
    }

    public async Task<bool> SetStatusAsync(Guid orderId, ServiceOrderStatus status, string? notes)
    {
        var existing = await _uow.ServiceOrders.FindAsync(orderId);
        if (existing == null) return false;

        var previousStatus = existing.Status;
        existing.Status = status;
        existing.UpdatedAt = DateTime.UtcNow;

        if (status == ServiceOrderStatus.Completed)
        {
            existing.CompletedDate = DateTime.UtcNow;
        }

        _uow.ServiceOrders.Update(existing);

        _uow.StatusHistories.Add(new ServiceOrderStatusHistory
        {
            ServiceOrderId = orderId,
            Status = status,
            Notes = notes ?? $"Status changed from {previousStatus} to {status}",
            ChangedAt = DateTime.UtcNow
        });

        return true;
    }

    public async Task<List<BllSelectListItem>> GetSelectListForAdminAsync()
    {
        var orders = await AllAsync();
        return orders
            .OrderByDescending(o => o.OrderDate)
            .Select(o => new BllSelectListItem
            {
                Value = o.Id.ToString(),
                Text = $"{o.OrderDate:yyyy-MM-dd} | {o.Description ?? "Order"}"
            })
            .ToList();
    }

    public async Task<bool> UpdateStatusForAdminAsync(Guid orderId, ServiceOrderStatus status, Guid? mechanicId, decimal? finalPrice, string? notes)
    {
        var existing = await _uow.ServiceOrders.FindAsync(orderId);
        if (existing == null) return false;

        var previousStatus = existing.Status;
        existing.Status = status;
        existing.MechanicId = mechanicId;
        existing.FinalPrice = finalPrice;
        existing.UpdatedAt = DateTime.UtcNow;

        if (status == ServiceOrderStatus.Completed)
        {
            existing.CompletedDate = DateTime.UtcNow;
        }

        _uow.ServiceOrders.Update(existing);

        _uow.StatusHistories.Add(new ServiceOrderStatusHistory
        {
            ServiceOrderId = orderId,
            Status = status,
            Notes = notes ?? $"Status changed from {previousStatus} to {status}",
            ChangedAt = DateTime.UtcNow
        });

        return true;
    }

    public async Task<BllDashboardStats> GetAdminDashboardStatsAsync()
    {
        var orders = (await _uow.ServiceOrders.AllAsync()).ToList();

        var stats = new BllDashboardStats
        {
            TotalVehicles = orders.Select(o => o.VehicleId).Distinct().Count(),
            TotalServiceOrders = orders.Count,
            TotalWorkshops = orders.Select(o => o.WorkshopId).Distinct().Count(),
            TotalMechanics = orders.Where(o => o.MechanicId.HasValue).Select(o => o.MechanicId!.Value).Distinct().Count(),
            TotalClients = orders.Select(o => o.AppUserId).Distinct().Count(),
            PendingOrders = await _uow.ServiceOrders.CountByStatusAsync(ServiceOrderStatus.Pending),
            InProgressOrders = await _uow.ServiceOrders.CountByStatusAsync(ServiceOrderStatus.InProgress),
            CompletedOrders = await _uow.ServiceOrders.CountByStatusAsync(ServiceOrderStatus.Completed),
            TotalRevenue = await _uow.Payments.SumPaidAmountAsync(),
            TotalPayments = await _uow.Payments.CountAsync()
        };

        return stats;
    }

    public async Task<BllDashboardStats> GetMechanicDashboardStatsAsync(Guid appUserId)
    {
        var orders = (await _uow.ServiceOrders.AllAsync()).ToList();
        var mechanicIds = await ResolveMechanicIdsByUserAsync(orders, appUserId);
        var mechanicOrders = orders.Where(o => o.MechanicId.HasValue && mechanicIds.Contains(o.MechanicId.Value)).ToList();

        var orderIds = mechanicOrders.Select(o => o.Id).ToHashSet();
        var payments = await _uow.Payments.AllAsync();

        return new BllDashboardStats
        {
            TotalVehicles = mechanicOrders.Select(o => o.VehicleId).Distinct().Count(),
            TotalServiceOrders = mechanicOrders.Count,
            PendingOrders = mechanicOrders.Count(o => o.Status == ServiceOrderStatus.Pending),
            InProgressOrders = mechanicOrders.Count(o => o.Status == ServiceOrderStatus.InProgress),
            CompletedOrders = mechanicOrders.Count(o => o.Status == ServiceOrderStatus.Completed),
            TotalPayments = payments.Count(p => orderIds.Contains(p.ServiceOrderId))
        };
    }

    public async Task<IEnumerable<BllServiceOrder>> AllByMechanicAsync(Guid appUserId)
    {
        var entities = (await _uow.ServiceOrders.AllWithDetailsAsync()).ToList();
        var mechanicIds = await ResolveMechanicIdsByUserAsync(entities, appUserId);
        var filtered = entities
            .Where(so => so.MechanicId.HasValue && mechanicIds.Contains(so.MechanicId.Value))
            .ToList();

        return await EnrichOrdersAsync(filtered);
    }

    public async Task<BllServiceOrder?> FindByMechanicAsync(Guid orderId, Guid appUserId)
    {
        var entity = await _uow.ServiceOrders.FindWithDetailsAsync(orderId);
        if (entity == null || !entity.MechanicId.HasValue) return null;

        var mechanic = await _sender.Send(new GetMechanicByIdQuery(entity.MechanicId.Value));
        if (mechanic == null || mechanic.AppUserId != appUserId) return null;

        return await EnrichOrderAsync(ServiceOrderMapper.ToBll(entity)!);
    }

    public async Task<bool> UpdateStatusByMechanicAsync(Guid orderId, Guid appUserId, ServiceOrderStatus newStatus, string? notes)
    {
        var order = await FindByMechanicAsync(orderId, appUserId);
        if (order == null) return false;

        return await SetStatusAsync(orderId, newStatus, notes);
    }

    public async Task<bool> HasOrdersForVehicleAsync(Guid vehicleId)
    {
        return await _uow.ServiceOrders.AnyByVehicleAsync(vehicleId);
    }

    private async Task<List<BllServiceOrder>> EnrichOrdersAsync(IEnumerable<ServiceOrder> entities)
    {
        var orders = entities.Select(e => ServiceOrderMapper.ToBll(e)!).ToList();
        foreach (var order in orders)
        {
            await EnrichOrderAsync(order);
        }

        return orders;
    }

    private async Task<BllServiceOrder> EnrichOrderAsync(BllServiceOrder order)
    {
        order.OwnerAppUserId = order.AppUserId;

        var vehicle = await _sender.Send(new GetVehicleByIdQuery(order.VehicleId));
        if (vehicle != null)
        {
            order.VehicleDisplay = $"{vehicle.Make} {vehicle.Model} ({vehicle.LicensePlate})";
        }

        var workshop = await _sender.Send(new GetWorkshopByIdQuery(order.WorkshopId));
        if (workshop != null)
        {
            order.WorkshopName = workshop.Name;
        }

        if (order.MechanicId.HasValue)
        {
            var mechanic = await _sender.Send(new GetMechanicByIdQuery(order.MechanicId.Value));
            if (mechanic != null)
            {
                order.MechanicName = $"{mechanic.FirstName} {mechanic.LastName}";
            }
        }

        var owner = await _sender.Send(new GetOwnerByAppUserIdQuery(order.AppUserId));
        if (owner != null)
        {
            order.OwnerName = $"{owner.FirstName} {owner.LastName}";
        }

        var detailedOrder = await _uow.ServiceOrders.FindWithDetailsAsync(order.Id);
        if (detailedOrder?.ServiceOrderItems != null)
        {
            order.ServiceIds = detailedOrder.ServiceOrderItems.Select(i => i.ServiceId).ToList();
            var services = new List<BllServiceLine>();

            foreach (var item in detailedOrder.ServiceOrderItems)
            {
                var service = await _sender.Send(new GetServiceByIdQuery(item.ServiceId));
                if (service == null) continue;

                services.Add(new BllServiceLine
                {
                    Id = service.Id,
                    Name = service.Name,
                    Description = service.Description,
                    BasePrice = service.BasePrice,
                    EstimatedTimeMinutes = service.EstimatedTimeMinutes
                });
            }

            order.Services = services;
            order.TotalAmount =
                (detailedOrder.ServiceOrderItems.Sum(i => i.Quantity * i.UnitPrice)) +
                (detailedOrder.ServiceOrderParts?.Sum(p => p.Quantity * p.UnitPrice) ?? 0m);

            order.HasPayment = detailedOrder.Payment != null;
        }
        else
        {
            var payment = await _uow.Payments.FindByServiceOrderAsync(order.Id);
            order.HasPayment = payment != null;
        }

        return order;
    }

    private async Task<HashSet<Guid>> ResolveMechanicIdsByUserAsync(IEnumerable<ServiceOrder> orders, Guid appUserId)
    {
        var mechanicIds = orders
            .Where(o => o.MechanicId.HasValue)
            .Select(o => o.MechanicId!.Value)
            .Distinct()
            .ToList();

        var result = new HashSet<Guid>();
        foreach (var mechanicId in mechanicIds)
        {
            var mechanic = await _sender.Send(new GetMechanicByIdQuery(mechanicId));
            if (mechanic?.AppUserId == appUserId)
            {
                result.Add(mechanicId);
            }
        }

        return result;
    }
}
