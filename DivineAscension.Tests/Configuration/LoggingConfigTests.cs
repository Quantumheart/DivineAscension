using System.Diagnostics.CodeAnalysis;
using DivineAscension.Configuration;

namespace DivineAscension.Tests.Configuration;

[ExcludeFromCodeCoverage]
public class LoggingConfigTests
{
    [Fact]
    public void GameBalanceConfig_DefaultsToAllLevelsEnabled()
    {
        // No-config / legacy worlds must keep full logging.
        var config = new GameBalanceConfig();

        Assert.True(config.EnableDebugLogs);
        Assert.True(config.EnableNotificationLogs);
        Assert.True(config.EnableWarningLogs);
        Assert.True(config.EnableErrorLogs);
    }

    [Fact]
    public void BuildLoggingConfig_MapsTogglesToLevels()
    {
        var config = new GameBalanceConfig
        {
            EnableDebugLogs = false,
            EnableNotificationLogs = false,
            EnableWarningLogs = true,
            EnableErrorLogs = true
        };

        var logging = config.BuildLoggingConfig();

        Assert.False(logging.EnableDebug);
        Assert.False(logging.EnableNotification);
        Assert.True(logging.EnableWarning);
        Assert.True(logging.EnableError);
    }

    [Fact]
    public void BuildLoggingConfig_AllTogglesOff_IsCompletelySilent()
    {
        var config = new GameBalanceConfig
        {
            EnableDebugLogs = false,
            EnableNotificationLogs = false,
            EnableWarningLogs = false,
            EnableErrorLogs = false
        };

        var logging = config.BuildLoggingConfig();

        Assert.False(logging.EnableDebug);
        Assert.False(logging.EnableNotification);
        Assert.False(logging.EnableWarning);
        Assert.False(logging.EnableError);
        // Event/Build/Chat follow the notification toggle, so silence is total.
        Assert.False(logging.EnableEvent);
        Assert.False(logging.EnableBuild);
        Assert.False(logging.EnableChat);
    }

    [Fact]
    public void BuildLoggingConfig_IsIndependentOfBalanceValidation()
    {
        // A balance-invalid config (out-of-order favor thresholds) must still produce the
        // requested logging levels — OnConfigChanged applies logging before Validate() so a bad
        // balance value saved in the same batch cannot leave the logs un-silenced (#620 regression).
        var config = new GameBalanceConfig
        {
            DiscipleThreshold = 999999, // breaks the ascending-threshold rule
            EnableDebugLogs = false
        };

        Assert.Throws<System.InvalidOperationException>(() => config.Validate());
        Assert.False(config.BuildLoggingConfig().EnableDebug);
    }

    [Fact]
    public void CopyFrom_OverwritesLevelTogglesInPlace()
    {
        var target = LoggingConfig.Default();

        target.CopyFrom(LoggingConfig.Silent());

        Assert.False(target.EnableError);
        Assert.False(target.EnableNotification);
    }

    [Fact]
    public void CopyFrom_CopiesCategoryFiltersWithoutSharingReferences()
    {
        var source = LoggingConfig.Default();
        source.ExcludedCategories.Add("ReligionManager");
        var target = LoggingConfig.Default();

        target.CopyFrom(source);

        Assert.Contains("ReligionManager", target.ExcludedCategories);
        // Mutating the source afterwards must not bleed into the target.
        source.ExcludedCategories.Add("FavorSystem");
        Assert.DoesNotContain("FavorSystem", target.ExcludedCategories);
    }
}
