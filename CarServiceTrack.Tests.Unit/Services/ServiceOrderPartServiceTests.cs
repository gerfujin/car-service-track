using FluentAssertions;
using MediatR;
using Moq;
using Orders.Application.DTO;
using Orders.Application.Services;
using Orders.Contracts;
using Orders.Contracts.Repositories;
using Orders.Domain;
using Orders.Domain.Enums;
using Workshops.Contracts.Queries;

namespace CarServiceTrack.Tests.Unit.Services;

public class ServiceOrderPartServiceTests
{
    private readonly Mock<IOrdersUnitOfWork> _uow = new();
    private readonly Mock<IServiceOrderPartRepository> _partRepo = new();
    private readonly Mock<IServiceOrderRepository> _orderRepo = new();
    private readonly Mock<ISender> _sender = new();
    private readonly ServiceOrderPartService _sut;

    public ServiceOrderPartServiceTests()
    {
        _uow.Setup(u => u.ServiceOrderParts).Returns(_partRepo.Object);
        _uow.Setup(u => u.ServiceOrders).Returns(_orderRepo.Object);
        _sut = new ServiceOrderPartService(_uow.Object, _sender.Object);
    }

    // ── CreateForApiAsync ─────────────────────────────────────────────────────
    [Fact]
    public async Task CreateForApiAsync_WithValidOrderAndPart_CreatesAndReturnsResult()
    {
        var orderId = Guid.NewGuid();
        var spareId = Guid.NewGuid();
        var order = MakeOrder(orderId);
        var sparePart = new SparePartDto(spareId, "Test Part", "PN-001", null, 45.99m, 10);
        _orderRepo.Setup(r => r.FindAsync(orderId)).ReturnsAsync(order);
        _sender.Setup(s => s.Send(It.IsAny<GetSparePartByIdQuery>(), default)).ReturnsAsync(sparePart);
        _partRepo.Setup(r => r.Add(It.IsAny<ServiceOrderPart>())).Returns<ServiceOrderPart>(p => p);

        var dto = new BllServiceOrderPart { ServiceOrderId = orderId, SparePartId = spareId, Quantity = 2, UnitPrice = 0 };
        var result = await _sut.CreateForApiAsync(dto);

        result.Error.Should().BeNull();
        result.Entity.Should().NotBeNull();
        result.RecalculateServiceOrderId.Should().Be(orderId);
        // When UnitPrice is 0, the spare part's price should be used
        _partRepo.Verify(r => r.Add(It.Is<ServiceOrderPart>(p => p.UnitPrice == 45.99m && p.Quantity == 2)), Times.Once);
    }

    [Fact]
    public async Task CreateForApiAsync_WhenOrderNotFound_ReturnsError()
    {
        _orderRepo.Setup(r => r.FindAsync(It.IsAny<Guid>())).ReturnsAsync((ServiceOrder?)null);

        var result = await _sut.CreateForApiAsync(new BllServiceOrderPart { ServiceOrderId = Guid.NewGuid(), SparePartId = Guid.NewGuid(), Quantity = 1 });

        result.Error.Should().Be("Service order not found.");
        result.Entity.Should().BeNull();
    }

    [Fact]
    public async Task CreateForApiAsync_WhenSparePartNotFound_ReturnsError()
    {
        var orderId = Guid.NewGuid();
        _orderRepo.Setup(r => r.FindAsync(orderId)).ReturnsAsync(MakeOrder(orderId));
        _sender.Setup(s => s.Send(It.IsAny<GetSparePartByIdQuery>(), default)).ReturnsAsync((SparePartDto?)null);

        var result = await _sut.CreateForApiAsync(new BllServiceOrderPart { ServiceOrderId = orderId, SparePartId = Guid.NewGuid(), Quantity = 1 });

        result.Error.Should().Be("Spare part not found.");
    }

    [Fact]
    public async Task CreateForApiAsync_WhenQuantityIsZero_ReturnsError()
    {
        var orderId = Guid.NewGuid();
        var spareId = Guid.NewGuid();
        _orderRepo.Setup(r => r.FindAsync(orderId)).ReturnsAsync(MakeOrder(orderId));
        _sender.Setup(s => s.Send(It.IsAny<GetSparePartByIdQuery>(), default))
            .ReturnsAsync(new SparePartDto(spareId, "Part", null, null, 10m, 5));

        var result = await _sut.CreateForApiAsync(new BllServiceOrderPart { ServiceOrderId = orderId, SparePartId = spareId, Quantity = 0 });

        result.Error.Should().Be("Quantity must be greater than 0.");
    }

    [Fact]
    public async Task CreateForApiAsync_WithExplicitPrice_UsesProvidedPriceNotSpareParts()
    {
        var orderId = Guid.NewGuid();
        var spareId = Guid.NewGuid();
        _orderRepo.Setup(r => r.FindAsync(orderId)).ReturnsAsync(MakeOrder(orderId));
        _sender.Setup(s => s.Send(It.IsAny<GetSparePartByIdQuery>(), default))
            .ReturnsAsync(new SparePartDto(spareId, "Part", null, null, 50m, 5));
        _partRepo.Setup(r => r.Add(It.IsAny<ServiceOrderPart>())).Returns<ServiceOrderPart>(p => p);

        var result = await _sut.CreateForApiAsync(new BllServiceOrderPart { ServiceOrderId = orderId, SparePartId = spareId, Quantity = 1, UnitPrice = 35m });

        result.Error.Should().BeNull();
        _partRepo.Verify(r => r.Add(It.Is<ServiceOrderPart>(p => p.UnitPrice == 35m)), Times.Once);
    }

    // ── UpdateForApiAsync ─────────────────────────────────────────────────────
    [Fact]
    public async Task UpdateForApiAsync_WhenPartNotFound_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _partRepo.Setup(r => r.FindAsync(id)).ReturnsAsync((ServiceOrderPart?)null);

        var result = await _sut.UpdateForApiAsync(id, new BllServiceOrderPart { Id = id, ServiceOrderId = Guid.NewGuid(), SparePartId = Guid.NewGuid(), Quantity = 1 });

        result.NotFound.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateForApiAsync_WhenNegativePrice_ReturnsError()
    {
        var id = Guid.NewGuid();
        _partRepo.Setup(r => r.FindAsync(id)).ReturnsAsync(MakeServiceOrderPart(id));

        var result = await _sut.UpdateForApiAsync(id, new BllServiceOrderPart { Id = id, ServiceOrderId = Guid.NewGuid(), SparePartId = Guid.NewGuid(), Quantity = 1, UnitPrice = -1m });

        result.Error.Should().Be("Price must be greater than or equal to 0.");
    }

    // ── RemoveForApiAsync ─────────────────────────────────────────────────────
    [Fact]
    public async Task RemoveForApiAsync_WithExistingPart_StagedForDeletion()
    {
        var id = Guid.NewGuid();
        var part = MakeServiceOrderPart(id);
        _partRepo.Setup(r => r.FindAsync(id)).ReturnsAsync(part);

        var result = await _sut.RemoveForApiAsync(id);

        result.NotFound.Should().BeFalse();
        result.RecalculateServiceOrderId.Should().Be(part.ServiceOrderId);
        _partRepo.Verify(r => r.Remove(part), Times.Once);
    }

    [Fact]
    public async Task RemoveForApiAsync_WithNonexistentPart_ReturnsNotFound()
    {
        _partRepo.Setup(r => r.FindAsync(It.IsAny<Guid>())).ReturnsAsync((ServiceOrderPart?)null);

        var result = await _sut.RemoveForApiAsync(Guid.NewGuid());

        result.NotFound.Should().BeTrue();
    }

    // ── AllForApiAsync ────────────────────────────────────────────────────────
    [Fact]
    public async Task AllForApiAsync_WhenMechanic_ReturnsAllParts()
    {
        var orderId = Guid.NewGuid();
        _partRepo.Setup(r => r.AllWithDetailsAsync(orderId))
            .ReturnsAsync(new List<ServiceOrderPart>
            {
                MakeServiceOrderPart(Guid.NewGuid(), orderId),
                MakeServiceOrderPart(Guid.NewGuid(), orderId)
            });
        // EnrichPartAsync calls sender for spare part info and orderRepo for owner
        _sender.Setup(s => s.Send(It.IsAny<GetSparePartByIdQuery>(), default)).ReturnsAsync((SparePartDto?)null);
        _orderRepo.Setup(r => r.FindAsync(It.IsAny<Guid>())).ReturnsAsync((ServiceOrder?)null);

        var result = (await _sut.AllForApiAsync(orderId, Guid.NewGuid(), isAdmin: false, isMechanic: true)).ToList();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task AllForApiAsync_WhenClientAndNoOwnedParts_ReturnsEmpty()
    {
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _partRepo.Setup(r => r.AllWithDetailsAsync(orderId)).ReturnsAsync(new List<ServiceOrderPart>());

        var result = await _sut.AllForApiAsync(orderId, userId, isAdmin: false, isMechanic: false);

        result.Should().BeEmpty();
    }

    // ── RecalculateOrderTotalAsync ────────────────────────────────────────────
    [Fact]
    public async Task RecalculateOrderTotalAsync_DelegatesToRepository()
    {
        var orderId = Guid.NewGuid();
        _partRepo.Setup(r => r.RecalculateOrderTotalAsync(orderId)).Returns(Task.CompletedTask);

        await _sut.RecalculateOrderTotalAsync(orderId);

        _partRepo.Verify(r => r.RecalculateOrderTotalAsync(orderId), Times.Once);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static ServiceOrder MakeOrder(Guid id) =>
        new() { Id = id, VehicleId = Guid.NewGuid(), WorkshopId = Guid.NewGuid(), AppUserId = Guid.NewGuid(), Status = ServiceOrderStatus.Pending };

    private static ServiceOrderPart MakeServiceOrderPart(Guid id, Guid? orderId = null) =>
        new() { Id = id, ServiceOrderId = orderId ?? Guid.NewGuid(), SparePartId = Guid.NewGuid(), Quantity = 1, UnitPrice = 20m };
}
