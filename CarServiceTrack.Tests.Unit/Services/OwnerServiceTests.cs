using FluentAssertions;
using Moq;
using Users.Application.DTO;
using Users.Application.Services;
using Users.Contracts;
using Users.Contracts.Repositories;
using Users.Domain;

namespace CarServiceTrack.Tests.Unit.Services;

public class OwnerServiceTests
{
    private readonly Mock<IUsersUnitOfWork> _uow = new();
    private readonly Mock<IOwnerRepository> _repo = new();
    private readonly OwnerService _sut;

    public OwnerServiceTests()
    {
        _uow.Setup(u => u.Owners).Returns(_repo.Object);
        _sut = new OwnerService(_uow.Object);
    }

    [Fact]
    public async Task FindByUserAsync_WhenOwnerExists_ReturnsMapped()
    {
        var userId = Guid.NewGuid();
        var owner = MakeOwner(userId);
        _repo.Setup(r => r.FindByUserAsync(userId)).ReturnsAsync(owner);

        var result = await _sut.FindByUserAsync(userId);

        result.Should().NotBeNull();
        result!.AppUserId.Should().Be(userId);
    }

    [Fact]
    public async Task FindByUserAsync_WhenOwnerDoesNotExist_ReturnsNull()
    {
        _repo.Setup(r => r.FindByUserAsync(It.IsAny<Guid>())).ReturnsAsync((Owner?)null);

        var result = await _sut.FindByUserAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task EnsureForUserAsync_WhenOwnerAlreadyExists_ReturnsExistingOwner()
    {
        var userId = Guid.NewGuid();
        var existing = MakeOwner(userId);
        _repo.Setup(r => r.FindByUserAsync(userId)).ReturnsAsync(existing);

        var result = await _sut.EnsureForUserAsync(userId, "user@test.com");

        result.Should().NotBeNull();
        result.AppUserId.Should().Be(userId);
        _repo.Verify(r => r.Add(It.IsAny<Owner>()), Times.Never);
    }

    [Fact]
    public async Task EnsureForUserAsync_WhenOwnerDoesNotExist_CreatesAndStagesNewOwner()
    {
        var userId = Guid.NewGuid();
        _repo.Setup(r => r.FindByUserAsync(userId)).ReturnsAsync((Owner?)null);
        _repo.Setup(r => r.Add(It.IsAny<Owner>())).Returns<Owner>(o => o);

        var result = await _sut.EnsureForUserAsync(userId, "user@test.com");

        result.Should().NotBeNull();
        result.AppUserId.Should().Be(userId);
        result.FirstName.Should().Be("user@test.com");
        _repo.Verify(r => r.Add(It.Is<Owner>(o => o.AppUserId == userId && o.FirstName == "user@test.com")), Times.Once);
    }

    [Fact]
    public async Task FindAsync_WithExistingId_ReturnsMapped()
    {
        var id = Guid.NewGuid();
        var owner = MakeOwner(Guid.NewGuid(), id);
        _repo.Setup(r => r.FindAsync(id)).ReturnsAsync(owner);

        var result = await _sut.FindAsync(id);

        result.Should().NotBeNull();
    }

    [Fact]
    public void Add_WithValidEntity_StagedAndReturnsMapped()
    {
        var dto = new BllOwner { Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe", AppUserId = Guid.NewGuid() };
        _repo.Setup(r => r.Add(It.IsAny<Owner>())).Returns(MakeOwner(dto.AppUserId, dto.Id));

        var result = _sut.Add(dto);

        result.FirstName.Should().Be("John");
        _repo.Verify(r => r.Add(It.IsAny<Owner>()), Times.Once);
    }

    [Fact]
    public void Remove_DelegatesToRepository()
    {
        _sut.Remove(new BllOwner { Id = Guid.NewGuid(), FirstName = "X", LastName = "Y", AppUserId = Guid.NewGuid() });
        _repo.Verify(r => r.Remove(It.IsAny<Owner>()), Times.Once);
    }

    private static Owner MakeOwner(Guid appUserId, Guid? id = null) =>
        new() { Id = id ?? Guid.NewGuid(), FirstName = "John", LastName = "Doe", AppUserId = appUserId };
}
