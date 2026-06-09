using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.GUI.UI.Utilities;
using Vintagestory.API.Config;

namespace DivineAscension.Tests.GUI.UI.Utilities;

/// <summary>
///     Unit tests for the <see cref="UiScale" /> global scale lever.
///     Each test restores <see cref="UiScale.Factor" /> to the 1.0 default so the
///     shared static state does not bleed across cases.
/// </summary>
[ExcludeFromCodeCoverage]
public class UiScaleTests
{
    public UiScaleTests() => UiScale.Factor = 1.0f;

    [Fact]
    public void DefaultFactor_IsOne()
    {
        Assert.Equal(1.0f, UiScale.Factor);
    }

    [Fact]
    public void Scaled_AtFactorOne_IsNoOp()
    {
        Assert.Equal(15f, UiScale.Scaled(15f));
        Assert.Equal(0f, UiScale.Scaled(0f));
        Assert.Equal(new Vector2(12f, 20f), UiScale.Scaled(new Vector2(12f, 20f)));
    }

    [Fact]
    public void Scaled_Float_MultipliesAndRoundsToNearestPixel()
    {
        UiScale.Factor = 1.5f;

        Assert.Equal(23f, UiScale.Scaled(15f)); // 22.5 -> 23
        Assert.Equal(21f, UiScale.Scaled(14f)); // 21.0
        Assert.Equal(2f, UiScale.Scaled(1f));   // 1.5 -> 2
    }

    [Fact]
    public void Scaled_Vector2_RoundsEachComponentIndependently()
    {
        UiScale.Factor = 1.5f;

        Assert.Equal(new Vector2(23f, 21f), UiScale.Scaled(new Vector2(15f, 14f)));
    }

    [Fact]
    public void Factor_ClampsToRange()
    {
        UiScale.Factor = 99f;
        Assert.Equal(UiScale.MaxFactor, UiScale.Factor);

        UiScale.Factor = 0.01f;
        Assert.Equal(UiScale.MinFactor, UiScale.Factor);
    }

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void Factor_RejectsNonFinite_KeepsPreviousValue(float bad)
    {
        UiScale.Factor = 2.0f;
        UiScale.Factor = bad;
        Assert.Equal(2.0f, UiScale.Factor);
    }

    [Fact]
    public void SyncFromGameSettings_AppliesGuiScale()
    {
        var original = RuntimeEnv.GUIScale;
        try
        {
            RuntimeEnv.GUIScale = 1.5f;
            UiScale.SyncFromGameSettings();
            Assert.Equal(1.5f, UiScale.Factor);
        }
        finally
        {
            RuntimeEnv.GUIScale = original;
        }
    }

    [Fact]
    public void SyncFromGameSettings_ClampsOutOfRangeGuiScale()
    {
        var original = RuntimeEnv.GUIScale;
        try
        {
            RuntimeEnv.GUIScale = 10f;
            UiScale.SyncFromGameSettings();
            Assert.Equal(UiScale.MaxFactor, UiScale.Factor);
        }
        finally
        {
            RuntimeEnv.GUIScale = original;
        }
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    public void SyncFromGameSettings_IgnoresNonPositiveGuiScale(float bogus)
    {
        var original = RuntimeEnv.GUIScale;
        try
        {
            UiScale.Factor = 2.0f;
            RuntimeEnv.GUIScale = bogus;
            UiScale.SyncFromGameSettings();
            Assert.Equal(2.0f, UiScale.Factor); // unchanged
        }
        finally
        {
            RuntimeEnv.GUIScale = original;
        }
    }
}
