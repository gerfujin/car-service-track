using FluentAssertions;
using Moq;
using Workshops.Application.DTO;
using Workshops.Application.Services;
using Workshops.Contracts;
using Workshops.Contracts.Repositories;
using Workshops.Domain;

namespace CarServiceTrack.Tests.Unit.Services;

public class MechanicServiceTests
{
    private readonly Mock<IWorkshopsUnitOfWork> _uow = new();
    private readonly Mock<IMechanicRepository> _repo = new();
    private readonly MechanicService _sut;

    public MechanicServiceTests()
    {
        _uow.Setup(u => u.Mechanics).Returns(_repo.Object);
        _sut = new MechanicService(_uow.Object);
    }

    [Fact]
    public async Task AllAsync_WhenMechanicsExist_ReturnsAllMapped()
    {
        _repo.Setup(r => r.AllAsync())
            .ReturnsAsync(new List<Mechanic> { MakeMechanic("Alice"), MakeMechanic("Bob") });

        var result = (await _sut.AllAsync()).ToList();

        result.Should().HaveCount(2);
        result[0].FirstName.Should().Be("Alice");
    }

    [Fact]
    public async Task FindAsync_WithExistingId_ReturnsMapped()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.FindAsync(id)).ReturnsAsync(MakeMechanic("Charlie", id));

        var result = await _sut.FindAsync(id);

        result.Should().NotBeNull();
        result!.FirstName.Should().Be("Charlie");
    }

    [Fact]
    public async Task FindAsync_WithNonexistentId_ReturnsNull()
    {
        _repo.Setup(r => r.FindAsync(It.IsAny<Guid>())).ReturnsAsync((Mechanic?)null);

        var result = await _sut.FindAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public void Add_WithValidEntity_StagedAndReturnsMapped()
    {
        var dto = new BllMechanic { Id = Guid.NewGuid(), FirstName = "Dave", LastName = "Jones" };
        _repo.Setup(r => r.Add(It.IsAny<Mechanic>())).Returns(MakeMechanic("Dave", dto.Id));

        var result = _sut.Add(dto);

        result.FirstName.Should().Be("Dave");
        _repo.Verify(r => r.Add(It.IsAny<Mechanic>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithExistingMechanic_UpdatesFieldsAndReturns()
    {
        var id = Guid.NewGuid();
        var existing = MakeMechanic("Old", id);
        _repo.Setup(r => r.FindAsync(id)).ReturnsAsync(existing);
        _repo.Setup(r => r.Update(It.IsAny<Mechanic>())).Returns<Mechanic>(m => m);

        var dto = new BllMechanic { Id = id, FirstName = "New", LastName = "Name", Phone = "+372", Email = "m@shop.ee", Specialization = "Brakes" };
        var result = await _sut.UpdateAsync(dto);

        result.Should().NotBeNull();
        _repo.Verify(r => r.Update(It.Is<Mechanic>(m =>
            m.FirstName == "New" &&
            m.LastName == "Name" &&
            m.Specialization == "Brakes")), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithNonexistentMechanic_ReturnsNull()
    {
        _repo.Setup(r => r.FindAsync(It.IsAny<Guid>())).ReturnsAsync((Mechanic?)null);

        var result = await _sut.UpdateAsync(new BllMechanic { Id = Guid.NewGuid(), FirstName = "X", LastName = "Y" });

        result.Should().BeNull();
    }

    [Fact]
    public void Remove_DelegatesToRepository()
    {
        _sut.Remove(new BllMechanic { Id = Guid.NewGuid(), FirstName = "X", LastName = "Y" });
        _repo.Verify(r => r.Remove(It.IsAny<Mechanic>()), Times.Once);
    }

    [Fact]
    public async Task ExistsAsync_WhenMechanicExists_ReturnsTrue()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.ExistsAsync(id)).ReturnsAsync(true);

        var result = await _sut.ExistsAsync(id);

        result.Should().BeTrue();
    }

    private static Mechanic MakeMechanic(string firstName, Guid? id = null) =>
        new() { Id = id ?? Guid.NewGuid(), FirstName = firstName, LastName = "Smith" };
}
