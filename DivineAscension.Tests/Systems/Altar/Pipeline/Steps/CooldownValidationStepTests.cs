using DivineAscension.Systems.Altar.Pipeline;
using DivineAscension.Systems.Altar.Pipeline.Steps;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Tests.Helpers;
using Moq;
using Vintagestory.API.MathTools;

namespace DivineAscension.Tests.Systems.Altar.Pipeline.Steps;

public class CooldownValidationStepTests
{
    private readonly Mock<IPlayerProgressionDataManager> _progressionDataManager;
    private readonly CooldownValidationStep _step;

    public CooldownValidationStepTests()
    {
        TestFixtures.InitializeLocalizationForTests();
        _progressionDataManager = new Mock<IPlayerProgressionDataManager>();
        _step = new CooldownValidationStep(_progressionDataManager.Object);
    }

    private static PrayerContext CreateContext(long currentTime = 0) =>
        new()
        {
            PlayerUID = "player1",
            PlayerName = "TestPlayer",
            AltarPosition = new BlockPos(100, 50, 100),
            Offering = null,
            CurrentTime = currentTime,
            Player = null!
        };

    [Fact]
    public void Name_ReturnsCorrectValue()
    {
        Assert.Equal("CooldownValidation", _step.Name);
    }

    [Fact]
    public void Execute_NoCooldown_Continues()
    {
        // Arrange
        var context = CreateContext();
        _progressionDataManager.Setup(x => x.GetPrayerCooldownExpiryUtc("player1"))
            .Returns((DateTime?)null);

        // Act
        _step.Execute(context);

        // Assert
        Assert.False(context.IsComplete);
    }

    [Fact]
    public void Execute_CooldownExpired_Continues()
    {
        // Arrange - cooldown expired 5 minutes ago
        var context = CreateContext();
        _progressionDataManager.Setup(x => x.GetPrayerCooldownExpiryUtc("player1"))
            .Returns(DateTime.UtcNow.AddMinutes(-5));

        // Act
        _step.Execute(context);

        // Assert
        Assert.False(context.IsComplete);
    }

    [Fact]
    public void Execute_OnCooldown_SetsFailureAndCompletes()
    {
        // Arrange - 30 minutes remaining
        var context = CreateContext();
        _progressionDataManager.Setup(x => x.GetPrayerCooldownExpiryUtc("player1"))
            .Returns(DateTime.UtcNow.AddMinutes(30));

        // Act
        _step.Execute(context);

        // Assert
        Assert.False(context.Success);
        Assert.True(context.IsComplete);
        Assert.Equal("You must wait 30 more minute(s) before praying again.", context.Message);
    }

    [Fact]
    public void Execute_CooldownNearExpiry_RoundsCorrectly()
    {
        // Arrange - 59.7 minutes remaining (slightly more than 59.5 to account for test execution time)
        // This ensures it rounds to 60 even if a few ms pass between setup and execution
        var context = CreateContext();
        _progressionDataManager.Setup(x => x.GetPrayerCooldownExpiryUtc("player1"))
            .Returns(DateTime.UtcNow.AddMinutes(59.7));

        // Act
        _step.Execute(context);

        // Assert
        Assert.False(context.Success);
        Assert.True(context.IsComplete);
        Assert.Equal("You must wait 60 more minute(s) before praying again.", context.Message);
    }

    [Fact]
    public void Execute_CooldownLessThanOneMinute_ShowsOneMinute()
    {
        // Arrange - 15 seconds remaining
        var context = CreateContext();
        _progressionDataManager.Setup(x => x.GetPrayerCooldownExpiryUtc("player1"))
            .Returns(DateTime.UtcNow.AddSeconds(15));

        // Act
        _step.Execute(context);

        // Assert
        Assert.False(context.Success);
        Assert.True(context.IsComplete);
        Assert.Equal("You must wait 1 more minute(s) before praying again.", context.Message);
    }
}