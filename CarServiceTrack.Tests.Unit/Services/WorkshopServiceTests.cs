using App.BLL.DTO;
using App.BLL.Services;
using App.DAL.Contracts;
using App.Domain;
using Base.Domain;
using FluentAssertions;
using Moq;

namespace CarServiceTrack.Tests.Unit.Services;

public class WorkshopServiceTests
{
    private readonly Mock<IAppUnitOfWork> _uow = new();
    private readonly Mock<IWorkshopRepository> _workshopRepo = new();
    private readonly Mock<IServiceOrderRepository> _orderRepo = new();
    private readonly WorkshopService _sut;

    public WorkshopServiceTests()
    {
        _uow.Setup(u => u.Workshops).Returns(_workshopRepo.Object);
        _uow.Setup(u => u.ServiceOrders).Returns(_orderRepo.Object);
        _sut = new WorkshopService(_uow.Object);
    }

    [Fact]
    public async Task AllAsync_ReturnsAllMapped()
    {
        _workshopRepo.Setup(r => r.AllAsync()).ReturnsAsync(new List<Workshop> { MakeWorkshop("Shop A"), MakeWorkshop("Shop B") });

        var result = (await _sut.AllAsync()).ToList();

        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Shop A");
    }

    [Fact]
    public async Task FindAsync_WithExistingId_ReturnsMapped()
    {
        var id = Guid.NewGuid();
        _workshopRepo.Setup(r => r.FindAsync(id)).ReturnsAsync(MakeWorkshop("My Shop", id));

        var result = await _sut.FindAsync(id);

        result.Should().NotBeNull();
        result!.Name.Should().Be("My Shop");
    }

    [Fact]
    public async Task FindAsync_WithNonexistentId_ReturnsNull()
    {
        _workshopRepo.Setup(r => r.FindAsync(It.IsAny<Guid>())).ReturnsAsync((Workshop?)null);

        var result = await _sut.FindAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public void Add_StagedAndReturnsMapped()
    {
        var dto = new BllWorkshop { Id = Guid.NewGuid(), Name = "New Shop", Address = "Main St" };
        var domain = MakeWorkshop("New Shop", dto.Id);
        _workshopRepo.Setup(r => r.Add(It.IsAny<Workshop>())).Returns(domain);

        var result = _sut.Add(dto);

        result.Should().NotBeNull();
        result.Name.Should().Be("New Shop");
    }

    [Fact]
    public async Task UpdateAsync_WithExistingWorkshop_UpdatesFieldsAndReturns()
    {
        var id = Guid.NewGuid();
        var existing = MakeWorkshop("Old Name", id);
        _workshopRepo.Setup(r => r.FindAsync(id)).ReturnsAsync(existing);
        _workshopRepo.Setup(r => r.Update(It.IsAny<Workshop>())).Returns<Workshop>(w => w);

        var dto = new BllWorkshop { Id = id, Name = "New Name", Address = "New Address", Phone = "+372 123", Email = "new@shop.ee" };
        var result = await _sut.UpdateAsync(dto);

        result.Should().NotBeNull();
        _workshopRepo.Verify(r => r.Update(It.Is<Workshop>(w =>
            w.Phone == "+372 123" &&
            w.Email == "new@shop.ee")), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithNonexistentWorkshop_ReturnsNull()
    {
        _workshopRepo.Setup(r => r.FindAsync(It.IsAny<Guid>())).ReturnsAsync((Workshop?)null);

        var result = await _sut.UpdateAsync(new BllWorkshop { Id = Guid.NewGuid(), Name = "X", Address = "Y" });

        result.Should().BeNull();
    }

    [Fact]
    public async Task CanDeleteAsync_WhenNoOrders_ReturnsTrue()
    {
        var id = Guid.NewGuid();
        _orderRepo.Setup(r => r.AnyByWorkshopAsync(id)).ReturnsAsync(false);

        var result = await _sut.CanDeleteAsync(id);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanDeleteAsync_WhenOrdersExist_ReturnsFalse()
    {
        var id = Guid.NewGuid();
        _orderRepo.Setup(r => r.AnyByWorkshopAsync(id)).ReturnsAsync(true);

        var result = await _sut.CanDeleteAsync(id);

        result.Should().BeFalse();
    }

    [Fact]
    public void Remove_DelegatesToRepository()
    {
        _sut.Remove(new BllWorkshop { Id = Guid.NewGuid(), Name = "X", Address = "Y" });
        _workshopRepo.Verify(r => r.Remove(It.IsAny<Workshop>()), Times.Once);
    }

    private static Workshop MakeWorkshop(string name, Guid? id = null) =>
        new() { Id = id ?? Guid.NewGuid(), Name = new LangStr(name, "en"), Address = new LangStr("Test Addr", "en") };
}
