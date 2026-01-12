using DivineAscension.Constants;
using DivineAscension.Services;
using DivineAscension.Tests.Helpers;

namespace DivineAscension.Tests.Services;

public class LocalizationServiceTests
{
    public LocalizationServiceTests()
    {
        TestFixtures.InitializeLocalizationForTests();
    }

    #region Singleton Tests

    [Fact]
    public void Instance_AlwaysReturnsSameInstance()
    {
        // Act
        var instance1 = LocalizationService.Instance;
        var instance2 = LocalizationService.Instance;

        // Assert
        Assert.Same(instance1, instance2);
    }

    #endregion

    #region Basic Get Tests

    [Fact]
    public void Get_WithValidKey_ReturnsTranslation()
    {
        // Act
        var result = LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_NO_RELIGION);

        // Assert - should return translation, not the key
        Assert.NotEqual(LocalizationKeys.CMD_ERROR_NO_RELIGION, result);
        Assert.Contains("religion", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Get_WithMissingKey_ReturnsKey()
    {
        // Act
        var result = LocalizationService.Instance.Get("nonexistent.key");

        // Assert
        Assert.Equal("nonexistent.key", result);
    }

    [Fact]
    public void Get_WithNullKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => LocalizationService.Instance.Get(null!));
    }

    [Fact]
    public void Get_WithEmptyKey_ReturnsEmptyKey()
    {
        // Act
        var result = LocalizationService.Instance.Get(string.Empty);

        // Assert - empty key returns itself (fallback behavior)
        Assert.Equal(string.Empty, result);
    }

    #endregion

    #region Parameter Substitution Tests

    [Fact]
    public void Get_WithParameterSubstitution_FormatsCorrectly()
    {
        // Arrange - use a key that has parameters
        var testKey = LocalizationKeys.CMD_SUCCESS_BLESSING_UNLOCKED;

        // Act
        var result = LocalizationService.Instance.Get(testKey, "TestBlessing");

        // Assert - should contain the parameter value
        Assert.Contains("TestBlessing", result);
    }

    [Fact]
    public void Get_WithMultipleParameters_FormatsAllCorrectly()
    {
        // Arrange - use a key with multiple parameters
        var testKey = LocalizationKeys.NET_RELIGION_ROLE_CHANGED;

        // Act
        var result = LocalizationService.Instance.Get(testKey, "TestReligion", "TestRole");

        // Assert - should contain both parameter values
        Assert.Contains("TestReligion", result);
        Assert.Contains("TestRole", result);
    }

    [Fact]
    public void Get_WithMissingParameters_DoesNotThrow()
    {
        // Arrange - use a key with parameters but don't provide them
        var testKey = LocalizationKeys.CMD_SUCCESS_BLESSING_UNLOCKED;

        // Act & Assert - should not throw
        var exception = Record.Exception(() => LocalizationService.Instance.Get(testKey));
        Assert.Null(exception);
    }

    [Fact]
    public void Get_WithExtraParameters_DoesNotThrow()
    {
        // Arrange - provide more parameters than needed
        var testKey = LocalizationKeys.CMD_ERROR_NO_RELIGION;

        // Act & Assert - should not throw
        var exception = Record.Exception(() =>
            LocalizationService.Instance.Get(testKey, "extra1", "extra2", "extra3"));
        Assert.Null(exception);
    }

    #endregion

    #region HasKey Tests

    [Fact]
    public void HasKey_WithValidKey_ReturnsTrue()
    {
        // Act
        var result = LocalizationService.Instance.HasKey(LocalizationKeys.CMD_ERROR_NO_RELIGION);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasKey_WithMissingKey_ReturnsFalse()
    {
        // Act
        var result = LocalizationService.Instance.HasKey("nonexistent.key.that.does.not.exist");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasKey_WithNullKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => LocalizationService.Instance.HasKey(null!));
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public void Get_ConcurrentAccess_DoesNotThrow()
    {
        // Arrange
        var keys = new[]
        {
            LocalizationKeys.CMD_ERROR_NO_RELIGION,
            LocalizationKeys.UI_TAB_RELIGION,
            LocalizationKeys.UI_TAB_BLESSINGS,
            LocalizationKeys.DOMAIN_CRAFT_NAME,
            LocalizationKeys.RANK_FAVOR_INITIATE
        };

        // Act & Assert - run many concurrent accesses
        var exception = Record.Exception(() =>
        {
            Parallel.For(0, 1000, i =>
            {
                var key = keys[i % keys.Length];
                var result = LocalizationService.Instance.Get(key);
                Assert.NotNull(result);
            });
        });

        Assert.Null(exception);
    }

    [Fact]
    public void Get_ConcurrentAccessWithParameters_DoesNotThrow()
    {
        // Arrange
        var testKey = LocalizationKeys.CMD_SUCCESS_BLESSING_UNLOCKED;

        // Act & Assert - run many concurrent accesses with parameters
        var exception = Record.Exception(() =>
        {
            Parallel.For(0, 1000, i =>
            {
                var result = LocalizationService.Instance.Get(testKey, $"Blessing{i}");
                Assert.Contains($"Blessing{i}", result);
            });
        });

        Assert.Null(exception);
    }

    #endregion

    #region Coverage Tests for Common Keys

    [Theory]
    [InlineData(LocalizationKeys.UI_TAB_RELIGION)]
    [InlineData(LocalizationKeys.UI_TAB_BLESSINGS)]
    [InlineData(LocalizationKeys.UI_TAB_CIVILIZATION)]
    [InlineData(LocalizationKeys.UI_COMMON_CONFIRM)]
    [InlineData(LocalizationKeys.UI_COMMON_CANCEL)]
    [InlineData(LocalizationKeys.UI_COMMON_UNKNOWN)]
    [InlineData(LocalizationKeys.UI_COMMON_NEVER)]
    public void Get_CommonUIKeys_ReturnValidTranslations(string key)
    {
        // Act
        var result = LocalizationService.Instance.Get(key);

        // Assert
        Assert.NotEqual(key, result); // Should not return the key itself
        Assert.False(string.IsNullOrEmpty(result));
    }

    [Theory]
    [InlineData(LocalizationKeys.DOMAIN_CRAFT_NAME)]
    [InlineData(LocalizationKeys.DOMAIN_WILD_NAME)]
    [InlineData(LocalizationKeys.DOMAIN_HARVEST_NAME)]
    [InlineData(LocalizationKeys.DOMAIN_STONE_NAME)]
    public void Get_DeityKeys_ReturnValidTranslations(string key)
    {
        // Act
        var result = LocalizationService.Instance.Get(key);

        // Assert
        Assert.NotEqual(key, result);
        Assert.False(string.IsNullOrEmpty(result));
    }

    [Theory]
    [InlineData(LocalizationKeys.RANK_FAVOR_INITIATE)]
    [InlineData(LocalizationKeys.RANK_FAVOR_DISCIPLE)]
    [InlineData(LocalizationKeys.RANK_FAVOR_ZEALOT)]
    [InlineData(LocalizationKeys.RANK_FAVOR_CHAMPION)]
    [InlineData(LocalizationKeys.RANK_FAVOR_AVATAR)]
    public void Get_FavorRankKeys_ReturnValidTranslations(string key)
    {
        // Act
        var result = LocalizationService.Instance.Get(key);

        // Assert
        Assert.NotEqual(key, result);
        Assert.False(string.IsNullOrEmpty(result));
    }

    [Theory]
    [InlineData(LocalizationKeys.RANK_PRESTIGE_FLEDGLING)]
    [InlineData(LocalizationKeys.RANK_PRESTIGE_ESTABLISHED)]
    [InlineData(LocalizationKeys.RANK_PRESTIGE_RENOWNED)]
    [InlineData(LocalizationKeys.RANK_PRESTIGE_LEGENDARY)]
    [InlineData(LocalizationKeys.RANK_PRESTIGE_MYTHIC)]
    public void Get_PrestigeRankKeys_ReturnValidTranslations(string key)
    {
        // Act
        var result = LocalizationService.Instance.Get(key);

        // Assert
        Assert.NotEqual(key, result);
        Assert.False(string.IsNullOrEmpty(result));
    }

    #endregion
}