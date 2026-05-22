using Base.Domain;
using FluentAssertions;
using Moq;
using Workshops.Application.DTO;
using Workshops.Application.Services;
using Workshops.Contracts;
using Workshops.Contracts.Repositories;
using Workshops.Domain;

namespace CarServiceTrack.Tests.Unit.Services;

public class SparePartServiceTests
{
    private readonly Mock<IWorkshopsUnitOfWork> _uow = new();
    private readonly Mock<ISparePartRepository> _repo = new();
    private readonly SparePartService _sut;

    public SparePartServiceTests()
    {
        _uow.Setup(u => u.SpareParts).Returns(_repo.Object);
        _sut = new SparePartService(_uow.Object);
    }

    [Fact]
    public async Task AllAsync_WhenPartsExist_ReturnsAllMapped()
    {
        _repo.Setup(r => r.AllAsync())
            .ReturnsAsync(new List<SparePart>
            {
                MakePart("Oil Filter", 12.99m),
                MakePart("Air Filter", 18.99m)
            });

        var result = (await _sut.AllAsync()).ToList();

        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Oil Filter");
    }

    [Fact]
    public async Task FindAsync_WithExistingId_ReturnsMapped()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.FindAsync(id)).ReturnsAsync(MakePart("Spark Plug", 32.99m, id));

        var result = await _sut.FindAsync(id);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Spark Plug");
    }

    [Fact]
    public async Task FindAsync_WithNonexistentId_ReturnsNull()
    {
        _repo.Setup(r => r.FindAsync(It.IsAny<Guid>())).ReturnsAsync((SparePart?)null);

        var result = await _sut.FindAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public void Add_WithValidEntity_StagedAndReturnsMapped()
    {
        var dto = new BllSparePart { Id = Guid.NewGuid(), Name = "New Part", UnitPrice = 10m, StockQuantity = 5 };
        var domain = MakePart("New Part", 10m, dto.Id);
        _repo.Setup(r => r.Add(It.IsAny<SparePart>())).Returns(domain);

        var result = _sut.Add(dto);

        result.Should().NotBeNull();
        result.Name.Should().Be("New Part");
        _repo.Verify(r => r.Add(It.IsAny<SparePart>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithExistingPart_UpdatesFieldsAndReturns()
    {
        var id = Guid.NewGuid();
        var existing = MakePart("Old Name", 5m, id);
        _repo.Setup(r => r.FindAsync(id)).ReturnsAsync(existing);
        _repo.Setup(r => r.Update(It.IsAny<SparePart>())).Returns<SparePart>(p => p);

        var dto = new BllSparePart { Id = id, Name = "New Name", UnitPrice = 99m, StockQuantity = 20, PartNumber = "PN-001" };
        var result = await _sut.UpdateAsync(dto);

        result.Should().NotBeNull();
        _repo.Verify(r => r.Update(It.Is<SparePart>(p =>
            p.PartNumber == "PN-001" &&
            p.UnitPrice == 99m &&
            p.StockQuantity == 20)), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithNonexistentPart_ReturnsNull()
    {
        _repo.Setup(r => r.FindAsync(It.IsAny<Guid>())).ReturnsAsync((SparePart?)null);

        var result = await _sut.UpdateAsync(new BllSparePart { Id = Guid.NewGuid(), Name = "X" });

        result.Should().BeNull();
    }

    [Fact]
    public void Remove_DelegatesToRepository()
    {
        var dto = new BllSparePart { Id = Guid.NewGuid(), Name = "X", UnitPrice = 1m };
        _sut.Remove(dto);
        _repo.Verify(r => r.Remove(It.IsAny<SparePart>()), Times.Once);
    }

    [Fact]
    public async Task ExistsAsync_WhenPartExists_ReturnsTrue()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.ExistsAsync(id)).ReturnsAsync(true);

        var result = await _sut.ExistsAsync(id);

        result.Should().BeTrue();
    }

    private static SparePart MakePart(string name, decimal price, Guid? id = null) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            Name = new LangStr(name, "en"),
            UnitPrice = price,
            StockQuantity = 10
        };
}
