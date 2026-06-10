using System.Diagnostics.CodeAnalysis;
using DivineAscension.Services.UI;

namespace DivineAscension.Tests.Services.UI;

/// <summary>
///     Unit tests for <see cref="CinzelFontSystem.NearestBakedSize" /> — the pure
///     size-snapping used so UI-scaled serif sizes resolve to a baked atlas size.
/// </summary>
[ExcludeFromCodeCoverage]
public class CinzelFontSystemTests
{
    [Theory]
    [InlineData(6)]
    [InlineData(18)]
    [InlineData(24)]
    [InlineData(36)]
    [InlineData(60)]
    public void NearestBakedSize_ReturnsExactBakedSizeUnchanged(int baked)
    {
        Assert.Equal(baked, CinzelFontSystem.NearestBakedSize(baked));
    }

    [Theory]
    [InlineData(26, 24)] // closer to 24 than 30
    [InlineData(28, 30)] // closer to 30
    [InlineData(20, 18)] // closer to 18 than 24
    [InlineData(40, 36)] // closer to 36 than 48
    public void NearestBakedSize_SnapsToNearest(int requested, int expected)
    {
        Assert.Equal(expected, CinzelFontSystem.NearestBakedSize(requested));
    }

    [Theory]
    [InlineData(21, 18)] // |18-21| == |24-21|; lower (first-encountered) wins
    [InlineData(27, 24)] // |24-27| == |30-27|; lower wins
    public void NearestBakedSize_TieBreaksToSmaller(int requested, int expected)
    {
        Assert.Equal(expected, CinzelFontSystem.NearestBakedSize(requested));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(-10)]
    public void NearestBakedSize_BelowLadder_ClampsToSmallest(int requested)
    {
        Assert.Equal(6, CinzelFontSystem.NearestBakedSize(requested));
    }

    [Theory]
    [InlineData(72)]
    [InlineData(100)]
    public void NearestBakedSize_AboveLadder_ClampsToLargest(int requested)
    {
        Assert.Equal(60, CinzelFontSystem.NearestBakedSize(requested));
    }
}
