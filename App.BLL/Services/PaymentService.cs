using App.BLL.DTO;
using App.BLL.Mappers;
using App.DAL.Contracts;
using App.Domain;
using App.Domain.Enums;

namespace App.BLL.Services;

public class PaymentService : IPaymentService
{
    private readonly IAppUnitOfWork _uow;

    public PaymentService(IAppUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IEnumerable<BllPayment>> AllAsync()
    {
        var entities = await _uow.Payments.AllAsync();
        return entities.Select(e => PaymentMapper.ToBll(e)!).ToList();
    }

    public async Task<IEnumerable<BllPayment>> AllWithDetailsAsync(Guid? serviceOrderId = null)
    {
        var entities = await _uow.Payments.AllWithDetailsAsync(serviceOrderId);
        return entities.Select(e => PaymentMapper.ToBll(e)!).ToList();
    }

    public async Task<IEnumerable<BllPayment>> AllByUserAsync(Guid appUserId, Guid? serviceOrderId = null)
    {
        var entities = await _uow.Payments.AllByUserAsync(appUserId, serviceOrderId);
        return entities.Select(e => PaymentMapper.ToBll(e)!).ToList();
    }

    public async Task<BllPayment?> FindAsync(Guid id)
    {
        return PaymentMapper.ToBll(await _uow.Payments.FindAsync(id));
    }

    public async Task<BllPayment?> FindWithDetailsAsync(Guid id)
    {
        return PaymentMapper.ToBll(await _uow.Payments.FindWithDetailsAsync(id));
    }

    public async Task<BllPayment?> FindWithDetailsForUserAsync(Guid id, Guid appUserId)
    {
        return PaymentMapper.ToBll(await _uow.Payments.FindWithDetailsForUserAsync(id, appUserId));
    }

    public async Task<BllPayment?> FindByServiceOrderAsync(Guid serviceOrderId)
    {
        return PaymentMapper.ToBll(await _uow.Payments.FindByServiceOrderAsync(serviceOrderId));
    }

    public async Task<BllPayment?> FindByServiceOrderForUserAsync(Guid serviceOrderId, Guid appUserId)
    {
        return PaymentMapper.ToBll(await _uow.Payments.FindByServiceOrderForUserAsync(serviceOrderId, appUserId));
    }

    // Staging only — no SaveChanges here.
    public BllPayment Add(BllPayment entity)
    {
        var added = _uow.Payments.Add(PaymentMapper.ToDomain(entity)!);
        return PaymentMapper.ToBll(added)!;
    }

    // Sync IBaseService.Update delegates to UpdateAsync (blocking) — one code path.
    public BllPayment Update(BllPayment entity)
    {
        return UpdateAsync(entity).GetAwaiter().GetResult() ?? entity;
    }

    // Staging only — no SaveChanges here. Load-then-merge preserves CreatedAt.
    public async Task<BllPayment?> UpdateAsync(BllPayment entity)
    {
        var existing = await _uow.Payments.FindAsync(entity.Id);
        if (existing == null) return null;

        existing.Amount = entity.Amount;
        existing.Status = entity.Status;
        existing.PaidAt = entity.PaidAt;
        existing.PaymentMethod = entity.PaymentMethod;
        existing.Notes = entity.Notes;
        existing.UpdatedAt = DateTime.UtcNow;

        _uow.Payments.Update(existing);
        return PaymentMapper.ToBll(existing);
    }

    // Staging only — no SaveChanges here.
    public void Remove(BllPayment entity)
    {
        _uow.Payments.Remove(PaymentMapper.ToDomain(entity)!);
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _uow.Payments.ExistsAsync(id);
    }

    public async Task<(BllPayment? payment, string? error)> CreateForServiceOrderAsync(
        Guid serviceOrderId, decimal amount)
    {
        // Load the ServiceOrder with Vehicle + Workshop navigation for validation and response building.
        // Uses the existing ServiceOrders repo (same UoW) which already includes Vehicle and Workshop.
        var serviceOrder = await _uow.ServiceOrders.FindWithDetailsAsync(serviceOrderId);
        if (serviceOrder == null)
            return (null, "Service order not found.");

        if (await _uow.Payments.AnyByServiceOrderAsync(serviceOrderId))
            return (null, "A payment invoice already exists for this service order.");

        var now = DateTime.UtcNow;
        var domain = new Payment
        {
            Amount = amount,
            Status = PaymentStatus.Pending,
            ServiceOrderId = serviceOrderId,
            CreatedAt = now,
            UpdatedAt = now
        };
        var added = _uow.Payments.Add(domain);

        // Populate navigation projection fields from the already-loaded ServiceOrder.
        var bll = PaymentMapper.ToBll(added)!;
        bll.OwnerId = serviceOrder.Vehicle?.OwnerId;
        bll.VehicleInfo = serviceOrder.Vehicle != null
            ? $"{serviceOrder.Vehicle.Make} {serviceOrder.Vehicle.Model} ({serviceOrder.Vehicle.LicensePlate})"
            : null;
        bll.WorkshopName = serviceOrder.Workshop?.Name.ToString();

        return (bll, null);
    }

    public async Task MarkAsPaidAsync(Guid id)
    {
        var existing = await _uow.Payments.FindAsync(id);
        if (existing == null) return;

        existing.Status = PaymentStatus.Paid;
        existing.PaidAt = DateTime.UtcNow;
        existing.UpdatedAt = DateTime.UtcNow;
        _uow.Payments.Update(existing);
    }
}
