using FluentAssertions;
using MediatR;
using Moq;
using Orders.Application.DTO;
using Orders.Application.Services;
using Orders.Contracts;
using Orders.Contracts.Repositories;
using Orders.Domain;
using Orders.Domain.Enums;
using Users.Contracts.Queries;
using Workshops.Contracts.Queries;

namespace CarServiceTrack.Tests.Unit.Services;

public class ServiceOrderServiceTests
{
    private readonly Mock<IOrdersUnitOfWork> _uow = new();
    private readonly Mock<IServiceOrderRepository> _orderRepo = new();
    private readonly Mock<IServiceOrderStatusHistoryRepository> _historyRepo = new();
    private readonly Mock<IPaymentRepository> _paymentRepo = new();
    private readonly Mock<ISender> _sender = new();
    private readonly ServiceOrderService _sut;

    public ServiceOrderServiceTests()
    {
        _uow.Setup(u => u.ServiceOrders).Returns(_orderRepo.Object);
        _uow.Setup(u => u.StatusHistories).Returns(_historyRepo.Object);
        _uow.Setup(u => u.Payments).Returns(_paymentRepo.Object);
        _sut = new ServiceOrderService(_uow.Object, _sender.Object);

        // Default enrichment stubs — return null for all cross-module queries
        _sender.Setup(s => s.Send(It.IsAny<GetVehicleByIdQuery>(), default)).ReturnsAsync((VehicleDto?)null);
        _sender.Setup(s => s.Send(It.IsAny<GetWorkshopByIdQuery>(), default)).ReturnsAsync((WorkshopDto?)null);
        _sender.Setup(s => s.Send(It.IsAny<GetMechanicByIdQuery>(), default)).ReturnsAsync((MechanicDto?)null);
        _sender.Setup(s => s.Send(It.IsAny<GetOwnerByAppUserIdQuery>(), default)).ReturnsAsync((OwnerDto?)null);
        _paymentRepo.Setup(r => r.FindByServiceOrderAsync(It.IsAny<Guid>())).ReturnsAsync((Payment?)null);
    }

    // ── AllAsync ──────────────────────────────────────────────────────────────
    [Fact]
    public async Task AllAsync_WhenOrdersExist_ReturnsAllMapped()
    {
        var entities = new List<ServiceOrder> { MakeOrder(), MakeOrder() };
        _orderRepo.Setup(r => r.AllAsync()).ReturnsAsync(entities);
        // EnrichOrderAsync calls FindWithDetailsAsync for each order
        _orderRepo.Setup(r => r.FindWithDetailsAsync(It.IsAny<Guid>())).ReturnsAsync((ServiceOrder?)null);

        var result = await _sut.AllAsync();

        result.Should().HaveCount(2);
    }

    // ── AllByUserAsync ────────────────────────────────────────────────────────
    [Fact]
    public async Task AllByUserAsync_WithUserId_DelegatesToRepository()
    {
        var userId = Guid.NewGuid();
        var entities = new List<ServiceOrder> { MakeOrder() };
        _orderRepo.Setup(r => r.AllByUserAsync(userId)).ReturnsAsync(entities);
        _orderRepo.Setup(r => r.FindWithDetailsAsync(It.IsAny<Guid>())).ReturnsAsync((ServiceOrder?)null);

        var result = await _sut.AllByUserAsync(userId);

        result.Should().HaveCount(1);
        _orderRepo.Verify(r => r.AllByUserAsync(userId), Times.Once);
    }

    // ── FindAsync ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task FindAsync_WithExistingId_ReturnsMappedOrder()
    {
        var id = Guid.NewGuid();
        var entity = MakeOrder(id);
        _orderRepo.Setup(r => r.FindAsync(id)).ReturnsAsync(entity);
        _orderRepo.Setup(r => r.FindWithDetailsAsync(id)).ReturnsAsync((ServiceOrder?)null);

        var result = await _sut.FindAsync(id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
    }

    [Fact]
    public async Task FindAsync_WithNonexistentId_ReturnsNull()
    {
        _orderRepo.Setup(r => r.FindAsync(It.IsAny<Guid>())).ReturnsAsync((ServiceOrder?)null);

        var result = await _sut.FindAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    // ── FindWithDetailsAsync ──────────────────────────────────────────────────
    [Fact]
    public async Task FindWithDetailsAsync_WithExistingId_ReturnsMappedOrder()
    {
        var id = Guid.NewGuid();
        _orderRepo.Setup(r => r.FindWithDetailsAsync(id)).ReturnsAsync(MakeOrder(id));

        var result = await _sut.FindWithDetailsAsync(id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
    }

    [Fact]
    public async Task FindWithDetailsAsync_WithNonexistentId_ReturnsNull()
    {
        _orderRepo.Setup(r => r.FindWithDetailsAsync(It.IsAny<Guid>())).ReturnsAsync((ServiceOrder?)null);

        var result = await _sut.FindWithDetailsAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    // ── IsOwnedByUserAsync ────────────────────────────────────────────────────
    [Fact]
    public async Task IsOwnedByUserAsync_WhenOwned_ReturnsTrue()
    {
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _orderRepo.Setup(r => r.IsOwnedByUserAsync(orderId, userId)).ReturnsAsync(true);

        var result = await _sut.IsOwnedByUserAsync(orderId, userId);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsOwnedByUserAsync_WhenNotOwned_ReturnsFalse()
    {
        _orderRepo.Setup(r => r.IsOwnedByUserAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync(false);

        var result = await _sut.IsOwnedByUserAsync(Guid.NewGuid(), Guid.NewGuid());

        result.Should().BeFalse();
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────
    [Fact]
    public async Task UpdateAsync_WithExistingId_UpdatesAndReturns()
    {
        var id = Guid.NewGuid();
        var existing = MakeOrder(id);
        _orderRepo.Setup(r => r.FindAsync(id)).ReturnsAsync(existing);
        _orderRepo.Setup(r => r.Update(It.IsAny<ServiceOrder>())).Returns(existing);
        _orderRepo.Setup(r => r.UpdateServiceItemsAsync(It.IsAny<Guid>(), It.IsAny<ICollection<Guid>>()))
            .Returns(Task.CompletedTask);
        _orderRepo.Setup(r => r.FindWithDetailsAsync(id)).ReturnsAsync((ServiceOrder?)null);

        var dto = new BllServiceOrder { Id = id, VehicleId = existing.VehicleId, WorkshopId = existing.WorkshopId, Status = ServiceOrderStatus.InProgress, OrderDate = DateTime.UtcNow, ServiceIds = new List<Guid>() };
        var result = await _sut.UpdateAsync(dto);

        result.Should().NotBeNull();
        _orderRepo.Verify(r => r.Update(It.Is<ServiceOrder>(o => o.Status == ServiceOrderStatus.InProgress)), Times.Once);
        _orderRepo.Verify(r => r.UpdateServiceItemsAsync(id, It.IsAny<ICollection<Guid>>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithNonexistentId_ReturnsNull()
    {
        _orderRepo.Setup(r => r.FindAsync(It.IsAny<Guid>())).ReturnsAsync((ServiceOrder?)null);

        var result = await _sut.UpdateAsync(new BllServiceOrder { Id = Guid.NewGuid(), ServiceIds = new List<Guid>() });

        result.Should().BeNull();
    }

    // ── SetStatusAsync ────────────────────────────────────────────────────────
    [Fact]
    public async Task SetStatusAsync_WithExistingOrder_SetsStatusAndStagesHistory()
    {
        var id = Guid.NewGuid();
        var order = MakeOrder(id, ServiceOrderStatus.Pending);
        _orderRepo.Setup(r => r.FindAsync(id)).ReturnsAsync(order);
        _orderRepo.Setup(r => r.Update(It.IsAny<ServiceOrder>())).Returns(order);
        _historyRepo.Setup(r => r.Add(It.IsAny<ServiceOrderStatusHistory>()))
            .Returns<ServiceOrderStatusHistory>(h => h);

        var result = await _sut.SetStatusAsync(id, ServiceOrderStatus.InProgress, "Moving to in-progress");

        result.Should().BeTrue();
        _orderRepo.Verify(r => r.Update(It.Is<ServiceOrder>(o => o.Status == ServiceOrderStatus.InProgress)), Times.Once);
        _historyRepo.Verify(r => r.Add(It.Is<ServiceOrderStatusHistory>(h =>
            h.ServiceOrderId == id &&
            h.Status == ServiceOrderStatus.InProgress)), Times.Once);
    }

    [Fact]
    public async Task SetStatusAsync_WithNonexistentOrder_ReturnsFalse()
    {
        _orderRepo.Setup(r => r.FindAsync(It.IsAny<Guid>())).ReturnsAsync((ServiceOrder?)null);

        var result = await _sut.SetStatusAsync(Guid.NewGuid(), ServiceOrderStatus.Completed, null);

        result.Should().BeFalse();
        _historyRepo.Verify(r => r.Add(It.IsAny<ServiceOrderStatusHistory>()), Times.Never);
    }

    [Fact]
    public async Task SetStatusAsync_WhenStatusIsCompleted_SetsCompletedDate()
    {
        var id = Guid.NewGuid();
        var order = MakeOrder(id, ServiceOrderStatus.InProgress);
        _orderRepo.Setup(r => r.FindAsync(id)).ReturnsAsync(order);
        _orderRepo.Setup(r => r.Update(It.IsAny<ServiceOrder>())).Returns(order);
        _historyRepo.Setup(r => r.Add(It.IsAny<ServiceOrderStatusHistory>()))
            .Returns<ServiceOrderStatusHistory>(h => h);

        await _sut.SetStatusAsync(id, ServiceOrderStatus.Completed, null);

        _orderRepo.Verify(r => r.Update(It.Is<ServiceOrder>(o =>
            o.Status == ServiceOrderStatus.Completed &&
            o.CompletedDate != null)), Times.Once);
    }

    // ── AddWithItemsAsync ─────────────────────────────────────────────────────
    [Fact]
    public async Task AddWithItemsAsync_WithServiceIds_StagesOrderAndCallsAddItems()
    {
        var serviceId = Guid.NewGuid();
        var order = MakeOrder();
        _orderRepo.Setup(r => r.Add(It.IsAny<ServiceOrder>())).Returns(order);
        _orderRepo.Setup(r => r.AddServiceItemsAsync(It.IsAny<Guid>(), It.IsAny<ICollection<Guid>>()))
            .Returns(Task.CompletedTask);
        _orderRepo.Setup(r => r.FindWithDetailsAsync(It.IsAny<Guid>())).ReturnsAsync((ServiceOrder?)null);

        var dto = new BllServiceOrder
        {
            VehicleId = Guid.NewGuid(),
            WorkshopId = Guid.NewGuid(),
            Status = ServiceOrderStatus.Pending,
            OrderDate = DateTime.UtcNow,
            ServiceIds = new List<Guid> { serviceId }
        };

        var result = await _sut.AddWithItemsAsync(dto);

        result.Should().NotBeNull();
        _orderRepo.Verify(r => r.Add(It.IsAny<ServiceOrder>()), Times.Once);
        _orderRepo.Verify(r => r.AddServiceItemsAsync(It.IsAny<Guid>(), It.Is<ICollection<Guid>>(s => s.Contains(serviceId))), Times.Once);
    }

    [Fact]
    public async Task AddWithItemsAsync_WithNoServiceIds_DoesNotCallAddItems()
    {
        var order = MakeOrder();
        _orderRepo.Setup(r => r.Add(It.IsAny<ServiceOrder>())).Returns(order);
        _orderRepo.Setup(r => r.FindWithDetailsAsync(It.IsAny<Guid>())).ReturnsAsync((ServiceOrder?)null);

        var dto = new BllServiceOrder { VehicleId = Guid.NewGuid(), WorkshopId = Guid.NewGuid(), Status = ServiceOrderStatus.Pending, OrderDate = DateTime.UtcNow, ServiceIds = new List<Guid>() };

        await _sut.AddWithItemsAsync(dto);

        _orderRepo.Verify(r => r.AddServiceItemsAsync(It.IsAny<Guid>(), It.IsAny<ICollection<Guid>>()), Times.Never);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static ServiceOrder MakeOrder(Guid? id = null, ServiceOrderStatus status = ServiceOrderStatus.Pending) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            VehicleId = Guid.NewGuid(),
            WorkshopId = Guid.NewGuid(),
            AppUserId = Guid.NewGuid(),
            Status = status,
            OrderDate = DateTime.UtcNow,
            ServiceOrderItems = new List<ServiceOrderItem>()
        };
}
