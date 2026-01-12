using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;

namespace DivineAscension.Commands.Parsers;

/// <summary>
/// Custom argument parser that handles quoted strings with spaces.
/// Supports: "My Religion Name" (quoted) or SingleWord (unquoted).
/// When quoted, extracts the content between quotes and leaves remaining args for next parser.
/// When unquoted, behaves like the standard Word parser.
/// </summary>
public class QuotedStringParser : ArgumentParserBase
{
    private string? _lastErrorMessage;
    private string? _parsedValue;

    /// <summary>
    /// Creates a new quoted string parser.
    /// </summary>
    /// <param name="argName">The argument name for help/syntax display</param>
    /// <param name="isMandatory">Whether this argument is required</param>
    public QuotedStringParser(string argName, bool isMandatory)
        : base(argName, isMandatory)
    {
    }

    /// <summary>
    /// Gets the last error message from parsing, if any.
    /// </summary>
    public new string? LastErrorMessage => _lastErrorMessage;

    /// <inheritdoc />
    public new int ArgCount => IsMissing ? 0 : 1;

    /// <inheritdoc />
    public new string GetSyntax()
    {
        return IsMandatoryArg ? $"<{ArgumentName}>" : $"[{ArgumentName}]";
    }

    /// <inheritdoc />
    public override string GetSyntaxExplanation(string indent)
    {
        var optionalText = IsMandatoryArg ? "" : " (optional)";
        return $"{indent}{ArgumentName}: Name or \"quoted name with spaces\"{optionalText}";
    }

    /// <inheritdoc />
    public override string[] GetValidRange(CmdArgs args)
    {
        return Array.Empty<string>();
    }

    /// <inheritdoc />
    public override object? GetValue()
    {
        return _parsedValue;
    }

    /// <inheritdoc />
    public override void SetValue(object? data)
    {
        _parsedValue = data as string;
        IsMissing = _parsedValue == null;
    }

    /// <inheritdoc />
    public override EnumParseResult TryProcess(TextCommandCallingArgs args, Action<AsyncParseResults>? onReady = null)
    {
        var rawArgs = args.RawArgs;

        // Check if we have any input
        if (rawArgs.Length == 0)
        {
            IsMissing = true;
            _parsedValue = null;
            _lastErrorMessage = IsMandatoryArg ? $"Missing required argument: {ArgumentName}" : null;
            return IsMandatoryArg ? EnumParseResult.Bad : EnumParseResult.Good;
        }

        // Peek to check for quotes without consuming
        var firstWord = rawArgs.PeekWord();
        if (string.IsNullOrEmpty(firstWord))
        {
            IsMissing = true;
            _parsedValue = null;
            _lastErrorMessage = IsMandatoryArg ? $"Missing required argument: {ArgumentName}" : null;
            return IsMandatoryArg ? EnumParseResult.Bad : EnumParseResult.Good;
        }

        // Check if it starts with a quote
        if (firstWord.StartsWith('"'))
        {
            // Pop all remaining text to parse the quoted string
            var remaining = rawArgs.PopAll();
            return ParseQuotedString(remaining, rawArgs);
        }

        // No quote - just pop a single word like the standard Word parser
        _parsedValue = rawArgs.PopWord();
        IsMissing = false;
        return EnumParseResult.Good;
    }

    /// <summary>
    /// Parses a quoted string from the input, handling the closing quote and remaining text.
    /// </summary>
    private EnumParseResult ParseQuotedString(string input, CmdArgs rawArgs)
    {
        // Skip the opening quote
        var startIndex = 1;
        var endQuoteIndex = input.IndexOf('"', startIndex);

        if (endQuoteIndex == -1)
        {
            // No closing quote found
            IsMissing = true;
            _parsedValue = null;
            _lastErrorMessage = $"Unclosed quote in {ArgumentName}. Use matching quotes: \"name with spaces\"";
            return EnumParseResult.Bad;
        }

        // Extract the quoted content
        _parsedValue = input.Substring(startIndex, endQuoteIndex - startIndex);
        IsMissing = false;

        // Check if there's remaining text after the closing quote
        var afterQuote = input.Substring(endQuoteIndex + 1).TrimStart();
        if (!string.IsNullOrEmpty(afterQuote))
        {
            // Push remaining tokens back for subsequent parsers
            PushRemainingArgs(afterQuote, rawArgs);
        }

        return EnumParseResult.Good;
    }

    /// <summary>
    /// Tokenizes remaining text and pushes each token back for subsequent parsers.
    /// Tokens are pushed in reverse order so they're popped in the correct order.
    /// </summary>
    private static void PushRemainingArgs(string remaining, CmdArgs rawArgs)
    {
        var tokens = TokenizeRemainingArgs(remaining);
        // Push in reverse order so they're popped in correct order
        for (var i = tokens.Count - 1; i >= 0; i--)
        {
            rawArgs.PushSingle(tokens[i]);
        }
    }

    /// <summary>
    /// Splits input into tokens on whitespace, preserving quoted strings as single tokens.
    /// </summary>
    private static List<string> TokenizeRemainingArgs(string input)
    {
        var tokens = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        foreach (var c in input)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
                current.Append(c);
            }
            else if (char.IsWhiteSpace(c) && !inQuotes)
            {
                if (current.Length > 0)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                }
            }
            else
            {
                current.Append(c);
            }
        }

        if (current.Length > 0)
        {
            tokens.Add(current.ToString());
        }

        return tokens;
    }
}