using App.BLL.DTO;
using App.BLL.Mappers;
using App.DAL.Contracts;

namespace App.BLL.Services;

public class ServiceOrderPartService : IServiceOrderPartService
{
    private readonly IAppUnitOfWork _uow;

    public ServiceOrderPartService(IAppUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IEnumerable<BllServiceOrderPart>> AllAsync()
    {
        var entities = await _uow.ServiceOrderParts.AllAsync();
        return entities.Select(e => ServiceOrderPartMapper.ToBll(e)!).ToList();
    }

    public async Task<IEnumerable<BllServiceOrderPart>> AllForApiAsync(Guid? serviceOrderId, Guid appUserId, bool isAdmin, bool isMechanic)
    {
        var entities = await _uow.ServiceOrderParts.AllWithDetailsAsync(serviceOrderId);
        var parts = entities.Select(e => ServiceOrderPartMapper.ToBll(e)!).ToList();

        if (isAdmin || isMechanic)
        {
            return parts;
        }

        var owner = await _uow.Owners.FindByUserAsync(appUserId);
        if (owner == null)
        {
            return new List<BllServiceOrderPart>();
        }

        return parts.Where(p => p.OwnerId == owner.Id).ToList();
    }

    public async Task<BllServiceOrderPart?> FindAsync(Guid id)
    {
        return ServiceOrderPartMapper.ToBll(await _uow.ServiceOrderParts.FindAsync(id));
    }

    public async Task<BllServiceOrderPart?> FindForApiAsync(Guid id, Guid appUserId, bool isAdmin, bool isMechanic)
    {
        var part = ServiceOrderPartMapper.ToBll(await _uow.ServiceOrderParts.FindWithDetailsAsync(id));
        if (part == null) return null;

        if (isAdmin || isMechanic)
        {
            return part;
        }

        var owner = await _uow.Owners.FindByUserAsync(appUserId);
        if (owner == null || part.OwnerId != owner.Id)
        {
            return null;
        }

        return part;
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

        var sparePart = await _uow.SpareParts.FindAsync(entity.SparePartId);
        if (sparePart == null)
        {
            return new BllServiceOrderPartMutationResult { Error = "Spare part not found." };
        }

        if (entity.Quantity <= 0)
        {
            return new BllServiceOrderPartMutationResult { Error = "Quantity must be greater than 0." };
        }

        var effectivePrice = entity.UnitPrice <= 0 ? sparePart.UnitPrice : entity.UnitPrice;
        var added = _uow.ServiceOrderParts.Add(new App.Domain.ServiceOrderPart
        {
            ServiceOrderId = entity.ServiceOrderId,
            SparePartId = entity.SparePartId,
            Quantity = entity.Quantity,
            UnitPrice = effectivePrice,
        });

        var result = ServiceOrderPartMapper.ToBll(added)!;
        result.SparePartName = sparePart.Name.Translate() ?? sparePart.Name.ToString();
        result.SparePartPartNumber = sparePart.PartNumber;
        result.LineTotal = result.Quantity * result.UnitPrice;

        return new BllServiceOrderPartMutationResult
        {
            Entity = result,
            RecalculateServiceOrderId = order.Id,
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

        var sparePart = await _uow.SpareParts.FindAsync(entity.SparePartId);
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
        return new BllServiceOrderPartMutationResult
        {
            Entity = ServiceOrderPartMapper.ToBll(updated),
            RecalculateServiceOrderId = updated.ServiceOrderId,
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
            RecalculateServiceOrderId = orderId,
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
}
