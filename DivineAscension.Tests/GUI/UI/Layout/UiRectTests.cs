using System.Diagnostics.CodeAnalysis;
using DivineAscension.GUI.UI.Layout;

namespace DivineAscension.Tests.GUI.UI.Layout;

[ExcludeFromCodeCoverage]
public class UiRectTests
{
    private static readonly UiRect Source = new(10f, 20f, 200f, 100f);

    [Fact]
    public void SplitLeft_NormalCase_ReturnsSliceAndRemainder()
    {
        var (left, remainder) = Source.SplitLeft(60f, 8f);

        Assert.Equal(new UiRect(10f, 20f, 60f, 100f), left);
        Assert.Equal(new UiRect(78f, 20f, 132f, 100f), remainder);
    }

    [Fact]
    public void SplitLeft_NoGap_RemainderAbutsSlice()
    {
        var (left, remainder) = Source.SplitLeft(50f);
        Assert.Equal(left.Right, remainder.X);
    }

    [Fact]
    public void SplitLeft_OversizeWidth_ClampsToRectAndRemainderIsZero()
    {
        var (left, remainder) = Source.SplitLeft(500f);

        Assert.Equal(Source.W, left.W);
        Assert.Equal(0f, remainder.W);
    }

    [Fact]
    public void SplitLeft_ZeroWidth_LeftIsEmptyRemainderFull()
    {
        var (left, remainder) = Source.SplitLeft(0f);

        Assert.Equal(0f, left.W);
        Assert.Equal(Source.W, remainder.W);
    }

    [Fact]
    public void SplitLeft_NegativeGap_ClampsToZero()
    {
        var (_, withZero) = Source.SplitLeft(40f, 0f);
        var (_, withNegative) = Source.SplitLeft(40f, -10f);

        Assert.Equal(withZero, withNegative);
    }

    [Fact]
    public void SplitRight_NormalCase_ReturnsRemainderAndSlice()
    {
        var (remainder, right) = Source.SplitRight(60f, 8f);

        Assert.Equal(new UiRect(10f, 20f, 132f, 100f), remainder);
        Assert.Equal(new UiRect(150f, 20f, 60f, 100f), right);
    }

    [Fact]
    public void SplitRight_OversizeWidth_RemainderIsZero()
    {
        var (remainder, right) = Source.SplitRight(500f);

        Assert.Equal(0f, remainder.W);
        Assert.Equal(Source.W, right.W);
    }

    [Fact]
    public void Inset_NormalCase_ShrinksAllSides()
    {
        var inset = Source.Inset(5f);
        Assert.Equal(new UiRect(15f, 25f, 190f, 90f), inset);
    }

    [Fact]
    public void Inset_OversizedPx_ClampsToZeroDimensions()
    {
        var inset = Source.Inset(200f);

        Assert.Equal(0f, inset.W);
        Assert.Equal(0f, inset.H);
    }

    [Fact]
    public void Inset_NegativePx_ClampsToZero()
    {
        Assert.Equal(Source, Source.Inset(-3f));
    }

    [Fact]
    public void Cut_NormalCase_TrimsTopAndBottom()
    {
        var cut = Source.Cut(10f, 5f);
        Assert.Equal(new UiRect(10f, 30f, 200f, 85f), cut);
    }

    [Fact]
    public void Cut_OversizedTrim_HeightClampsToZero()
    {
        var cut = Source.Cut(80f, 80f);
        Assert.Equal(0f, cut.H);
    }
}
