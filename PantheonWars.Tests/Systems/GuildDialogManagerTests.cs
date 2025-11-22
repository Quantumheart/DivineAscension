using System.Diagnostics.CodeAnalysis;
using PantheonWars.GUI;
using PantheonWars.Models.Enum;

namespace PantheonWars.Tests.Systems;

[ExcludeFromCodeCoverage]
public class GuildDialogManagerTests
{
    [Fact]
    public void TestPropertyInitialization()
    {
        var manager = new GuildDialogManager(null!);
        Assert.Null(manager.CurrentReligionUID);
        Assert.Equal(DeityType.None, manager.CurrentDeity);
        Assert.Null(manager.CurrentReligionName);
        Assert.False(manager.IsDataLoaded);
    }

    [Fact]
    public void TestInitializeMethod()
    {
        var manager = new GuildDialogManager(null!);
        manager.Initialize("religion123", DeityType.Khoras, "God of Warriors");
        Assert.Equal("religion123", manager.CurrentReligionUID);
        Assert.Equal(DeityType.Khoras, manager.CurrentDeity);
        Assert.Equal("God of Warriors", manager.CurrentReligionName);
        Assert.True(manager.IsDataLoaded);
    }

    [Fact]
    public void TestResetMethod()
    {
        var manager = new GuildDialogManager(null!);
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
        var manager = new GuildDialogManager(null!);
        Assert.False(manager.HasReligion());

        manager.Initialize("religion123", DeityType.Khoras, "God of Warriors");
        Assert.True(manager.HasReligion());

        manager.Reset();
        Assert.False(manager.HasReligion());
    }
}