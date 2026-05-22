using MediatR;
using Orders.Application.DTO;
using Orders.Application.Mappers;
using Orders.Contracts;
using Orders.Domain;
using Workshops.Contracts.Queries;

namespace Orders.Application.Services;

public class ServiceOrderPartService : IServiceOrderPartService
{
    private readonly IOrdersUnitOfWork _uow;
    private readonly ISender _sender;

    public ServiceOrderPartService(IOrdersUnitOfWork uow, ISender sender)
    {
        _uow = uow;
        _sender = sender;
    }

    public async Task<IEnumerable<BllServiceOrderPart>> AllAsync()
    {
        var entities = await _uow.ServiceOrderParts.AllAsync();
        return await EnrichPartsAsync(entities);
    }

    public async Task<IEnumerable<BllServiceOrderPart>> AllForApiAsync(Guid? serviceOrderId, Guid appUserId, bool isAdmin, bool isMechanic)
    {
        var entities = await _uow.ServiceOrderParts.AllWithDetailsAsync(serviceOrderId);
        var parts = await EnrichPartsAsync(entities);

        if (isAdmin || isMechanic)
        {
            return parts;
        }

        return parts.Where(p => p.OwnerId == appUserId).ToList();
    }

    public async Task<BllServiceOrderPart?> FindAsync(Guid id)
    {
        var part = ServiceOrderPartMapper.ToBll(await _uow.ServiceOrderParts.FindAsync(id));
        return part == null ? null : await EnrichPartAsync(part);
    }

    public async Task<BllServiceOrderPart?> FindForApiAsync(Guid id, Guid appUserId, bool isAdmin, bool isMechanic)
    {
        var part = ServiceOrderPartMapper.ToBll(await _uow.ServiceOrderParts.FindWithDetailsAsync(id));
        if (part == null) return null;

        part = await EnrichPartAsync(part);
        if (isAdmin || isMechanic)
        {
            return part;
        }

        return part.OwnerId == appUserId ? part : null;
    }

    // Staging only - no SaveChanges here.
    public BllServiceOrderPart Add(BllServiceOrderPart entity)
    {
        var added = _uow.ServiceOrderParts.Add(ServiceOrderPartMapper.ToDomain(entity)!);
        return ServiceOrderPartMapper.ToBll(added)!;
    }

    public async Task<BllServiceOrderPartMutationResult> CreateForApiAsync(BllServiceOrderPart entity)
    {
        var order = await _uow.ServiceOrders.FindAsync(entity.ServiceOrderId);
        if (order == null)
        {
            return new BllServiceOrderPartMutationResult { Error = "Service order not found." };
        }

        var sparePart = await _sender.Send(new GetSparePartByIdQuery(entity.SparePartId));
        if (sparePart == null)
        {
            return new BllServiceOrderPartMutationResult { Error = "Spare part not found." };
        }

        if (entity.Quantity <= 0)
        {
            return new BllServiceOrderPartMutationResult { Error = "Quantity must be greater than 0." };
        }

        var effectivePrice = entity.UnitPrice <= 0 ? sparePart.UnitPrice : entity.UnitPrice;
        var added = _uow.ServiceOrderParts.Add(new ServiceOrderPart
        {
            ServiceOrderId = entity.ServiceOrderId,
            SparePartId = entity.SparePartId,
            Quantity = entity.Quantity,
            UnitPrice = effectivePrice
        });

        var result = ServiceOrderPartMapper.ToBll(added)!;
        result.OwnerId = order.AppUserId;
        result.SparePartName = sparePart.Name;
        result.SparePartPartNumber = sparePart.PartNumber;
        result.LineTotal = result.Quantity * result.UnitPrice;

        return new BllServiceOrderPartMutationResult
        {
            Entity = result,
            RecalculateServiceOrderId = order.Id
        };
    }

    public BllServiceOrderPart Update(BllServiceOrderPart entity)
    {
        return UpdateAsync(entity).GetAwaiter().GetResult() ?? entity;
    }

    public async Task<BllServiceOrderPart?> UpdateAsync(BllServiceOrderPart entity)
    {
        var result = await UpdateForApiAsync(entity.Id, entity);
        return result.Entity;
    }

    public async Task<BllServiceOrderPartMutationResult> UpdateForApiAsync(Guid id, BllServiceOrderPart entity)
    {
        var existing = await _uow.ServiceOrderParts.FindAsync(id);
        if (existing == null)
        {
            return new BllServiceOrderPartMutationResult { NotFound = true };
        }

        if (entity.UnitPrice < 0)
        {
            return new BllServiceOrderPartMutationResult { Error = "Price must be greater than or equal to 0." };
        }

        var order = await _uow.ServiceOrders.FindAsync(entity.ServiceOrderId);
        if (order == null)
        {
            return new BllServiceOrderPartMutationResult { Error = "Service order not found." };
        }

        var sparePart = await _sender.Send(new GetSparePartByIdQuery(entity.SparePartId));
        if (sparePart == null)
        {
            return new BllServiceOrderPartMutationResult { Error = "Spare part not found." };
        }

        if (entity.Quantity <= 0)
        {
            return new BllServiceOrderPartMutationResult { Error = "Quantity must be greater than 0." };
        }

        existing.ServiceOrderId = entity.ServiceOrderId;
        existing.SparePartId = entity.SparePartId;
        existing.Quantity = entity.Quantity;
        existing.UnitPrice = entity.UnitPrice <= 0 ? sparePart.UnitPrice : entity.UnitPrice;

        var updated = _uow.ServiceOrderParts.Update(existing);
        var bll = ServiceOrderPartMapper.ToBll(updated)!;
        bll.OwnerId = order.AppUserId;
        bll.SparePartName = sparePart.Name;
        bll.SparePartPartNumber = sparePart.PartNumber;

        return new BllServiceOrderPartMutationResult
        {
            Entity = bll,
            RecalculateServiceOrderId = updated.ServiceOrderId
        };
    }

    // Staging only - no SaveChanges here.
    public void Remove(BllServiceOrderPart entity)
    {
        _uow.ServiceOrderParts.Remove(ServiceOrderPartMapper.ToDomain(entity)!);
    }

    public async Task<BllServiceOrderPartMutationResult> RemoveForApiAsync(Guid id)
    {
        var existing = await _uow.ServiceOrderParts.FindAsync(id);
        if (existing == null)
        {
            return new BllServiceOrderPartMutationResult { NotFound = true };
        }

        var orderId = existing.ServiceOrderId;
        _uow.ServiceOrderParts.Remove(existing);

        return new BllServiceOrderPartMutationResult
        {
            RecalculateServiceOrderId = orderId
        };
    }

    public async Task RecalculateOrderTotalAsync(Guid serviceOrderId)
    {
        await _uow.ServiceOrderParts.RecalculateOrderTotalAsync(serviceOrderId);
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _uow.ServiceOrderParts.ExistsAsync(id);
    }

    private async Task<List<BllServiceOrderPart>> EnrichPartsAsync(IEnumerable<ServiceOrderPart> entities)
    {
        var parts = entities.Select(e => ServiceOrderPartMapper.ToBll(e)!).ToList();
        foreach (var part in parts)
        {
            await EnrichPartAsync(part);
        }

        return parts;
    }

    private async Task<BllServiceOrderPart> EnrichPartAsync(BllServiceOrderPart part)
    {
        var sparePart = await _sender.Send(new GetSparePartByIdQuery(part.SparePartId));
        if (sparePart != null)
        {
            part.SparePartName = sparePart.Name;
            part.SparePartPartNumber = sparePart.PartNumber;
        }

        var order = await _uow.ServiceOrders.FindAsync(part.ServiceOrderId);
        if (order != null)
        {
            part.OwnerId = order.AppUserId;
        }

        part.LineTotal = part.Quantity * part.UnitPrice;
        return part;
    }
}
