using System.Diagnostics.CodeAnalysis;
using DivineAscension.Constants;
using DivineAscension.Models.Enum;
using DivineAscension.Tests.Commands.Helpers;
using Vintagestory.API.Common;

namespace DivineAscension.Tests.Commands.Blessing;

[ExcludeFromCodeCoverage]
public class BlessingCommandInfoTests : BlessingCommandsTestHelpers
{
    public BlessingCommandInfoTests()
    {
        _sut = InitializeMocksAndSut();
    }

    #region Error Cases

    [Fact]
    public void GetBlessingInfo_BlessingIdNullOrEmpty_ReturnsUsageError()
    {
        // Act
        var result = _sut!.GetInfo(null);

        // Assert
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Equal(UsageMessageConstants.UsageBlessingsInfo, result.StatusMessage);
    }

    [Fact]
    public void GetBlessingInfo_BlessingNotFound_ReturnsError()
    {
        // Arrange
        _blessingRegistry.Setup(br => br.GetBlessing("nonexistent")).Returns((DivineAscension.Models.Blessing?)null);

        // Act
        var result = _sut!.GetInfo("nonexistent");

        // Assert
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("not found", result.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Player Blessing Info Display

    [Fact]
    public void GetBlessingInfo_PlayerBlessing_DisplaysBasicInfo()
    {
        // Arrange
        var blessing = new DivineAscension.Models.Blessing
        {
            BlessingId = "divine_strike",
            Name = "Divine Strike",
            Domain = DeityDomain.Craft,
            Kind = BlessingKind.Player,
            Category = BlessingCategory.Combat,
            Description = "Channel divine power into your strikes",
            RequiredFavorRank = (int)FavorRank.Initiate,
            PrerequisiteBlessings = new List<string>(),
            StatModifiers = new Dictionary<string, float>(),
            SpecialEffects = new List<string>()
        };

        _blessingRegistry.Setup(br => br.GetBlessing("divine_strike")).Returns(blessing);

        // Act
        var result = _sut!.GetInfo("divine_strike");

        // Assert
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("Divine Strike", result.StatusMessage);
        Assert.Contains("divine_strike", result.StatusMessage);
        Assert.Contains("Khoras", result.StatusMessage);
        Assert.Contains("Player", result.StatusMessage);
        Assert.Contains("Combat", result.StatusMessage);
        Assert.Contains("Channel divine power", result.StatusMessage);
    }

    [Fact]
    public void GetBlessingInfo_PlayerBlessing_DisplaysFavorRankRequirement()
    {
        // Arrange
        var blessing = new DivineAscension.Models.Blessing
        {
            BlessingId = "advanced_blessing",
            Name = "Advanced Blessing",
            Domain = DeityDomain.Craft,
            Kind = BlessingKind.Player,
            Category = BlessingCategory.Combat,
            Description = "Advanced blessing",
            RequiredFavorRank = (int)FavorRank.Zealot,
            PrerequisiteBlessings = new List<string>(),
            StatModifiers = new Dictionary<string, float>(),
            SpecialEffects = new List<string>()
        };

        _blessingRegistry.Setup(br => br.GetBlessing("advanced_blessing")).Returns(blessing);

        // Act
        var result = _sut!.GetInfo("advanced_blessing");

        // Assert
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("Zealot", result.StatusMessage);
    }

    [Fact]
    public void GetBlessingInfo_BlessingWithSinglePrerequisite_DisplaysPrerequisiteName()
    {
        // Arrange
        var prereqBlessing = new DivineAscension.Models.Blessing
        {
            BlessingId = "basic_blessing",
            Name = "Basic Blessing",
            Domain = DeityDomain.Craft,
            Kind = BlessingKind.Player,
            Category = BlessingCategory.Combat,
            Description = "Basic",
            RequiredFavorRank = (int)FavorRank.Initiate,
            PrerequisiteBlessings = new List<string>(),
            StatModifiers = new Dictionary<string, float>(),
            SpecialEffects = new List<string>()
        };

        var blessing = new DivineAscension.Models.Blessing
        {
            BlessingId = "advanced_blessing",
            Name = "Advanced Blessing",
            Domain = DeityDomain.Craft,
            Kind = BlessingKind.Player,
            Category = BlessingCategory.Combat,
            Description = "Advanced",
            RequiredFavorRank = (int)FavorRank.Disciple,
            PrerequisiteBlessings = new List<string> { "basic_blessing" },
            StatModifiers = new Dictionary<string, float>(),
            SpecialEffects = new List<string>()
        };

        _blessingRegistry.Setup(br => br.GetBlessing("advanced_blessing")).Returns(blessing);
        _blessingRegistry.Setup(br => br.GetBlessing("basic_blessing")).Returns(prereqBlessing);

        // Act
        var result = _sut!.GetInfo("advanced_blessing");

        // Assert
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("Basic Blessing", result.StatusMessage);
    }

    [Fact]
    public void GetBlessingInfo_BlessingWithMultiplePrerequisites_DisplaysAllPrerequisites()
    {
        // Arrange
        var prereq1 = new DivineAscension.Models.Blessing
        {
            BlessingId = "prereq1",
            Name = "First Prerequisite",
            Domain = DeityDomain.Craft,
            Kind = BlessingKind.Player,
            Category = BlessingCategory.Combat,
            Description = "First",
            RequiredFavorRank = (int)FavorRank.Initiate,
            PrerequisiteBlessings = new List<string>(),
            StatModifiers = new Dictionary<string, float>(),
            SpecialEffects = new List<string>()
        };

        var prereq2 = new DivineAscension.Models.Blessing
        {
            BlessingId = "prereq2",
            Name = "Second Prerequisite",
            Domain = DeityDomain.Craft,
            Kind = BlessingKind.Player,
            Category = BlessingCategory.Combat,
            Description = "Second",
            RequiredFavorRank = (int)FavorRank.Initiate,
            PrerequisiteBlessings = new List<string>(),
            StatModifiers = new Dictionary<string, float>(),
            SpecialEffects = new List<string>()
        };

        var blessing = new DivineAscension.Models.Blessing
        {
            BlessingId = "ultimate_blessing",
            Name = "Ultimate Blessing",
            Domain = DeityDomain.Craft,
            Kind = BlessingKind.Player,
            Category = BlessingCategory.Combat,
            Description = "Ultimate",
            RequiredFavorRank = (int)FavorRank.Champion,
            PrerequisiteBlessings = new List<string> { "prereq1", "prereq2" },
            StatModifiers = new Dictionary<string, float>(),
            SpecialEffects = new List<string>()
        };

        _blessingRegistry.Setup(br => br.GetBlessing("ultimate_blessing")).Returns(blessing);
        _blessingRegistry.Setup(br => br.GetBlessing("prereq1")).Returns(prereq1);
        _blessingRegistry.Setup(br => br.GetBlessing("prereq2")).Returns(prereq2);

        // Act
        var result = _sut!.GetInfo("ultimate_blessing");

        // Assert
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("First Prerequisite", result.StatusMessage);
        Assert.Contains("Second Prerequisite", result.StatusMessage);
    }

    [Fact]
    public void GetBlessingInfo_PrerequisiteBlessingNotFound_ShowsIdAsFallback()
    {
        // Arrange
        var blessing = new DivineAscension.Models.Blessing
        {
            BlessingId = "orphan_blessing",
            Name = "Orphan Blessing",
            Domain = DeityDomain.Craft,
            Kind = BlessingKind.Player,
            Category = BlessingCategory.Combat,
            Description = "Has missing prereq",
            RequiredFavorRank = (int)FavorRank.Disciple,
            PrerequisiteBlessings = new List<string> { "missing_prereq" },
            StatModifiers = new Dictionary<string, float>(),
            SpecialEffects = new List<string>()
        };

        _blessingRegistry.Setup(br => br.GetBlessing("orphan_blessing")).Returns(blessing);
        _blessingRegistry.Setup(br => br.GetBlessing("missing_prereq")).Returns((DivineAscension.Models.Blessing?)null);

        // Act
        var result = _sut!.GetInfo("orphan_blessing");

        // Assert
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("missing_prereq", result.StatusMessage);
    }

    [Fact]
    public void GetBlessingInfo_BlessingWithStatModifiers_FormatsAsPercentage()
    {
        // Arrange
        var blessing = new DivineAscension.Models.Blessing
        {
            BlessingId = "speed_blessing",
            Name = "Speed Blessing",
            Domain = DeityDomain.Craft,
            Kind = BlessingKind.Player,
            Category = BlessingCategory.Utility,
            Description = "Move faster",
            RequiredFavorRank = (int)FavorRank.Initiate,
            PrerequisiteBlessings = new List<string>(),
            StatModifiers = new Dictionary<string, float>
            {
                { "walkspeed", 0.15f },
                { "maxhealthExtraPoints", 10.0f }
            },
            SpecialEffects = new List<string>()
        };

        _blessingRegistry.Setup(br => br.GetBlessing("speed_blessing")).Returns(blessing);

        // Act
        var result = _sut!.GetInfo("speed_blessing");

        // Assert
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("Movement Speed", result.StatusMessage); // walkspeed → Movement Speed
        Assert.Contains("15", result.StatusMessage); // 0.15 * 100 = 15
    }

    #endregion

    #region Religion Blessing Info Display

    [Fact]
    public void GetBlessingInfo_ReligionBlessing_DisplaysPrestigeRankRequirement()
    {
        // Arrange
        var blessing = new DivineAscension.Models.Blessing
        {
            BlessingId = "divine_sanctuary",
            Name = "Divine Sanctuary",
            Domain = DeityDomain.Craft,
            Kind = BlessingKind.Religion,
            Category = BlessingCategory.Defense,
            Description = "Protects all members",
            RequiredPrestigeRank = (int)PrestigeRank.Renowned,
            PrerequisiteBlessings = new List<string>(),
            StatModifiers = new Dictionary<string, float>(),
            SpecialEffects = new List<string>()
        };

        _blessingRegistry.Setup(br => br.GetBlessing("divine_sanctuary")).Returns(blessing);

        // Act
        var result = _sut!.GetInfo("divine_sanctuary");

        // Assert
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("Renowned", result.StatusMessage);
        Assert.Contains("Religion", result.StatusMessage);
    }

    [Fact]
    public void GetBlessingInfo_BlessingWithSingleSpecialEffect_DisplaysEffect()
    {
        // Arrange
        var blessing = new DivineAscension.Models.Blessing
        {
            BlessingId = "fire_aura",
            Name = "Fire Aura",
            Domain = DeityDomain.Craft,
            Kind = BlessingKind.Religion,
            Category = BlessingCategory.Combat,
            Description = "Burn enemies",
            RequiredPrestigeRank = (int)PrestigeRank.Fledgling,
            PrerequisiteBlessings = new List<string>(),
            StatModifiers = new Dictionary<string, float>(),
            SpecialEffects = new List<string> { "fire_damage_aura" }
        };

        _blessingRegistry.Setup(br => br.GetBlessing("fire_aura")).Returns(blessing);

        // Act
        var result = _sut!.GetInfo("fire_aura");

        // Assert
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("fire_damage_aura", result.StatusMessage);
    }

    [Fact]
    public void GetBlessingInfo_BlessingWithMultipleSpecialEffects_DisplaysAllEffects()
    {
        // Arrange
        var blessing = new DivineAscension.Models.Blessing
        {
            BlessingId = "combo_blessing",
            Name = "Combo Blessing",
            Domain = DeityDomain.Craft,
            Kind = BlessingKind.Religion,
            Category = BlessingCategory.Combat,
            Description = "Multiple effects",
            RequiredPrestigeRank = (int)PrestigeRank.Renowned,
            PrerequisiteBlessings = new List<string>(),
            StatModifiers = new Dictionary<string, float>(),
            SpecialEffects = new List<string> { "effect1", "effect2", "effect3" }
        };

        _blessingRegistry.Setup(br => br.GetBlessing("combo_blessing")).Returns(blessing);

        // Act
        var result = _sut!.GetInfo("combo_blessing");

        // Assert
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("effect1", result.StatusMessage);
        Assert.Contains("effect2", result.StatusMessage);
        Assert.Contains("effect3", result.StatusMessage);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void GetBlessingInfo_BlessingWithBothStatModifiersAndSpecialEffects_DisplaysBoth()
    {
        // Arrange
        var blessing = new DivineAscension.Models.Blessing
        {
            BlessingId = "complete_blessing",
            Name = "Complete Blessing",
            Domain = DeityDomain.Craft,
            Kind = BlessingKind.Player,
            Category = BlessingCategory.Utility,
            Description = "Everything",
            RequiredFavorRank = (int)FavorRank.Champion,
            PrerequisiteBlessings = new List<string>(),
            StatModifiers = new Dictionary<string, float> { { "walkspeed", 0.1f } },
            SpecialEffects = new List<string> { "special_effect" }
        };

        _blessingRegistry.Setup(br => br.GetBlessing("complete_blessing")).Returns(blessing);

        // Act
        var result = _sut!.GetInfo("complete_blessing");

        // Assert
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("Movement Speed", result.StatusMessage); // walkspeed → Movement Speed
        Assert.Contains("special_effect", result.StatusMessage);
    }

    [Fact]
    public void GetBlessingInfo_BlessingWithEmptyModifiersAndEffects_DoesNotDisplayEmptySections()
    {
        // Arrange
        var blessing = new DivineAscension.Models.Blessing
        {
            BlessingId = "simple_blessing",
            Name = "Simple Blessing",
            Domain = DeityDomain.Craft,
            Kind = BlessingKind.Player,
            Category = BlessingCategory.Utility,
            Description = "Simple",
            RequiredFavorRank = (int)FavorRank.Initiate,
            PrerequisiteBlessings = new List<string>(),
            StatModifiers = new Dictionary<string, float>(),
            SpecialEffects = new List<string>()
        };

        _blessingRegistry.Setup(br => br.GetBlessing("simple_blessing")).Returns(blessing);

        // Act
        var result = _sut!.GetInfo("simple_blessing");

        // Assert
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        // No stat modifiers or special effects sections should appear
        Assert.DoesNotContain("Stat Modifiers:", result.StatusMessage);
        Assert.DoesNotContain("Special Effects:", result.StatusMessage);
    }

    [Fact]
    public void GetBlessingInfo_BlessingWithNoPrerequisites_DoesNotDisplayPrerequisitesSection()
    {
        // Arrange
        var blessing = new DivineAscension.Models.Blessing
        {
            BlessingId = "starter_blessing",
            Name = "Starter Blessing",
            Domain = DeityDomain.Craft,
            Kind = BlessingKind.Player,
            Category = BlessingCategory.Combat,
            Description = "No prereqs",
            RequiredFavorRank = (int)FavorRank.Initiate,
            PrerequisiteBlessings = new List<string>(),
            StatModifiers = new Dictionary<string, float>(),
            SpecialEffects = new List<string>()
        };

        _blessingRegistry.Setup(br => br.GetBlessing("starter_blessing")).Returns(blessing);

        // Act
        var result = _sut!.GetInfo("starter_blessing");

        // Assert
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        // No prerequisites section should appear
        Assert.DoesNotContain("Prerequisites:", result.StatusMessage);
    }

    #endregion
}