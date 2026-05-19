using System.Diagnostics.CodeAnalysis;
using DivineAscension.Commands;
using DivineAscension.Models.Enum;

namespace DivineAscension.Tests.Commands;

[ExcludeFromCodeCoverage]
public class CommandHelpersTests
{
    [Fact]
    public void ParseDomainAndPlayer_BothNull_ReturnsBothNull()
    {
        var (d, p) = CommandHelpers.ParseDomainAndPlayer(null, null);
        Assert.Null(d);
        Assert.Null(p);
    }

    [Fact]
    public void ParseDomainAndPlayer_FirstIsDomain_ReturnsDomainOnly()
    {
        var (d, p) = CommandHelpers.ParseDomainAndPlayer("Wild", null);
        Assert.Equal(DeityDomain.Wild, d);
        Assert.Null(p);
    }

    [Fact]
    public void ParseDomainAndPlayer_FirstIsPlayer_ReturnsPlayerOnly()
    {
        var (d, p) = CommandHelpers.ParseDomainAndPlayer("Bob", null);
        Assert.Null(d);
        Assert.Equal("Bob", p);
    }

    [Fact]
    public void ParseDomainAndPlayer_DomainThenPlayer_ParsesBoth()
    {
        var (d, p) = CommandHelpers.ParseDomainAndPlayer("Conquest", "Alice");
        Assert.Equal(DeityDomain.Conquest, d);
        Assert.Equal("Alice", p);
    }

    [Fact]
    public void ParseDomainAndPlayer_PlayerThenDomain_ParsesBoth()
    {
        var (d, p) = CommandHelpers.ParseDomainAndPlayer("Alice", "Stone");
        Assert.Equal(DeityDomain.Stone, d);
        Assert.Equal("Alice", p);
    }

    [Fact]
    public void ParseDomainAndPlayer_CaseInsensitiveDomain()
    {
        var (d, _) = CommandHelpers.ParseDomainAndPlayer("harvest", null);
        Assert.Equal(DeityDomain.Harvest, d);
    }

    [Fact]
    public void ParseDomainAndPlayer_NoneIsRejected()
    {
        var (d, p) = CommandHelpers.ParseDomainAndPlayer("None", null);
        Assert.Null(d);
        Assert.Equal("None", p); // treated as playername
    }
}
