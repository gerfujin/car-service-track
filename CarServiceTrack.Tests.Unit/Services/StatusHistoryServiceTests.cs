using FluentAssertions;
using Moq;
using Orders.Application.DTO;
using Orders.Application.Services;
using Orders.Contracts;
using Orders.Contracts.Repositories;
using Orders.Domain;
using Orders.Domain.Enums;

namespace CarServiceTrack.Tests.Unit.Services;

public class StatusHistoryServiceTests
{
    private readonly Mock<IOrdersUnitOfWork> _uow = new();
    private readonly Mock<IServiceOrderStatusHistoryRepository> _repo = new();
    private readonly StatusHistoryService _sut;

    public StatusHistoryServiceTests()
    {
        _uow.Setup(u => u.StatusHistories).Returns(_repo.Object);
        _sut = new StatusHistoryService(_uow.Object);
    }

    [Fact]
    public async Task AllByOrderAsync_WithOrderId_DelegatesToRepository()
    {
        var orderId = Guid.NewGuid();
        var entities = new List<ServiceOrderStatusHistory>
        {
            MakeHistory(orderId, ServiceOrderStatus.Pending),
            MakeHistory(orderId, ServiceOrderStatus.InProgress)
        };
        _repo.Setup(r => r.AllByOrderAsync(orderId)).ReturnsAsync(entities);

        var result = (await _sut.AllByOrderAsync(orderId)).ToList();

        result.Should().HaveCount(2);
        _repo.Verify(r => r.AllByOrderAsync(orderId), Times.Once);
    }

    [Fact]
    public async Task AllAsync_WhenHistoriesExist_ReturnsAllMapped()
    {
        _repo.Setup(r => r.AllAsync())
            .ReturnsAsync(new List<ServiceOrderStatusHistory> { MakeHistory(Guid.NewGuid(), ServiceOrderStatus.Pending) });

        var result = await _sut.AllAsync();

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task FindAsync_WithExistingId_ReturnsMapped()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.FindAsync(id)).ReturnsAsync(MakeHistory(Guid.NewGuid(), ServiceOrderStatus.Completed, id));

        var result = await _sut.FindAsync(id);

        result.Should().NotBeNull();
        result!.Status.Should().Be(ServiceOrderStatus.Completed);
    }

    [Fact]
    public async Task FindAsync_WithNonexistentId_ReturnsNull()
    {
        _repo.Setup(r => r.FindAsync(It.IsAny<Guid>())).ReturnsAsync((ServiceOrderStatusHistory?)null);

        var result = await _sut.FindAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public void Add_WithValidEntity_StagedAndReturnsMapped()
    {
        var dto = new BllStatusHistory { Id = Guid.NewGuid(), ServiceOrderId = Guid.NewGuid(), Status = ServiceOrderStatus.Accepted, ChangedAt = DateTime.UtcNow };
        var domain = MakeHistory(dto.ServiceOrderId, ServiceOrderStatus.Accepted, dto.Id);
        _repo.Setup(r => r.Add(It.IsAny<ServiceOrderStatusHistory>())).Returns(domain);

        var result = _sut.Add(dto);

        result.Should().NotBeNull();
        result.Status.Should().Be(ServiceOrderStatus.Accepted);
        _repo.Verify(r => r.Add(It.IsAny<ServiceOrderStatusHistory>()), Times.Once);
    }

    [Fact]
    public void Remove_DelegatesToRepository()
    {
        _sut.Remove(new BllStatusHistory { Id = Guid.NewGuid(), ServiceOrderId = Guid.NewGuid(), Status = ServiceOrderStatus.Pending, ChangedAt = DateTime.UtcNow });
        _repo.Verify(r => r.Remove(It.IsAny<ServiceOrderStatusHistory>()), Times.Once);
    }

    [Fact]
    public async Task ExistsAsync_WhenHistoryExists_ReturnsTrue()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.ExistsAsync(id)).ReturnsAsync(true);

        var result = await _sut.ExistsAsync(id);

        result.Should().BeTrue();
    }

    private static ServiceOrderStatusHistory MakeHistory(Guid orderId, ServiceOrderStatus status, Guid? id = null) =>
        new() { Id = id ?? Guid.NewGuid(), ServiceOrderId = orderId, Status = status, ChangedAt = DateTime.UtcNow };
}
