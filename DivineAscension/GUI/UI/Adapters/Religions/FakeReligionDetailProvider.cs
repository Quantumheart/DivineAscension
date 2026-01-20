using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace DivineAscension.GUI.UI.Adapters.Religions;

/// <summary>
///     Dev-only, UI-only religion detail provider that generates synthetic religion details deterministically.
///     Uses adapter pattern via IReligionDetailProvider so UIs can swap between real and fake data.
///     Syncs with IReligionProvider for consistent base religion data.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class FakeReligionDetailProvider : IReligionDetailProvider
{
    private static readonly string[] First =
    {
        "Ari", "Kai", "Niko", "Mira", "Sora", "Lena", "Theo", "Rin", "Eli", "Nova",
        "Ira", "Juno", "Orin", "Zara", "Kade", "Vera", "Tess", "Vale", "Rhea", "Cai"
    };

    private static readonly string[] Last =
    {
        "Stone", "Reed", "Ash", "Vale", "Rook", "Quill", "Frost", "Wilde", "Grove", "Bluff",
        "Kestrel", "March", "Dawn", "Flint", "Hollow", "Strand", "Rowan", "Lark", "Moss", "Ever"
    };

    private static readonly string[] FavorRanks =
        { "Initiate", "Acolyte", "Devotee", "Zealot", "Champion", "Avatar" };

    private readonly Dictionary<string, ReligionDetailVM> _cache = new();

    private readonly IReligionProvider _religionProvider;

    public FakeReligionDetailProvider(IReligionProvider religionProvider)
    {
        _religionProvider = religionProvider;
    }

    public ReligionDetailVM? GetReligionDetail(string religionUID)
    {
        if (string.IsNullOrEmpty(religionUID)) return null;

        if (_cache.TryGetValue(religionUID, out var cached))
            return cached;

        // Look up base religion data from provider
        var religion = _religionProvider.GetReligions()
            .FirstOrDefault(r => r.religionUID == religionUID);

        if (religion is null) return null;

        var detail = GenerateDetail(religion);
        _cache[religionUID] = detail;
        return detail;
    }

    public void Refresh()
    {
        _cache.Clear();
    }

    private ReligionDetailVM GenerateDetail(ReligionVM religion)
    {
        // Use religionUID hash for deterministic member generation
        var uidHash = religion.religionUID.GetHashCode();
        var rnd = new Random(uidHash);

        // Generate founder name from founderUID
        var founderHash = religion.founderUID.GetHashCode();
        var founderRnd = new Random(founderHash);
        var founderName = $"{First[founderRnd.Next(First.Length)]} {Last[founderRnd.Next(Last.Length)]}";

        // Generate members - use memberCount from religion but cap at reasonable number for detail view
        var memberCount = Math.Min(religion.memberCount, 50);
        memberCount = Math.Max(memberCount, 1); // At least founder
        var members = new List<MemberDetailVM>(memberCount);

        for (var i = 0; i < memberCount; i++)
        {
            var playerUID = GenerateGuid(rnd).ToString("N");
            var playerName = $"{First[rnd.Next(First.Length)]} {Last[rnd.Next(Last.Length)]}";
            var favorRank = FavorRanks[rnd.Next(FavorRanks.Length)];
            var favor = rnd.Next(0, 5000);
            members.Add(new MemberDetailVM(playerUID, playerName, favorRank, favor));
        }

        return new ReligionDetailVM(
            religion.religionUID,
            religion.religionName,
            religion.deity,
            religion.deityName,
            religion.description,
            religion.prestige,
            religion.prestigeRank,
            religion.isPublic,
            religion.founderUID,
            founderName,
            members
        );
    }

    private static Guid GenerateGuid(Random rnd)
    {
        var bytes = new byte[16];
        rnd.NextBytes(bytes);
        return new Guid(bytes);
    }
}