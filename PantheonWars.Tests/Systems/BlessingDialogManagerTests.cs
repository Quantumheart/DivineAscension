using System.Diagnostics.CodeAnalysis;
using PantheonWars.GUI;
using PantheonWars.Models.Enum;

namespace PantheonWars.Tests.Systems;

[ExcludeFromCodeCoverage]
public class BlessingDialogManagerTests
{
    [Fact]
    public void TestPropertyInitialization()
    {
        var manager = new BlessingDialogManager(null!);
        Assert.Null(manager.CurrentReligionUID);
        Assert.Equal(DeityType.None, manager.CurrentDeity);
        Assert.Null(manager.CurrentReligionName);
        Assert.False(manager.IsDataLoaded);
    }

    [Fact]
    public void TestInitializeMethod()
    {
        var manager = new BlessingDialogManager(null!);
        manager.Initialize("religion123", DeityType.Khoras, "God of Warriors");
        Assert.Equal("religion123", manager.CurrentReligionUID);
        Assert.Equal(DeityType.Khoras, manager.CurrentDeity);
        Assert.Equal("God of Warriors", manager.CurrentReligionName);
        Assert.True(manager.IsDataLoaded);
    }

    [Fact]
    public void TestResetMethod()
    {
        var manager = new BlessingDialogManager(null!);
        manager.Initialize("religion123", DeityType.Khoras, "God of Warriors");
        manager.Reset();
        Assert.Null(manager.CurrentReligionUID);
        Assert.Equal(DeityType.None, manager.CurrentDeity);
        Assert.Null(manager.CurrentReligionName);
        Assert.False(manager.IsDataLoaded);
    }

    [Fact]
    public void TestHasReligion()
    {
        var manager = new BlessingDialogManager(null!);
        Assert.False(manager.HasReligion());

        manager.Initialize("religion123", DeityType.Khoras, "God of Warriors");
        Assert.True(manager.HasReligion());

        manager.Reset();
        Assert.False(manager.HasReligion());
    }
}