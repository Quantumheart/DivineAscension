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

    #region Multiple Parser Sequence Tests

    /// <summary>
    /// Regression test for the bug where multiple QuotedStringParsers in sequence
    /// with Word parsers between them failed to parse correctly.
    /// Simulates: /religion create "TEST TEST" craft "TESTY"
    /// </summary>
    [Fact]
    public void TryProcess_MultipleQuotedStringsWithWordBetween_ParsesAllTokensCorrectly()
    {
        // Arrange - simulate /religion create "TEST TEST" craft "TESTY"
        // The shell splits this into: ["\"TEST", "TEST\"", "craft", "\"TESTY\""]
        var args = CreateArgs("\"TEST", "TEST\"", "craft", "\"TESTY\"");

        var nameParser = new QuotedStringParser("name", isMandatory: true);

        // Act - first parser extracts the quoted religion name
        var result1 = nameParser.TryProcess(args);

        // Assert - first parser works
        Assert.Equal(EnumParseResult.Good, result1);
        Assert.Equal("TEST TEST", nameParser.GetValue());

        // The remaining args should be separate tokens: "craft" and "\"TESTY\""
        // NOT a single combined string "craft \"TESTY\""
        Assert.Equal(2, args.RawArgs.Length);

        // Pop the domain word (simulating Word parser)
        var domain = args.RawArgs.PopWord();
        Assert.Equal("craft", domain);

        // Now the second QuotedStringParser should be able to parse "TESTY"
        var deityParser = new QuotedStringParser("deityname", isMandatory: true);
        var result2 = deityParser.TryProcess(args);

        Assert.Equal(EnumParseResult.Good, result2);
        Assert.Equal("TESTY", deityParser.GetValue());
    }

    /// <summary>
    /// Tests that quoted strings in the remaining args are preserved as single tokens
    /// after the first QuotedStringParser processes.
    /// </summary>
    [Fact]
    public void TryProcess_QuotedStringInRemainingArgs_PreservedAsSingleToken()
    {
        // Arrange - simulate: "First Name" word "Second Name" optional
        var args = CreateArgs("\"First", "Name\"", "word", "\"Second", "Name\"", "optional");

        var firstParser = new QuotedStringParser("first", isMandatory: true);

        // Act
        var result = firstParser.TryProcess(args);

        // Assert
        Assert.Equal(EnumParseResult.Good, result);
        Assert.Equal("First Name", firstParser.GetValue());

        // Remaining should be 3 tokens: "word", "\"Second Name\"" (preserved as single token), "optional"
        Assert.Equal(3, args.RawArgs.Length);

        // Pop the single word
        var word = args.RawArgs.PopWord();
        Assert.Equal("word", word);

        // Parse the second quoted string
        var secondParser = new QuotedStringParser("second", isMandatory: true);
        var result2 = secondParser.TryProcess(args);

        Assert.Equal(EnumParseResult.Good, result2);
        Assert.Equal("Second Name", secondParser.GetValue());

        // Should still have "optional" remaining
        Assert.Equal(1, args.RawArgs.Length);
        Assert.Equal("optional", args.RawArgs.PopWord());
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