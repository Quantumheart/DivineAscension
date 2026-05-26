using System;
using System.Diagnostics.CodeAnalysis;
using DivineAscension.GUI.Models.Religion.Invites;
using DivineAscension.Models.Enum;

namespace DivineAscension.Tests.GUI.Models.Religion.Invites;

/// <summary>
/// Unit tests for <see cref="InviteData.BuildQuoteLine" />.
/// </summary>
[ExcludeFromCodeCoverage]
public class InviteDataTests
{
    private const string DefaultQuote = "\"They would have you among them.\"";

    private static InviteData Invite(string description) =>
        new("id", "Order of the Forge", DateTime.UtcNow, DeityDomain.Craft, description);

    [Fact]
    public void BuildQuoteLine_EmptyDescription_ReturnsDefault()
    {
        Assert.Equal(DefaultQuote, Invite("").BuildQuoteLine(DefaultQuote));
    }

    [Fact]
    public void BuildQuoteLine_WhitespaceDescription_ReturnsDefault()
    {
        Assert.Equal(DefaultQuote, Invite("   \n\t ").BuildQuoteLine(DefaultQuote));
    }

    [Fact]
    public void BuildQuoteLine_NonEmptyDescription_WrapsInQuotes()
    {
        Assert.Equal("\"Walk with us.\"", Invite("Walk with us.").BuildQuoteLine(DefaultQuote));
    }

    [Fact]
    public void BuildQuoteLine_CollapsesWhitespaceAndNewlines()
    {
        Assert.Equal("\"The wild remembers.\"",
            Invite("The   wild\nremembers.").BuildQuoteLine(DefaultQuote));
    }

    [Fact]
    public void BuildQuoteLine_LongDescription_TruncatesWithEllipsis()
    {
        var description = new string('a', 250);

        var result = Invite(description).BuildQuoteLine(DefaultQuote);

        Assert.StartsWith("\"", result);
        Assert.EndsWith("…\"", result);
        // 100 chars + surrounding quotes + ellipsis.
        Assert.Equal(100 + 3, result.Length);
    }
}
