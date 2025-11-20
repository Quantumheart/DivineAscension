using System.Diagnostics.CodeAnalysis;
using Moq;
using PantheonWars.Models;
using PantheonWars.Models.Enum;
using PantheonWars.Systems.BuffSystem.Interfaces;
using Vintagestory.API.Server;
using Xunit;

namespace PantheonWars.Tests.Models;

/// <summary>
///     Unit tests for Ability abstract class
/// </summary>
[ExcludeFromCodeCoverage]
public class AbilityTests
{
    /// <summary>
    ///     Concrete test implementation of Ability for testing
    /// </summary>
    private class TestAbility : Ability
    {
        public bool ExecuteCalled { get; private set; }
        public bool ShouldExecuteSucceed { get; set; } = true;

        public TestAbility(string id, string name, string description, DeityType deity, AbilityType type)
            : base(id, name, description, deity, type)
        {
        }

        public override bool Execute(IServerPlayer caster, ICoreServerAPI sapi, IBuffManager? buffManager = null)
        {
            ExecuteCalled = true;
            return ShouldExecuteSucceed;
        }

        // Expose protected properties for testing
        public void SetCooldown(float cooldown) => CooldownSeconds = cooldown;
        public void SetFavorCost(int cost) => FavorCost = cost;
        public void SetMinimumRank(DevotionRank rank) => MinimumRank = rank;
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_InitializesProperties()
    {
        // Act
        var ability = new TestAbility(
            "test_ability",
            "Test Ability",
            "Test Description",
            DeityType.Aethra,
            AbilityType.Buff
        );

        // Assert
        Assert.Equal("test_ability", ability.Id);
        Assert.Equal("Test Ability", ability.Name);
        Assert.Equal("Test Description", ability.Description);
        Assert.Equal(DeityType.Aethra, ability.Deity);
        Assert.Equal(AbilityType.Buff, ability.Type);
        Assert.Equal(DevotionRank.Initiate, ability.MinimumRank); // Default value
        Assert.Equal(0f, ability.CooldownSeconds); // Default value
        Assert.Equal(0, ability.FavorCost); // Default value
    }

    [Fact]
    public void Constructor_WithDifferentDeity_SetsCorrectly()
    {
        // Act
        var ability = new TestAbility(
            "lysa_ability",
            "Lysa's Hunt",
            "Description",
            DeityType.Gaia,
            AbilityType.Debuff
        );

        // Assert
        Assert.Equal(DeityType.Gaia, ability.Deity);
        Assert.Equal(AbilityType.Debuff, ability.Type);
    }

    #endregion

    #region Property Tests

    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        // Arrange
        var ability = new TestAbility(
            "test_ability",
            "Test Ability",
            "Test Description",
            DeityType.Aethra,
            AbilityType.Buff
        );

        // Act
        ability.SetCooldown(30.5f);
        ability.SetFavorCost(50);
        ability.SetMinimumRank(DevotionRank.Zealot);

        // Assert
        Assert.Equal(30.5f, ability.CooldownSeconds);
        Assert.Equal(50, ability.FavorCost);
        Assert.Equal(DevotionRank.Zealot, ability.MinimumRank);
    }

    #endregion

    #region Execute Tests

    [Fact]
    public void Execute_CallsConcreteImplementation()
    {
        // Arrange
        var ability = new TestAbility(
            "test_ability",
            "Test",
            "Desc",
            DeityType.Aethra,
            AbilityType.Buff
        );
        var mockPlayer = new Mock<IServerPlayer>();
        var mockAPI = new Mock<ICoreServerAPI>();

        // Act
        var result = ability.Execute(mockPlayer.Object, mockAPI.Object);

        // Assert
        Assert.True(ability.ExecuteCalled);
        Assert.True(result);
    }

    [Fact]
    public void Execute_WithBuffManager_PassesThrough()
    {
        // Arrange
        var ability = new TestAbility(
            "test_ability",
            "Test",
            "Desc",
            DeityType.Aethra,
            AbilityType.Buff
        );
        var mockPlayer = new Mock<IServerPlayer>();
        var mockAPI = new Mock<ICoreServerAPI>();
        var mockBuffManager = new Mock<IBuffManager>();

        // Act
        var result = ability.Execute(mockPlayer.Object, mockAPI.Object, mockBuffManager.Object);

        // Assert
        Assert.True(ability.ExecuteCalled);
        Assert.True(result);
    }

    [Fact]
    public void Execute_WhenFails_ReturnsFalse()
    {
        // Arrange
        var ability = new TestAbility(
            "test_ability",
            "Test",
            "Desc",
            DeityType.Aethra,
            AbilityType.Buff
        )
        {
            ShouldExecuteSucceed = false
        };
        var mockPlayer = new Mock<IServerPlayer>();
        var mockAPI = new Mock<ICoreServerAPI>();

        // Act
        var result = ability.Execute(mockPlayer.Object, mockAPI.Object);

        // Assert
        Assert.True(ability.ExecuteCalled);
        Assert.False(result);
    }

    #endregion

    #region CanExecute Tests

    [Fact]
    public void CanExecute_DefaultImplementation_ReturnsTrue()
    {
        // Arrange
        var ability = new TestAbility(
            "test_ability",
            "Test",
            "Desc",
            DeityType.Aethra,
            AbilityType.Buff
        );
        var mockPlayer = new Mock<IServerPlayer>();
        var mockAPI = new Mock<ICoreServerAPI>();

        // Act
        var result = ability.CanExecute(mockPlayer.Object, mockAPI.Object, out var failureReason);

        // Assert
        Assert.True(result);
        Assert.Equal(string.Empty, failureReason);
    }

    [Fact]
    public void CanExecute_CustomImplementation_CanReturnFalse()
    {
        // Arrange - Create a custom ability that overrides CanExecute
        var customAbility = new CustomValidationAbility(
            "custom_ability",
            "Custom",
            "Desc",
            DeityType.Gaia,
            AbilityType.Buff
        )
        {
            ShouldValidationPass = false,
            ValidationFailureMessage = "Not enough resources"
        };
        var mockPlayer = new Mock<IServerPlayer>();
        var mockAPI = new Mock<ICoreServerAPI>();

        // Act
        var result = customAbility.CanExecute(mockPlayer.Object, mockAPI.Object, out var failureReason);

        // Assert
        Assert.False(result);
        Assert.Equal("Not enough resources", failureReason);
    }

    /// <summary>
    ///     Custom ability with validation logic for testing
    /// </summary>
    private class CustomValidationAbility : Ability
    {
        public bool ShouldValidationPass { get; set; } = true;
        public string ValidationFailureMessage { get; set; } = string.Empty;

        public CustomValidationAbility(string id, string name, string description, DeityType deity, AbilityType type)
            : base(id, name, description, deity, type)
        {
        }

        public override bool Execute(IServerPlayer caster, ICoreServerAPI sapi, IBuffManager? buffManager = null)
        {
            return true;
        }

        public override bool CanExecute(IServerPlayer caster, ICoreServerAPI sapi, out string failureReason)
        {
            if (!ShouldValidationPass)
            {
                failureReason = ValidationFailureMessage;
                return false;
            }

            failureReason = string.Empty;
            return true;
        }
    }

    #endregion

    #region Enum Tests

    [Fact]
    public void AbilityType_HasExpectedValues()
    {
        // Arrange & Act & Assert
        Assert.Equal(0, (int)AbilityType.Buff);
        Assert.Equal(1, (int)AbilityType.Debuff);
    }

    [Fact]
    public void DevotionRank_HasExpectedValues()
    {
        // Arrange & Act & Assert
        Assert.Equal(0, (int)DevotionRank.Initiate);
        Assert.Equal(1, (int)DevotionRank.Disciple);
        Assert.Equal(2, (int)DevotionRank.Zealot);
        Assert.Equal(3, (int)DevotionRank.Champion);
        Assert.Equal(4, (int)DevotionRank.Avatar);
    }

    #endregion
}
