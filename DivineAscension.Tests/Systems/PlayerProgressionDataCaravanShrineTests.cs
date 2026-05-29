using DivineAscension.Systems;

namespace DivineAscension.Tests.Systems;

public class PlayerProgressionDataCaravanShrineTests
{
    [Fact]
    public void HasPlacedCaravanShrine_DefaultsFalse()
    {
        var data = new PlayerProgressionData("p1");
        Assert.False(data.HasPlacedCaravanShrine);
    }

    [Fact]
    public void SetPlacedCaravanShrine_RecordsCoords()
    {
        var data = new PlayerProgressionData("p1");
        data.SetPlacedCaravanShrine(10, 64, -20);

        Assert.True(data.HasPlacedCaravanShrine);
        Assert.Equal(10, data.PlacedCaravanShrineX);
        Assert.Equal(64, data.PlacedCaravanShrineY);
        Assert.Equal(-20, data.PlacedCaravanShrineZ);
    }

    [Fact]
    public void IsPlacedCaravanShrineAt_MatchesExactCoords()
    {
        var data = new PlayerProgressionData("p1");
        data.SetPlacedCaravanShrine(10, 64, -20);

        Assert.True(data.IsPlacedCaravanShrineAt(10, 64, -20));
        Assert.False(data.IsPlacedCaravanShrineAt(11, 64, -20));
        Assert.False(data.IsPlacedCaravanShrineAt(10, 65, -20));
        Assert.False(data.IsPlacedCaravanShrineAt(10, 64, -19));
    }

    [Fact]
    public void IsPlacedCaravanShrineAt_FalseWhenUnset()
    {
        var data = new PlayerProgressionData("p1");
        Assert.False(data.IsPlacedCaravanShrineAt(0, 0, 0));
    }

    [Fact]
    public void ClearPlacedCaravanShrine_ResetsToNull()
    {
        var data = new PlayerProgressionData("p1");
        data.SetPlacedCaravanShrine(1, 2, 3);
        data.ClearPlacedCaravanShrine();

        Assert.False(data.HasPlacedCaravanShrine);
        Assert.Null(data.PlacedCaravanShrineX);
        Assert.Null(data.PlacedCaravanShrineY);
        Assert.Null(data.PlacedCaravanShrineZ);
    }

    [Fact]
    public void NegativeCoords_RoundTrip()
    {
        var data = new PlayerProgressionData("p1");
        data.SetPlacedCaravanShrine(-500_000, 80, -1_000_000);

        Assert.True(data.IsPlacedCaravanShrineAt(-500_000, 80, -1_000_000));
    }
}
