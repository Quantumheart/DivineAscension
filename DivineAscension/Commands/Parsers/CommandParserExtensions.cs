using Vintagestory.API.Common;

namespace DivineAscension.Commands.Parsers;

/// <summary>
/// Extension methods to add custom parsers to Vintage Story's command API.
/// Usage: _sapi.ChatCommands.Parsers.QuotedString("name")
/// </summary>
public static class CommandParserExtensions
{
    /// <summary>
    /// Creates a parser that handles quoted strings with spaces.
    /// Users can specify names as: "My Multi Word Name" or SingleWord
    /// </summary>
    /// <param name="parsers">The command argument parsers instance</param>
    /// <param name="argName">The argument name for help/syntax display</param>
    /// <returns>A new QuotedStringParser instance</returns>
    public static QuotedStringParser QuotedString(this CommandArgumentParsers parsers, string argName)
    {
        return new QuotedStringParser(argName, isMandatory: true);
    }

    /// <summary>
    /// Creates an optional parser that handles quoted strings with spaces.
    /// Users can specify names as: "My Multi Word Name" or SingleWord
    /// </summary>
    /// <param name="parsers">The command argument parsers instance</param>
    /// <param name="argName">The argument name for help/syntax display</param>
    /// <returns>A new QuotedStringParser instance (optional)</returns>
    public static QuotedStringParser OptionalQuotedString(this CommandArgumentParsers parsers, string argName)
    {
        return new QuotedStringParser(argName, isMandatory: false);
    }
}