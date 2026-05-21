using App.BLL.DTO;
using App.BLL.Services;
using App.DAL.Contracts;
using App.Domain;
using Base.Domain;
using FluentAssertions;
using Moq;

namespace CarServiceTrack.Tests.Unit.Services;

public class ServiceServiceTests
{
    private readonly Mock<IAppUnitOfWork> _uow = new();
    private readonly Mock<IServiceRepository> _repo = new();
    private readonly ServiceService _sut;

    public ServiceServiceTests()
    {
        _uow.Setup(u => u.Services).Returns(_repo.Object);
        _sut = new ServiceService(_uow.Object);
    }

    [Fact]
    public async Task AllAsync_ReturnsAllMapped()
    {
        _repo.Setup(r => r.AllAsync()).ReturnsAsync(new List<Service> { MakeService("Oil Change", 49.99m), MakeService("Brake Check", 39.99m) });

        var result = (await _sut.AllAsync()).ToList();

        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Oil Change");
    }

    [Fact]
    public async Task FindAsync_WithExistingId_ReturnsMapped()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.FindAsync(id)).ReturnsAsync(MakeService("Diagnostics", 59.99m, id));

        var result = await _sut.FindAsync(id);

        result.Should().NotBeNull();
        result!.BasePrice.Should().Be(59.99m);
    }

    [Fact]
    public async Task FindAsync_WithNonexistentId_ReturnsNull()
    {
        _repo.Setup(r => r.FindAsync(It.IsAny<Guid>())).ReturnsAsync((Service?)null);

        var result = await _sut.FindAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public void Add_StagedAndReturnsMapped()
    {
        var dto = new BllService { Id = Guid.NewGuid(), Name = "New Service", BasePrice = 25m };
        _repo.Setup(r => r.Add(It.IsAny<Service>())).Returns(MakeService("New Service", 25m, dto.Id));

        var result = _sut.Add(dto);

        result.Name.Should().Be("New Service");
        _repo.Verify(r => r.Add(It.IsAny<Service>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithExistingService_UpdatesFieldsAndReturns()
    {
        var id = Guid.NewGuid();
        var existing = MakeService("Old Name", 10m, id);
        _repo.Setup(r => r.FindAsync(id)).ReturnsAsync(existing);
        _repo.Setup(r => r.Update(It.IsAny<Service>())).Returns<Service>(s => s);

        var dto = new BllService { Id = id, Name = "New Name", Description = "New Desc", BasePrice = 99m, EstimatedTimeMinutes = 60 };
        var result = await _sut.UpdateAsync(dto);

        result.Should().NotBeNull();
        _repo.Verify(r => r.Update(It.Is<Service>(s =>
            s.BasePrice == 99m &&
            s.EstimatedTimeMinutes == 60)), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithNonexistentService_ReturnsNull()
    {
        _repo.Setup(r => r.FindAsync(It.IsAny<Guid>())).ReturnsAsync((Service?)null);

        var result = await _sut.UpdateAsync(new BllService { Id = Guid.NewGuid(), Name = "X", BasePrice = 1m });

        result.Should().BeNull();
    }

    [Fact]
    public void Remove_DelegatesToRepository()
    {
        _sut.Remove(new BllService { Id = Guid.NewGuid(), Name = "X", BasePrice = 1m });
        _repo.Verify(r => r.Remove(It.IsAny<Service>()), Times.Once);
    }

    [Fact]
    public async Task ExistsAsync_WhenExists_ReturnsTrue()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.ExistsAsync(id)).ReturnsAsync(true);
        (await _sut.ExistsAsync(id)).Should().BeTrue();
    }

    private static Service MakeService(string name, decimal price, Guid? id = null) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            Name = new LangStr(name, "en"),
            Description = new LangStr("Test description", "en"),
            BasePrice = price
        };
}
