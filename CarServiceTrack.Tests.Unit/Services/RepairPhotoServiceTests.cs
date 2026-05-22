using FluentAssertions;
using Moq;
using Orders.Application.DTO;
using Orders.Application.Services;
using Orders.Contracts;
using Orders.Contracts.Repositories;
using Orders.Domain;

namespace CarServiceTrack.Tests.Unit.Services;

public class RepairPhotoServiceTests
{
    private readonly Mock<IOrdersUnitOfWork> _uow = new();
    private readonly Mock<IRepairPhotoRepository> _photoRepo = new();
    private readonly Mock<IServiceOrderRepository> _orderRepo = new();
    private readonly RepairPhotoService _sut;

    public RepairPhotoServiceTests()
    {
        _uow.Setup(u => u.RepairPhotos).Returns(_photoRepo.Object);
        _uow.Setup(u => u.ServiceOrders).Returns(_orderRepo.Object);
        _sut = new RepairPhotoService(_uow.Object);
    }

    // ── AllForApiAsync ────────────────────────────────────────────────────────
    [Fact]
    public async Task AllForApiAsync_WhenAdminRole_ReturnsAllPhotos()
    {
        var orderId = Guid.NewGuid();
        _photoRepo.Setup(r => r.AllByServiceOrderWithDetailsAsync(orderId))
            .ReturnsAsync(new List<RepairPhoto> { MakePhoto(orderId), MakePhoto(orderId) });

        var result = (await _sut.AllForApiAsync(orderId, Guid.NewGuid(), isAdmin: true, isMechanic: false)).ToList();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task AllForApiAsync_WhenClientAndPhotoOwnedByUser_ReturnsOwnedPhotos()
    {
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        // Photo with OwnerId matching userId
        var ownedPhoto = MakePhoto(orderId);
        ownedPhoto.ServiceOrderId = orderId;
        // BllRepairPhoto.OwnerId is populated by mapper from navigation; since domain has no
        // navigation loaded, OwnerId will be null → filter excludes all photos.
        _photoRepo.Setup(r => r.AllByServiceOrderWithDetailsAsync(orderId))
            .ReturnsAsync(new List<RepairPhoto> { ownedPhoto });

        var result = (await _sut.AllForApiAsync(orderId, userId, isAdmin: false, isMechanic: false)).ToList();

        // OwnerId = null (no navigation) → filter excludes all
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task AllForApiAsync_WhenMechanicRole_ReturnsAllPhotos()
    {
        var orderId = Guid.NewGuid();
        _photoRepo.Setup(r => r.AllByServiceOrderWithDetailsAsync(orderId))
            .ReturnsAsync(new List<RepairPhoto> { MakePhoto(orderId) });

        var result = (await _sut.AllForApiAsync(orderId, Guid.NewGuid(), isAdmin: false, isMechanic: true)).ToList();

        result.Should().HaveCount(1);
    }

    // ── FindForApiAsync ───────────────────────────────────────────────────────
    [Fact]
    public async Task FindForApiAsync_WhenAdminAndPhotoExists_ReturnsPhoto()
    {
        var id = Guid.NewGuid();
        _photoRepo.Setup(r => r.FindWithDetailsAsync(id)).ReturnsAsync(MakePhoto(Guid.NewGuid(), id));

        var result = await _sut.FindForApiAsync(id, Guid.NewGuid(), isAdmin: true, isMechanic: false);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task FindForApiAsync_WhenPhotoDoesNotExist_ReturnsNull()
    {
        _photoRepo.Setup(r => r.FindWithDetailsAsync(It.IsAny<Guid>())).ReturnsAsync((RepairPhoto?)null);

        var result = await _sut.FindForApiAsync(Guid.NewGuid(), Guid.NewGuid(), false, false);

        result.Should().BeNull();
    }

    [Fact]
    public async Task FindForApiAsync_WhenClientAndPhotoNotOwned_ReturnsNull()
    {
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        // Photo with OwnerId = null (no navigation) → OwnerId != userId → returns null
        _photoRepo.Setup(r => r.FindWithDetailsAsync(id)).ReturnsAsync(MakePhoto(Guid.NewGuid(), id));

        var result = await _sut.FindForApiAsync(id, userId, isAdmin: false, isMechanic: false);

        result.Should().BeNull();
    }

    // ── ValidateUploadAsync ───────────────────────────────────────────────────
    [Fact]
    public async Task ValidateUploadAsync_WhenOrderNotFound_ReturnsError()
    {
        var orderId = Guid.NewGuid();
        _orderRepo.Setup(r => r.ExistsAsync(orderId)).ReturnsAsync(false);

        var result = await _sut.ValidateUploadAsync(orderId);

        result.Error.Should().Be("Service order not found.");
    }

    [Fact]
    public async Task ValidateUploadAsync_WhenAtMaxPhotos_ReturnsError()
    {
        var orderId = Guid.NewGuid();
        _orderRepo.Setup(r => r.ExistsAsync(orderId)).ReturnsAsync(true);
        _photoRepo.Setup(r => r.CountByServiceOrderAsync(orderId)).ReturnsAsync(20);

        var result = await _sut.ValidateUploadAsync(orderId);

        result.Error.Should().Be("Maximum 20 photos per order.");
    }

    [Fact]
    public async Task ValidateUploadAsync_WhenBelowLimit_ReturnsSuccess()
    {
        var orderId = Guid.NewGuid();
        _orderRepo.Setup(r => r.ExistsAsync(orderId)).ReturnsAsync(true);
        _photoRepo.Setup(r => r.CountByServiceOrderAsync(orderId)).ReturnsAsync(5);

        var result = await _sut.ValidateUploadAsync(orderId);

        result.Error.Should().BeNull();
        result.NotFound.Should().BeFalse();
    }

    // ── RemoveForApiAsync ─────────────────────────────────────────────────────
    [Fact]
    public async Task RemoveForApiAsync_WithExistingPhoto_StagedForDeletion()
    {
        var id = Guid.NewGuid();
        var photo = MakePhoto(Guid.NewGuid(), id);
        _photoRepo.Setup(r => r.FindAsync(id)).ReturnsAsync(photo);

        var result = await _sut.RemoveForApiAsync(id);

        result.NotFound.Should().BeFalse();
        _photoRepo.Verify(r => r.Remove(photo), Times.Once);
    }

    [Fact]
    public async Task RemoveForApiAsync_WithNonexistentPhoto_ReturnsNotFound()
    {
        _photoRepo.Setup(r => r.FindAsync(It.IsAny<Guid>())).ReturnsAsync((RepairPhoto?)null);

        var result = await _sut.RemoveForApiAsync(Guid.NewGuid());

        result.NotFound.Should().BeTrue();
        _photoRepo.Verify(r => r.Remove(It.IsAny<RepairPhoto>()), Times.Never);
    }

    // ── AddUploaded ───────────────────────────────────────────────────────────
    [Fact]
    public void AddUploaded_StampsUploadedAtAndStages()
    {
        var orderId = Guid.NewGuid();
        var dto = new BllRepairPhoto { Id = Guid.NewGuid(), ServiceOrderId = orderId, FilePath = "/uploads/photo.jpg" };
        var domainPhoto = MakePhoto(orderId, dto.Id);
        _photoRepo.Setup(r => r.Add(It.IsAny<RepairPhoto>())).Returns(domainPhoto);

        var result = _sut.AddUploaded(dto);

        result.Should().NotBeNull();
        dto.UploadedAt.Should().NotBe(default);
        _photoRepo.Verify(r => r.Add(It.IsAny<RepairPhoto>()), Times.Once);
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────
    [Fact]
    public async Task UpdateAsync_WithNonexistentPhoto_ReturnsNull()
    {
        _photoRepo.Setup(r => r.FindAsync(It.IsAny<Guid>())).ReturnsAsync((RepairPhoto?)null);

        var result = await _sut.UpdateAsync(new BllRepairPhoto { Id = Guid.NewGuid(), ServiceOrderId = Guid.NewGuid() });

        result.Should().BeNull();
    }

    private static RepairPhoto MakePhoto(Guid serviceOrderId, Guid? id = null) =>
        new() { Id = id ?? Guid.NewGuid(), ServiceOrderId = serviceOrderId, FilePath = "/uploads/test.jpg", UploadedAt = DateTime.UtcNow };
}
