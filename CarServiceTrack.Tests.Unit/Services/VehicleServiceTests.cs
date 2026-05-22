using FluentAssertions;
using Moq;
using Users.Application.DTO;
using Users.Application.Services;
using Users.Contracts;
using Users.Contracts.Repositories;
using Users.Domain;

namespace CarServiceTrack.Tests.Unit.Services;

public class VehicleServiceTests
{
    private readonly Mock<IUsersUnitOfWork> _uow = new();
    private readonly Mock<IVehicleRepository> _vehicleRepo = new();
    private readonly Mock<IOwnerRepository> _ownerRepo = new();
    private readonly VehicleService _sut;

    public VehicleServiceTests()
    {
        _uow.Setup(u => u.Vehicles).Returns(_vehicleRepo.Object);
        _uow.Setup(u => u.Owners).Returns(_ownerRepo.Object);
        _sut = new VehicleService(_uow.Object);
    }

    // ── AllAsync ──────────────────────────────────────────────────────────────
    [Fact]
    public async Task AllAsync_WhenVehiclesExist_ReturnsAllMappedVehicles()
    {
        var ownerId = Guid.NewGuid();
        var entities = new List<Vehicle>
        {
            MakeVehicle(ownerId, "Toyota", "Camry"),
            MakeVehicle(ownerId, "Honda", "Civic")
        };
        _vehicleRepo.Setup(r => r.AllAsync()).ReturnsAsync(entities);

        var result = (await _sut.AllAsync()).ToList();

        result.Should().HaveCount(2);
        result[0].Make.Should().Be("Toyota");
        result[1].Make.Should().Be("Honda");
    }

    [Fact]
    public async Task AllAsync_WhenNoVehicles_ReturnsEmptyList()
    {
        _vehicleRepo.Setup(r => r.AllAsync()).ReturnsAsync(new List<Vehicle>());

        var result = await _sut.AllAsync();

        result.Should().BeEmpty();
    }

    // ── AllByUserAsync ────────────────────────────────────────────────────────
    [Fact]
    public async Task AllByUserAsync_WithUserId_ReturnsOnlyUserVehicles()
    {
        var userId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var entities = new List<Vehicle> { MakeVehicle(ownerId, "Ford", "Focus") };
        _vehicleRepo.Setup(r => r.AllByUserAsync(userId)).ReturnsAsync(entities);

        var result = (await _sut.AllByUserAsync(userId)).ToList();

        result.Should().HaveCount(1);
        result[0].Make.Should().Be("Ford");
        _vehicleRepo.Verify(r => r.AllByUserAsync(userId), Times.Once);
    }

    // ── FindAsync ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task FindAsync_WithExistingId_ReturnsMappedVehicle()
    {
        var id = Guid.NewGuid();
        var entity = MakeVehicle(Guid.NewGuid(), "BMW", "3 Series", id);
        _vehicleRepo.Setup(r => r.FindAsync(id)).ReturnsAsync(entity);

        var result = await _sut.FindAsync(id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
        result.Make.Should().Be("BMW");
    }

    [Fact]
    public async Task FindAsync_WithNonexistentId_ReturnsNull()
    {
        _vehicleRepo.Setup(r => r.FindAsync(It.IsAny<Guid>())).ReturnsAsync((Vehicle?)null);

        var result = await _sut.FindAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    // ── FindByUserAsync ───────────────────────────────────────────────────────
    [Fact]
    public async Task FindByUserAsync_WhenUserOwnsVehicle_ReturnsMappedVehicle()
    {
        var vehicleId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var entity = MakeVehicle(Guid.NewGuid(), "Audi", "A4", vehicleId);
        _vehicleRepo.Setup(r => r.FindByUserAsync(vehicleId, userId)).ReturnsAsync(entity);

        var result = await _sut.FindByUserAsync(vehicleId, userId);

        result.Should().NotBeNull();
        result!.Make.Should().Be("Audi");
    }

    [Fact]
    public async Task FindByUserAsync_WhenUserDoesNotOwnVehicle_ReturnsNull()
    {
        _vehicleRepo.Setup(r => r.FindByUserAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync((Vehicle?)null);

        var result = await _sut.FindByUserAsync(Guid.NewGuid(), Guid.NewGuid());

        result.Should().BeNull();
    }

    // ── Add ───────────────────────────────────────────────────────────────────
    [Fact]
    public void Add_WithValidEntity_StagedAndReturnsMappedVehicle()
    {
        var ownerId = Guid.NewGuid();
        var dto = new BllVehicle { Id = Guid.NewGuid(), Make = "Volvo", Model = "XC60", Year = 2022, LicensePlate = "XC123", OwnerId = ownerId };
        var domain = MakeVehicle(ownerId, "Volvo", "XC60", dto.Id);
        _vehicleRepo.Setup(r => r.Add(It.IsAny<Vehicle>())).Returns(domain);

        var result = _sut.Add(dto);

        result.Should().NotBeNull();
        result.Make.Should().Be("Volvo");
        _vehicleRepo.Verify(r => r.Add(It.IsAny<Vehicle>()), Times.Once);
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────
    [Fact]
    public async Task UpdateAsync_WithExistingId_UpdatesEditableFieldsAndReturns()
    {
        var id = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var existing = MakeVehicle(ownerId, "Old Make", "Old Model", id);
        var updated = MakeVehicle(ownerId, "Old Make", "Old Model", id);
        _vehicleRepo.Setup(r => r.FindAsync(id)).ReturnsAsync(existing);
        _vehicleRepo.Setup(r => r.Update(It.IsAny<Vehicle>())).Returns(updated);

        var dto = new BllVehicle { Id = id, Make = "New Make", Model = "New Model", Year = 2024, LicensePlate = "NEW123", OwnerId = ownerId };
        var result = await _sut.UpdateAsync(dto);

        result.Should().NotBeNull();
        _vehicleRepo.Verify(r => r.Update(It.Is<Vehicle>(v => v.Make == "New Make" && v.Model == "New Model")), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithNonexistentId_ReturnsNull()
    {
        _vehicleRepo.Setup(r => r.FindAsync(It.IsAny<Guid>())).ReturnsAsync((Vehicle?)null);

        var result = await _sut.UpdateAsync(new BllVehicle { Id = Guid.NewGuid(), Make = "X", Model = "Y", Year = 2020, LicensePlate = "Z", OwnerId = Guid.NewGuid() });

        result.Should().BeNull();
        _vehicleRepo.Verify(r => r.Update(It.IsAny<Vehicle>()), Times.Never);
    }

    // ── ExistsAsync ───────────────────────────────────────────────────────────
    [Fact]
    public async Task ExistsAsync_WhenVehicleExists_ReturnsTrue()
    {
        var id = Guid.NewGuid();
        _vehicleRepo.Setup(r => r.ExistsAsync(id)).ReturnsAsync(true);

        var result = await _sut.ExistsAsync(id);

        result.Should().BeTrue();
    }

    // ── Remove ────────────────────────────────────────────────────────────────
    [Fact]
    public void Remove_WithValidEntity_DelegatesToRepository()
    {
        var dto = new BllVehicle { Id = Guid.NewGuid(), Make = "X", Model = "Y", Year = 2020, LicensePlate = "Z", OwnerId = Guid.NewGuid() };

        _sut.Remove(dto);

        _vehicleRepo.Verify(r => r.Remove(It.IsAny<Vehicle>()), Times.Once);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static Vehicle MakeVehicle(Guid ownerId, string make, string model, Guid? id = null) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            Make = make,
            Model = model,
            Year = 2020,
            LicensePlate = $"{make[..2].ToUpper()}123",
            OwnerId = ownerId
        };
}
