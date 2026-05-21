using App.BLL.DTO;
using App.BLL.Services;
using App.DAL.Contracts;
using App.Domain;
using App.Domain.Enums;
using Base.Domain;
using FluentAssertions;
using Moq;

namespace CarServiceTrack.Tests.Unit.Services;

public class PaymentServiceTests
{
    private readonly Mock<IAppUnitOfWork> _uow = new();
    private readonly Mock<IPaymentRepository> _paymentRepo = new();
    private readonly Mock<IServiceOrderRepository> _orderRepo = new();
    private readonly PaymentService _sut;

    public PaymentServiceTests()
    {
        _uow.Setup(u => u.Payments).Returns(_paymentRepo.Object);
        _uow.Setup(u => u.ServiceOrders).Returns(_orderRepo.Object);
        _sut = new PaymentService(_uow.Object);
    }

    // ── FindByServiceOrderAsync ───────────────────────────────────────────────
    [Fact]
    public async Task FindByServiceOrderAsync_WithExistingOrder_ReturnsMappedPayment()
    {
        var orderId = Guid.NewGuid();
        var payment = MakePayment(orderId: orderId);
        _paymentRepo.Setup(r => r.FindByServiceOrderAsync(orderId)).ReturnsAsync(payment);

        var result = await _sut.FindByServiceOrderAsync(orderId);

        result.Should().NotBeNull();
        result!.ServiceOrderId.Should().Be(orderId);
    }

    [Fact]
    public async Task FindByServiceOrderAsync_WithNonexistentOrder_ReturnsNull()
    {
        _paymentRepo.Setup(r => r.FindByServiceOrderAsync(It.IsAny<Guid>())).ReturnsAsync((Payment?)null);

        var result = await _sut.FindByServiceOrderAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    // ── CreateForServiceOrderAsync ────────────────────────────────────────────
    [Fact]
    public async Task CreateForServiceOrderAsync_WithValidOrder_CreatesAndReturnsPayment()
    {
        var orderId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var order = MakeOrderWithDetails(orderId, ownerId);
        _orderRepo.Setup(r => r.FindWithDetailsAsync(orderId)).ReturnsAsync(order);
        _paymentRepo.Setup(r => r.AnyByServiceOrderAsync(orderId)).ReturnsAsync(false);
        _paymentRepo.Setup(r => r.Add(It.IsAny<Payment>())).Returns<Payment>(p => p);

        var (payment, error) = await _sut.CreateForServiceOrderAsync(orderId, 150m);

        error.Should().BeNull();
        payment.Should().NotBeNull();
        payment!.Amount.Should().Be(150m);
        payment.Status.Should().Be(PaymentStatus.Pending);
        _paymentRepo.Verify(r => r.Add(It.Is<Payment>(p => p.Amount == 150m && p.Status == PaymentStatus.Pending)), Times.Once);
    }

    [Fact]
    public async Task CreateForServiceOrderAsync_WithNonexistentOrder_ReturnsError()
    {
        _orderRepo.Setup(r => r.FindWithDetailsAsync(It.IsAny<Guid>())).ReturnsAsync((ServiceOrder?)null);

        var (payment, error) = await _sut.CreateForServiceOrderAsync(Guid.NewGuid(), 100m);

        payment.Should().BeNull();
        error.Should().Be("Service order not found.");
        _paymentRepo.Verify(r => r.Add(It.IsAny<Payment>()), Times.Never);
    }

    [Fact]
    public async Task CreateForServiceOrderAsync_WhenPaymentAlreadyExists_ReturnsError()
    {
        var orderId = Guid.NewGuid();
        _orderRepo.Setup(r => r.FindWithDetailsAsync(orderId)).ReturnsAsync(MakeOrderWithDetails(orderId, Guid.NewGuid()));
        _paymentRepo.Setup(r => r.AnyByServiceOrderAsync(orderId)).ReturnsAsync(true);

        var (payment, error) = await _sut.CreateForServiceOrderAsync(orderId, 100m);

        payment.Should().BeNull();
        error.Should().Be("A payment invoice already exists for this service order.");
        _paymentRepo.Verify(r => r.Add(It.IsAny<Payment>()), Times.Never);
    }

    // ── MarkAsPaidAsync ───────────────────────────────────────────────────────
    [Fact]
    public async Task MarkAsPaidAsync_WithExistingPayment_SetsStatusPaidAndPaidAt()
    {
        var id = Guid.NewGuid();
        var payment = MakePayment(id, PaymentStatus.Pending);
        _paymentRepo.Setup(r => r.FindAsync(id)).ReturnsAsync(payment);
        _paymentRepo.Setup(r => r.Update(It.IsAny<Payment>())).Returns<Payment>(p => p);

        await _sut.MarkAsPaidAsync(id);

        _paymentRepo.Verify(r => r.Update(It.Is<Payment>(p =>
            p.Status == PaymentStatus.Paid &&
            p.PaidAt != null)), Times.Once);
    }

    [Fact]
    public async Task MarkAsPaidAsync_WithNonexistentPayment_DoesNotCallUpdate()
    {
        _paymentRepo.Setup(r => r.FindAsync(It.IsAny<Guid>())).ReturnsAsync((Payment?)null);

        await _sut.MarkAsPaidAsync(Guid.NewGuid());

        _paymentRepo.Verify(r => r.Update(It.IsAny<Payment>()), Times.Never);
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────
    [Fact]
    public async Task UpdateAsync_WithExistingPayment_UpdatesFieldsAndReturns()
    {
        var id = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var existing = MakePayment(id, PaymentStatus.Pending, orderId);
        _paymentRepo.Setup(r => r.FindAsync(id)).ReturnsAsync(existing);
        _paymentRepo.Setup(r => r.Update(It.IsAny<Payment>())).Returns<Payment>(p => p);

        var dto = new BllPayment { Id = id, Amount = 200m, Status = PaymentStatus.Paid, ServiceOrderId = orderId, PaymentMethod = "Card" };
        var result = await _sut.UpdateAsync(dto);

        result.Should().NotBeNull();
        _paymentRepo.Verify(r => r.Update(It.Is<Payment>(p =>
            p.Amount == 200m &&
            p.Status == PaymentStatus.Paid &&
            p.PaymentMethod == "Card")), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithNonexistentPayment_ReturnsNull()
    {
        _paymentRepo.Setup(r => r.FindAsync(It.IsAny<Guid>())).ReturnsAsync((Payment?)null);

        var result = await _sut.UpdateAsync(new BllPayment { Id = Guid.NewGuid(), ServiceOrderId = Guid.NewGuid() });

        result.Should().BeNull();
        _paymentRepo.Verify(r => r.Update(It.IsAny<Payment>()), Times.Never);
    }

    // ── AllByUserAsync ────────────────────────────────────────────────────────
    [Fact]
    public async Task AllByUserAsync_DelegatesToRepository()
    {
        var userId = Guid.NewGuid();
        _paymentRepo.Setup(r => r.AllByUserAsync(userId, null)).ReturnsAsync(new List<Payment>());

        var result = await _sut.AllByUserAsync(userId);

        result.Should().BeEmpty();
        _paymentRepo.Verify(r => r.AllByUserAsync(userId, null), Times.Once);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static Payment MakePayment(Guid? id = null, PaymentStatus status = PaymentStatus.Pending, Guid? orderId = null) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            Amount = 100m,
            Status = status,
            ServiceOrderId = orderId ?? Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

    private static ServiceOrder MakeOrderWithDetails(Guid id, Guid ownerId)
    {
        var vehicle = new Vehicle { Id = Guid.NewGuid(), Make = "BMW", Model = "X5", Year = 2022, LicensePlate = "BM123", OwnerId = ownerId };
        var workshop = new Workshop { Id = Guid.NewGuid(), Name = new LangStr("Test Workshop", "en"), Address = new LangStr("Addr", "en") };
        return new ServiceOrder
        {
            Id = id,
            VehicleId = vehicle.Id,
            Vehicle = vehicle,
            WorkshopId = workshop.Id,
            Workshop = workshop,
            Status = ServiceOrderStatus.Completed,
            OrderDate = DateTime.UtcNow
        };
    }
}
