using System.Diagnostics.CodeAnalysis;
using DivineAscension.Commands.Parsers;
using Moq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace DivineAscension.Tests.Commands.Parsers;

/// <summary>
/// Tests for the QuotedStringParser command argument parser.
/// </summary>
[ExcludeFromCodeCoverage]
public class QuotedStringParserTests
{
    #region Test Helpers

    private static TextCommandCallingArgs CreateArgs(params string[] rawArgs)
    {
        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("test-player");
        mockPlayer.Setup(p => p.PlayerName).Returns("TestPlayer");

        return new TextCommandCallingArgs
        {
            LanguageCode = "en",
            Caller = new Caller
            {
                Type = EnumCallerType.Player,
                Player = mockPlayer.Object,
                CallerPrivileges = new[] { "chat" },
                CallerRole = "player",
                Pos = new Vec3d(0, 0, 0)
            },
            RawArgs = new CmdArgs(rawArgs),
            Parsers = new List<ICommandArgumentParser>()
        };
    }

    #endregion

    #region GetValidRange Tests

    [Fact]
    public void GetValidRange_ReturnsEmptyArray()
    {
        var parser = new QuotedStringParser("name", isMandatory: true);
        var result = parser.GetValidRange(new CmdArgs());

        Assert.Empty(result);
    }

    #endregion

    #region Single Word (Unquoted) Tests

    [Fact]
    public void TryProcess_SingleWord_ExtractsCorrectly()
    {
        // Arrange
        var parser = new QuotedStringParser("name", isMandatory: true);
        var args = CreateArgs("TestReligion");

        // Act
        var result = parser.TryProcess(args);

        // Assert
        Assert.Equal(EnumParseResult.Good, result);
        Assert.Equal("TestReligion", parser.GetValue());
        Assert.False(parser.IsMissing);
    }

    [Fact]
    public void TryProcess_SingleWordWithTrailingArgs_LeavesRemainingArgs()
    {
        // Arrange
        var parser = new QuotedStringParser("name", isMandatory: true);
        var args = CreateArgs("TestReligion", "Craft", "public");

        // Act
        var result = parser.TryProcess(args);

        // Assert
        Assert.Equal(EnumParseResult.Good, result);
        Assert.Equal("TestReligion", parser.GetValue());
        // Remaining args should still be available
        Assert.Equal(2, args.RawArgs.Length);
    }

    #endregion

    #region Quoted String Tests

    [Fact]
    public void TryProcess_QuotedString_ExtractsWithSpaces()
    {
        // Arrange
        var parser = new QuotedStringParser("name", isMandatory: true);
        var args = CreateArgs("\"My", "Religion", "Name\"");

        // Act
        var result = parser.TryProcess(args);

        // Assert
        Assert.Equal(EnumParseResult.Good, result);
        Assert.Equal("My Religion Name", parser.GetValue());
        Assert.False(parser.IsMissing);
    }

    [Fact]
    public void TryProcess_QuotedStringWithTrailingArgs_LeavesRemainingArgs()
    {
        // Arrange
        var parser = new QuotedStringParser("name", isMandatory: true);
        var args = CreateArgs("\"My", "Religion\"", "Craft", "public");

        // Act
        var result = parser.TryProcess(args);

        // Assert
        Assert.Equal(EnumParseResult.Good, result);
        Assert.Equal("My Religion", parser.GetValue());
        // Remaining args should be "Craft public"
        Assert.True(args.RawArgs.Length > 0);
    }

    [Fact]
    public void TryProcess_QuotedEmptyString_ExtractsEmptyString()
    {
        // Arrange
        var parser = new QuotedStringParser("name", isMandatory: true);
        var args = CreateArgs("\"\"");

        // Act
        var result = parser.TryProcess(args);

        // Assert
        Assert.Equal(EnumParseResult.Good, result);
        Assert.Equal("", parser.GetValue());
        Assert.False(parser.IsMissing);
    }

    #endregion

    #region Error Cases

    [Fact]
    public void TryProcess_UnclosedQuote_ReturnsBad()
    {
        // Arrange
        var parser = new QuotedStringParser("name", isMandatory: true);
        var args = CreateArgs("\"Unclosed", "Quote");

        // Act
        var result = parser.TryProcess(args);

        // Assert
        Assert.Equal(EnumParseResult.Bad, result);
        Assert.True(parser.IsMissing);
        Assert.NotNull(parser.LastErrorMessage);
        Assert.Contains("Unclosed quote", parser.LastErrorMessage);
    }

    [Fact]
    public void TryProcess_MandatoryMissingArg_ReturnsBad()
    {
        // Arrange
        var parser = new QuotedStringParser("name", isMandatory: true);
        var args = CreateArgs();

        // Act
        var result = parser.TryProcess(args);

        // Assert
        Assert.Equal(EnumParseResult.Bad, result);
        Assert.True(parser.IsMissing);
        Assert.NotNull(parser.LastErrorMessage);
        Assert.Contains("Missing required argument", parser.LastErrorMessage);
    }

    #endregion

    #region Optional Parser Tests

    [Fact]
    public void TryProcess_OptionalMissingArg_ReturnsGood()
    {
        // Arrange
        var parser = new QuotedStringParser("name", isMandatory: false);
        var args = CreateArgs();

        // Act
        var result = parser.TryProcess(args);

        // Assert
        Assert.Equal(EnumParseResult.Good, result);
        Assert.True(parser.IsMissing);
        Assert.Null(parser.GetValue());
    }

    [Fact]
    public void TryProcess_OptionalWithValue_ExtractsCorrectly()
    {
        // Arrange
        var parser = new QuotedStringParser("name", isMandatory: false);
        var args = CreateArgs("\"Optional", "Name\"");

        // Act
        var result = parser.TryProcess(args);

        // Assert
        Assert.Equal(EnumParseResult.Good, result);
        Assert.Equal("Optional Name", parser.GetValue());
        Assert.False(parser.IsMissing);
    }

    #endregion

    #region Property Tests

    [Fact]
    public void ArgumentName_ReturnsConfiguredName()
    {
        var parser = new QuotedStringParser("religionname", isMandatory: true);
        Assert.Equal("religionname", parser.ArgumentName);
    }

    [Fact]
    public void IsMandatoryArg_ReturnsConfiguredValue()
    {
        var mandatoryParser = new QuotedStringParser("name", isMandatory: true);
        var optionalParser = new QuotedStringParser("name", isMandatory: false);

        Assert.True(mandatoryParser.IsMandatoryArg);
        Assert.False(optionalParser.IsMandatoryArg);
    }

    [Fact]
    public void GetSyntax_MandatoryArg_ReturnsAngleBrackets()
    {
        var parser = new QuotedStringParser("name", isMandatory: true);
        Assert.Equal("<name>", parser.GetSyntax());
    }

    [Fact]
    public void GetSyntax_OptionalArg_ReturnsSquareBrackets()
    {
        var parser = new QuotedStringParser("name", isMandatory: false);
        Assert.Equal("[name]", parser.GetSyntax());
    }

    [Fact]
    public void GetSyntaxExplanation_IncludesQuotedStringInfo()
    {
        var parser = new QuotedStringParser("name", isMandatory: true);
        var explanation = parser.GetSyntaxExplanation("  ");

        Assert.Contains("name", explanation);
        Assert.Contains("quoted", explanation.ToLower());
    }

    [Fact]
    public void ArgCount_WhenMissing_ReturnsZero()
    {
        var parser = new QuotedStringParser("name", isMandatory: false);
        var args = CreateArgs();

        parser.TryProcess(args);

        Assert.Equal(0, parser.ArgCount);
    }

    [Fact]
    public void ArgCount_WhenParsed_ReturnsOne()
    {
        var parser = new QuotedStringParser("name", isMandatory: true);
        var args = CreateArgs("TestValue");

        parser.TryProcess(args);

        Assert.Equal(1, parser.ArgCount);
    }

    #endregion

    #region SetValue Tests

    [Fact]
    public void SetValue_WithString_SetsValueCorrectly()
    {
        var parser = new QuotedStringParser("name", isMandatory: true);

        parser.SetValue("Test Value");

        Assert.Equal("Test Value", parser.GetValue());
        Assert.False(parser.IsMissing);
    }

    [Fact]
    public void SetValue_WithNull_SetsIsMissingTrue()
    {
        var parser = new QuotedStringParser("name", isMandatory: true);

        parser.SetValue(null);

        Assert.Null(parser.GetValue());
        Assert.True(parser.IsMissing);
    }

    #endregion
}