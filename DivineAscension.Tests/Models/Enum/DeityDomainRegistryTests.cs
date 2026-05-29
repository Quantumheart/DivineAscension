using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DivineAscension.Models.Enum;

namespace DivineAscension.Tests.Models.Enum;

/// <summary>
///     Guard tests for the single-source-of-truth domain enumeration + metadata
///     registry (#558). The <see cref="Registry_CoversEveryRealDomain"/> test is
///     the load-bearing one: it fails CI the moment a new <see cref="DeityDomain"/>
///     value is added without a matching <see cref="DeityDomainRegistry"/> entry,
///     before the "everything shows Unknown" bug can ship.
/// </summary>
[ExcludeFromCodeCoverage]
public class DeityDomainRegistryTests
{
    [Fact]
    public void All_ExcludesNone_AndCoversEveryEnumValue()
    {
        Assert.DoesNotContain(DeityDomain.None, DeityDomains.All);

        var expected = System.Enum.GetValues<DeityDomain>().Where(d => d != DeityDomain.None);
        Assert.Equal(expected, DeityDomains.All);
    }

    [Fact]
    public void AllCodes_AreLowercaseAndParallelToAll()
    {
        Assert.Equal(DeityDomains.All.Count, DeityDomains.AllCodes.Count);
        for (var i = 0; i < DeityDomains.All.Count; i++)
            Assert.Equal(DeityDomains.All[i].ToString().ToLowerInvariant(), DeityDomains.AllCodes[i]);
    }

    [Fact]
    public void Selectable_IsSubsetOfAll()
    {
        Assert.All(DeityDomains.Selectable, d => Assert.Contains(d, DeityDomains.All));
    }

    [Fact]
    public void Registry_CoversEveryRealDomain()
    {
        foreach (var domain in DeityDomains.All)
        {
            // Must not throw — a real domain without an entry is the bug this guards.
            var meta = DeityDomainRegistry.Get(domain);
            Assert.Equal(domain, meta.Domain);
        }

        Assert.Equal(DeityDomains.All.Count, DeityDomainRegistry.All.Count);
    }

    [Fact]
    public void Registry_EntriesAreFullyPopulated()
    {
        foreach (var meta in DeityDomainRegistry.All)
        {
            Assert.False(string.IsNullOrWhiteSpace(meta.ShortCode), $"{meta.Domain} ShortCode");
            Assert.False(string.IsNullOrWhiteSpace(meta.NameLocKey), $"{meta.Domain} NameLocKey");
            Assert.False(string.IsNullOrWhiteSpace(meta.TitleLocKey), $"{meta.Domain} TitleLocKey");
            Assert.False(string.IsNullOrWhiteSpace(meta.DescriptionLocKey), $"{meta.Domain} DescriptionLocKey");
            Assert.False(string.IsNullOrWhiteSpace(meta.EpithetLocKey), $"{meta.Domain} EpithetLocKey");
            Assert.False(string.IsNullOrWhiteSpace(meta.FeastPatronLocKey), $"{meta.Domain} FeastPatronLocKey");
            Assert.NotEmpty(meta.PrestigeActivityKeywords);
            Assert.InRange(meta.HolyDay.Month, 1, 12);
            Assert.True(meta.HolyDay.Day >= 1, $"{meta.Domain} HolyDay.Day");
        }
    }

    [Fact]
    public void ShortCodes_AreUnique()
    {
        var codes = DeityDomainRegistry.All.Select(m => m.ShortCode).ToList();
        Assert.Equal(codes.Count, codes.Distinct().Count());
    }

    [Fact]
    public void Get_ThrowsForNone()
    {
        Assert.Throws<ArgumentException>(() => DeityDomainRegistry.Get(DeityDomain.None));
    }

    [Fact]
    public void TryGet_FalseForNone()
    {
        Assert.False(DeityDomainRegistry.TryGet(DeityDomain.None, out _));
    }
}
