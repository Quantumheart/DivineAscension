using System.Diagnostics.CodeAnalysis;
using DivineAscension.Data;
using DivineAscension.GUI.State;
using Vintagestory.API.Util;

namespace DivineAscension.Tests.Data;

[ExcludeFromCodeCoverage]
public class UiPrefsTests
{
    [Fact]
    public void Defaults_MatchWindowBaseDimensions()
    {
        var prefs = new UiPrefs();

        Assert.Equal(1400, prefs.WindowWidth);
        Assert.Equal(900, prefs.WindowHeight);
        Assert.False(prefs.SidebarCollapsed);
        Assert.Empty(prefs.CollapsedGroups);
        Assert.Equal(SidebarNavId.ReligionInfo, prefs.LastNavId);
    }

    [Fact]
    public void SerializerUtil_RoundTrip_PreservesAllFields()
    {
        var prefs = new UiPrefs
        {
            WindowWidth = 1600,
            WindowHeight = 1000,
            SidebarCollapsed = true,
            LastNavId = SidebarNavId.CivilizationDiplomacy
        };
        prefs.CollapsedGroups["religion"] = true;
        prefs.CollapsedGroups["blessings"] = false;

        var bytes = SerializerUtil.Serialize(prefs);
        var roundtripped = SerializerUtil.Deserialize<UiPrefs>(bytes);

        Assert.Equal(prefs.WindowWidth, roundtripped.WindowWidth);
        Assert.Equal(prefs.WindowHeight, roundtripped.WindowHeight);
        Assert.Equal(prefs.SidebarCollapsed, roundtripped.SidebarCollapsed);
        Assert.Equal(prefs.LastNavId, roundtripped.LastNavId);
        Assert.True(roundtripped.CollapsedGroups["religion"]);
        Assert.False(roundtripped.CollapsedGroups["blessings"]);
    }

    [Fact]
    public void ModConfigData_RoundTrip_PreservesNestedUiPrefs()
    {
        var config = new ModConfigData
        {
            UiPrefs =
            {
                WindowWidth = 1280,
                LastNavId = SidebarNavId.Blessings
            }
        };
        config.UiPrefs.CollapsedGroups["x"] = true;

        var bytes = SerializerUtil.Serialize(config);
        var roundtripped = SerializerUtil.Deserialize<ModConfigData>(bytes);

        Assert.Equal(1280, roundtripped.UiPrefs.WindowWidth);
        Assert.Equal(SidebarNavId.Blessings, roundtripped.UiPrefs.LastNavId);
        Assert.True(roundtripped.UiPrefs.CollapsedGroups["x"]);
    }
}
