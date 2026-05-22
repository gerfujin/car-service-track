using MediatR;
using Orders.Application.DTO;
using Orders.Application.Mappers;
using Orders.Contracts;
using Orders.Domain;
using Orders.Domain.Enums;
using Users.Contracts.Queries;
using Workshops.Contracts.Queries;

namespace Orders.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IOrdersUnitOfWork _uow;
    private readonly ISender _sender;

    public PaymentService(IOrdersUnitOfWork uow, ISender sender)
    {
        _uow = uow;
        _sender = sender;
    }

    public async Task<IEnumerable<BllPayment>> AllAsync()
    {
        var entities = await _uow.Payments.AllAsync();
        return await EnrichPaymentsAsync(entities);
    }

    public async Task<IEnumerable<BllPayment>> AllWithDetailsAsync(Guid? serviceOrderId = null)
    {
        var entities = await _uow.Payments.AllWithDetailsAsync(serviceOrderId);
        return await EnrichPaymentsAsync(entities);
    }

    public async Task<IEnumerable<BllPayment>> AllByUserAsync(Guid appUserId, Guid? serviceOrderId = null)
    {
        var entities = await _uow.Payments.AllByUserAsync(appUserId, serviceOrderId);
        return await EnrichPaymentsAsync(entities);
    }

    public async Task<BllPayment?> FindAsync(Guid id)
    {
        var payment = PaymentMapper.ToBll(await _uow.Payments.FindAsync(id));
        return payment == null ? null : await EnrichPaymentAsync(payment);
    }

    public async Task<BllPayment?> FindWithDetailsAsync(Guid id)
    {
        var payment = PaymentMapper.ToBll(await _uow.Payments.FindWithDetailsAsync(id));
        return payment == null ? null : await EnrichPaymentAsync(payment);
    }

    public async Task<BllPayment?> FindWithDetailsForUserAsync(Guid id, Guid appUserId)
    {
        var payment = PaymentMapper.ToBll(await _uow.Payments.FindWithDetailsForUserAsync(id, appUserId));
        return payment == null ? null : await EnrichPaymentAsync(payment);
    }

    public async Task<BllPayment?> FindByServiceOrderAsync(Guid serviceOrderId)
    {
        var payment = PaymentMapper.ToBll(await _uow.Payments.FindByServiceOrderAsync(serviceOrderId));
        return payment == null ? null : await EnrichPaymentAsync(payment);
    }

    public async Task<BllPayment?> FindByServiceOrderForUserAsync(Guid serviceOrderId, Guid appUserId)
    {
        var payment = PaymentMapper.ToBll(await _uow.Payments.FindByServiceOrderForUserAsync(serviceOrderId, appUserId));
        return payment == null ? null : await EnrichPaymentAsync(payment);
    }

    // Staging only - no SaveChanges here.
    public BllPayment Add(BllPayment entity)
    {
        var added = _uow.Payments.Add(PaymentMapper.ToDomain(entity)!);
        return PaymentMapper.ToBll(added)!;
    }

    public BllPayment Update(BllPayment entity)
    {
        return UpdateAsync(entity).GetAwaiter().GetResult() ?? entity;
    }

    // Staging only - no SaveChanges here. Load-then-merge preserves CreatedAt.
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
        return await EnrichPaymentAsync(PaymentMapper.ToBll(existing)!);
    }

    // Staging only - no SaveChanges here.
    public void Remove(BllPayment entity)
    {
        _uow.Payments.Remove(PaymentMapper.ToDomain(entity)!);
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _uow.Payments.ExistsAsync(id);
    }

    public async Task<(BllPayment? payment, string? error)> CreateForServiceOrderAsync(Guid serviceOrderId, decimal amount)
    {
        var serviceOrder = await _uow.ServiceOrders.FindAsync(serviceOrderId);
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
        var bll = PaymentMapper.ToBll(added)!;
        bll.OwnerId = serviceOrder.AppUserId;
        bll = await EnrichPaymentAsync(bll);

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

    private async Task<List<BllPayment>> EnrichPaymentsAsync(IEnumerable<Payment> entities)
    {
        var payments = entities.Select(e => PaymentMapper.ToBll(e)!).ToList();
        foreach (var payment in payments)
        {
            await EnrichPaymentAsync(payment);
        }

        return payments;
    }

    private async Task<BllPayment> EnrichPaymentAsync(BllPayment payment)
    {
        var order = await _uow.ServiceOrders.FindAsync(payment.ServiceOrderId);
        if (order == null)
        {
            return payment;
        }

        payment.OwnerId = order.AppUserId;

        var vehicle = await _sender.Send(new GetVehicleByIdQuery(order.VehicleId));
        if (vehicle != null)
        {
            payment.VehicleInfo = $"{vehicle.Make} {vehicle.Model} ({vehicle.LicensePlate})";
        }

        var workshop = await _sender.Send(new GetWorkshopByIdQuery(order.WorkshopId));
        if (workshop != null)
        {
            payment.WorkshopName = workshop.Name;
        }
        return payment;
    }
}
