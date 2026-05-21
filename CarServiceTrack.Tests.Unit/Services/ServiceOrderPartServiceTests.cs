using App.BLL.DTO;
using App.BLL.Services;
using App.DAL.Contracts;
using App.Domain;
using Base.Domain;
using FluentAssertions;
using Moq;

namespace CarServiceTrack.Tests.Unit.Services;

public class ServiceOrderPartServiceTests
{
    private readonly Mock<IAppUnitOfWork> _uow = new();
    private readonly Mock<IServiceOrderPartRepository> _partRepo = new();
    private readonly Mock<IServiceOrderRepository> _orderRepo = new();
    private readonly Mock<ISparePartRepository> _spareRepo = new();
    private readonly Mock<IOwnerRepository> _ownerRepo = new();
    private readonly ServiceOrderPartService _sut;

    public ServiceOrderPartServiceTests()
    {
        _uow.Setup(u => u.ServiceOrderParts).Returns(_partRepo.Object);
        _uow.Setup(u => u.ServiceOrders).Returns(_orderRepo.Object);
        _uow.Setup(u => u.SpareParts).Returns(_spareRepo.Object);
        _uow.Setup(u => u.Owners).Returns(_ownerRepo.Object);
        _sut = new ServiceOrderPartService(_uow.Object);
    }

    // ── CreateForApiAsync ─────────────────────────────────────────────────────
    [Fact]
    public async Task CreateForApiAsync_WithValidOrderAndPart_CreatesAndReturnsResult()
    {
        var orderId = Guid.NewGuid();
        var spareId = Guid.NewGuid();
        var order = MakeOrder(orderId);
        var sparePart = MakeSparePart(spareId, 45.99m);
        _orderRepo.Setup(r => r.FindAsync(orderId)).ReturnsAsync(order);
        _spareRepo.Setup(r => r.FindAsync(spareId)).ReturnsAsync(sparePart);
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
        _spareRepo.Setup(r => r.FindAsync(It.IsAny<Guid>())).ReturnsAsync((SparePart?)null);

        var result = await _sut.CreateForApiAsync(new BllServiceOrderPart { ServiceOrderId = orderId, SparePartId = Guid.NewGuid(), Quantity = 1 });

        result.Error.Should().Be("Spare part not found.");
    }

    [Fact]
    public async Task CreateForApiAsync_WhenQuantityIsZero_ReturnsError()
    {
        var orderId = Guid.NewGuid();
        var spareId = Guid.NewGuid();
        _orderRepo.Setup(r => r.FindAsync(orderId)).ReturnsAsync(MakeOrder(orderId));
        _spareRepo.Setup(r => r.FindAsync(spareId)).ReturnsAsync(MakeSparePart(spareId, 10m));

        var result = await _sut.CreateForApiAsync(new BllServiceOrderPart { ServiceOrderId = orderId, SparePartId = spareId, Quantity = 0 });

        result.Error.Should().Be("Quantity must be greater than 0.");
    }

    [Fact]
    public async Task CreateForApiAsync_WithExplicitPrice_UsesProvidedPriceNotSpareParts()
    {
        var orderId = Guid.NewGuid();
        var spareId = Guid.NewGuid();
        _orderRepo.Setup(r => r.FindAsync(orderId)).ReturnsAsync(MakeOrder(orderId));
        _spareRepo.Setup(r => r.FindAsync(spareId)).ReturnsAsync(MakeSparePart(spareId, 50m));
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
        _partRepo.Setup(r => r.AllWithDetailsAsync(orderId)).ReturnsAsync(new List<ServiceOrderPart>
        {
            MakeServiceOrderPart(Guid.NewGuid(), orderId),
            MakeServiceOrderPart(Guid.NewGuid(), orderId)
        });

        var result = (await _sut.AllForApiAsync(orderId, Guid.NewGuid(), isAdmin: false, isMechanic: true)).ToList();

        result.Should().HaveCount(2);
        _ownerRepo.Verify(r => r.FindByUserAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task AllForApiAsync_WhenClientAndNoOwner_ReturnsEmpty()
    {
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _partRepo.Setup(r => r.AllWithDetailsAsync(orderId)).ReturnsAsync(new List<ServiceOrderPart>());
        _ownerRepo.Setup(r => r.FindByUserAsync(userId)).ReturnsAsync((Owner?)null);

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
        new() { Id = id, VehicleId = Guid.NewGuid(), WorkshopId = Guid.NewGuid(), Status = App.Domain.Enums.ServiceOrderStatus.Pending };

    private static SparePart MakeSparePart(Guid id, decimal price) =>
        new() { Id = id, Name = new LangStr("Test Part", "en"), UnitPrice = price, StockQuantity = 10 };

    private static ServiceOrderPart MakeServiceOrderPart(Guid id, Guid? orderId = null) =>
        new() { Id = id, ServiceOrderId = orderId ?? Guid.NewGuid(), SparePartId = Guid.NewGuid(), Quantity = 1, UnitPrice = 20m };
}
