using System.Diagnostics.CodeAnalysis;
using DivineAscension.Configuration;

namespace DivineAscension.Tests.Configuration;

/// <summary>
///     CopyFrom resets values in place without replacing the instance (#620). ConfigLib holds a
///     reference to the registered config and writes GUI changes into it; the validation-failure
///     fallback must reset that same object, not swap it, or every later config change is stranded.
/// </summary>
[ExcludeFromCodeCoverage]
public class GameBalanceConfigResetTests
{
    [Fact]
    public void CopyFrom_ResetsValuesInPlaceForExistingReferenceHolders()
    {
        var config = new GameBalanceConfig
        {
            EnableDebugLogs = false,
            DeathPenalty = 999,
            PassiveFavorRate = 9f
        };

        // Simulates ConfigLib (and the mod) holding a reference to the registered instance.
        var heldReference = config;

        config.CopyFrom(new GameBalanceConfig());

        // Same object, defaults restored — the holder sees the reset.
        Assert.Same(config, heldReference);
        Assert.True(heldReference.EnableDebugLogs);
        Assert.Equal(50, heldReference.DeathPenalty);
        Assert.Equal(0.5f, heldReference.PassiveFavorRate);
    }

    [Fact]
    public void CopyFrom_CopiesProvidedValues()
    {
        var target = new GameBalanceConfig();
        var source = new GameBalanceConfig
        {
            EnableErrorLogs = false,
            DeathPenalty = 123
        };

        target.CopyFrom(source);

        Assert.False(target.EnableErrorLogs);
        Assert.Equal(123, target.DeathPenalty);
    }
}
