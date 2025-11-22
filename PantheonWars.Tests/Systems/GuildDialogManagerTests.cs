using System.Diagnostics.CodeAnalysis;
using PantheonWars.GUI;

namespace PantheonWars.Tests.Systems;

[ExcludeFromCodeCoverage]
public class GuildDialogManagerTests
{
    [Fact]
    public void TestPropertyInitialization()
    {
        var manager = new GuildDialogManager(null!);
        Assert.Null(manager.CurrentReligionUID);
        Assert.Null(manager.CurrentReligionName);
        Assert.False(manager.IsDataLoaded);
    }

    [Fact]
    public void TestInitializeMethod()
    {
        var manager = new GuildDialogManager(null!);
        manager.Initialize("religion123", "Warriors Guild");
        Assert.Equal("religion123", manager.CurrentReligionUID);
        Assert.Equal("Warriors Guild", manager.CurrentReligionName);
        Assert.True(manager.IsDataLoaded);
    }

    [Fact]
    public void TestResetMethod()
    {
        var manager = new GuildDialogManager(null!);
        manager.Initialize("religion123", "Warriors Guild");
        manager.Reset();
        Assert.Null(manager.CurrentReligionUID);
        Assert.Null(manager.CurrentReligionName);
        Assert.False(manager.IsDataLoaded);
    }

    [Fact]
    public void TestHasReligion()
    {
        var manager = new GuildDialogManager(null!);
        Assert.False(manager.HasReligion());

        manager.Initialize("religion123", "Warriors Guild");
        Assert.True(manager.HasReligion());

        manager.Reset();
        Assert.False(manager.HasReligion());
    }
}