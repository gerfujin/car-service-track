using FluentAssertions;
using Moq;
using Users.Application.Services;
using Users.Contracts;
using Users.Contracts.Repositories;
using Users.Domain.Identity;

namespace CarServiceTrack.Tests.Unit.Services;

public class RefreshTokenServiceTests
{
    private readonly Mock<IUsersUnitOfWork> _uow = new();
    private readonly Mock<IRefreshTokenRepository> _repo = new();
    private readonly RefreshTokenService _sut;

    public RefreshTokenServiceTests()
    {
        _uow.Setup(u => u.RefreshTokens).Returns(_repo.Object);
        _sut = new RefreshTokenService(_uow.Object);
    }

    // ── RemoveExpiredForUserAsync ──────────────────────────────────────────────
    [Fact]
    public async Task RemoveExpiredForUserAsync_DelegatesToRepositoryWithCurrentTime()
    {
        var userId = Guid.NewGuid();
        _repo.Setup(r => r.RemoveExpiredForUserAsync(userId, It.IsAny<DateTime>())).ReturnsAsync((int?)3);

        var result = await _sut.RemoveExpiredForUserAsync(userId);

        result.Should().Be(3);
        _repo.Verify(r => r.RemoveExpiredForUserAsync(userId, It.IsAny<DateTime>()), Times.Once);
    }

    // ── AddForUserAsync ───────────────────────────────────────────────────────
    [Fact]
    public async Task AddForUserAsync_StagedTokenAndReturnsRefreshTokenString()
    {
        var userId = Guid.NewGuid();
        var token = new AppRefreshToken { AppUserId = userId };
        _repo.Setup(r => r.Add(It.IsAny<AppRefreshToken>())).Returns(token);

        var result = await _sut.AddForUserAsync(userId);

        result.Should().NotBeNullOrWhiteSpace();
        _repo.Verify(r => r.Add(It.Is<AppRefreshToken>(t => t.AppUserId == userId)), Times.Once);
    }

    // ── RotateForRefreshAsync ─────────────────────────────────────────────────
    [Fact]
    public async Task RotateForRefreshAsync_WithExactlyOneMatchingToken_RotatesAndReturnsNewToken()
    {
        var userId = Guid.NewGuid();
        var oldToken = "old-refresh-token-value";
        var tokenEntity = new AppRefreshToken
        {
            AppUserId = userId,
            RefreshToken = oldToken,
            ExpirationDT = DateTime.UtcNow.AddDays(7)
        };
        _repo.Setup(r => r.FindValidForRefreshAsync(userId, oldToken, It.IsAny<DateTime>()))
            .ReturnsAsync(new List<AppRefreshToken> { tokenEntity });

        var result = await _sut.RotateForRefreshAsync(userId, oldToken);

        result.MatchingTokenCount.Should().Be(1);
        result.Rotated.Should().BeTrue();
        result.RefreshToken.Should().NotBe(oldToken);
        tokenEntity.PreviousRefreshToken.Should().Be(oldToken);
    }

    [Fact]
    public async Task RotateForRefreshAsync_WhenNoTokenFound_ReturnsZeroCount()
    {
        var userId = Guid.NewGuid();
        _repo.Setup(r => r.FindValidForRefreshAsync(userId, It.IsAny<string>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<AppRefreshToken>());

        var result = await _sut.RotateForRefreshAsync(userId, "invalid-token");

        result.MatchingTokenCount.Should().Be(0);
        result.Rotated.Should().BeFalse();
        result.EmptyCollectionCountText.Should().Be("");
    }

    [Fact]
    public async Task RotateForRefreshAsync_WhenMultipleTokensFound_ReturnsCountAndNoRotation()
    {
        var userId = Guid.NewGuid();
        _repo.Setup(r => r.FindValidForRefreshAsync(userId, It.IsAny<string>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<AppRefreshToken>
            {
                new() { AppUserId = userId },
                new() { AppUserId = userId }
            });

        var result = await _sut.RotateForRefreshAsync(userId, "token");

        result.MatchingTokenCount.Should().Be(2);
        result.Rotated.Should().BeFalse();
    }

    // ── RemoveForLogoutAsync ──────────────────────────────────────────────────
    [Fact]
    public async Task RemoveForLogoutAsync_DelegatesToRepository()
    {
        var userId = Guid.NewGuid();
        var refreshToken = "token-to-remove";
        _repo.Setup(r => r.RemoveForLogoutAsync(userId, refreshToken)).Returns(Task.CompletedTask);

        await _sut.RemoveForLogoutAsync(userId, refreshToken);

        _repo.Verify(r => r.RemoveForLogoutAsync(userId, refreshToken), Times.Once);
    }
}
