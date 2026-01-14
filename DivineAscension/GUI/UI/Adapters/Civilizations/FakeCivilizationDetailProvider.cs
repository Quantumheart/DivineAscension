using System;
using System.Collections.Generic;
using System.Linq;

namespace DivineAscension.GUI.UI.Adapters.Civilizations;

/// <summary>
///     Dev-only, UI-only civilization detail provider that generates synthetic civilization details deterministically.
///     Uses adapter pattern via ICivilizationDetailProvider so UIs can swap between real and fake data.
///     Syncs with ICivilizationProvider for consistent base civilization data.
/// </summary>
internal sealed class FakeCivilizationDetailProvider : ICivilizationDetailProvider
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

    private static readonly string[] Domains = { "Craft", "Wild", "Harvest", "Stone" };

    private static readonly string[] ReligionPrefixes =
    {
        "Followers of", "Temple of", "Order of", "Church of",
        "Disciples of", "Sect of", "Faithful of", "Brotherhood of"
    };

    private readonly Dictionary<string, CivilizationDetailVM> _cache = new();

    private readonly ICivilizationProvider _civilizationProvider;

    public FakeCivilizationDetailProvider(ICivilizationProvider civilizationProvider)
    {
        _civilizationProvider = civilizationProvider;
    }

    public CivilizationDetailVM? GetCivilizationDetail(string civId)
    {
        if (string.IsNullOrEmpty(civId)) return null;

        if (_cache.TryGetValue(civId, out var cached))
            return cached;

        // Look up base civilization data from provider
        var civilization = _civilizationProvider.GetCivilizations()
            .FirstOrDefault(c => c.civId == civId);

        if (civilization is null) return null;

        var detail = GenerateDetail(civilization);
        _cache[civId] = detail;
        return detail;
    }

    public void Refresh()
    {
        _cache.Clear();
    }

    private CivilizationDetailVM GenerateDetail(CivilizationVM civilization)
    {
        // Use civId hash for deterministic member generation
        var civHash = civilization.civId.GetHashCode();
        var rnd = new Random(civHash);

        // Generate founder name from founderUID
        var founderHash = civilization.founderUID.GetHashCode();
        var founderRnd = new Random(founderHash);
        var founderName = $"{First[founderRnd.Next(First.Length)]} {Last[founderRnd.Next(Last.Length)]}";

        // Generate founding religion name from founderReligionUID
        var founderReligionHash = civilization.founderReligionUID.GetHashCode();
        var founderReligionRnd = new Random(founderReligionHash);
        var founderReligionName =
            $"{ReligionPrefixes[founderReligionRnd.Next(ReligionPrefixes.Length)]} {Domains[founderReligionRnd.Next(Domains.Length)]}";

        // Generate member religions - use memberCount from civilization
        var memberCount = Math.Min(civilization.memberCount, 4);
        memberCount = Math.Max(memberCount, 1); // At least 1 founding religion
        var members = new List<MemberReligionDetailVM>(memberCount);

        // Shuffle domains to ensure uniqueness
        var availableDomains = Domains.OrderBy(_ => rnd.Next()).Take(memberCount).ToList();

        for (var i = 0; i < memberCount; i++)
        {
            var religionId = GenerateGuid(rnd).ToString("N");
            var domain = availableDomains[i];
            var religionPrefix = ReligionPrefixes[rnd.Next(ReligionPrefixes.Length)];
            var religionName = $"{religionPrefix} {domain}";

            var religionFounderUID = GenerateGuid(rnd).ToString("N");
            var religionFounderName = $"{First[rnd.Next(First.Length)]} {Last[rnd.Next(Last.Length)]}";

            var religionMemberCount = rnd.Next(1, 50);

            // Generate deity name - use domain as base with optional variation
            var deityName = domain;
            if (rnd.Next(2) == 0)
                deityName = $"{First[rnd.Next(First.Length)]}{Last[rnd.Next(Last.Length)]}";

            members.Add(new MemberReligionDetailVM(
                religionId,
                religionName,
                domain,
                religionFounderUID,
                religionFounderName,
                religionMemberCount,
                deityName
            ));
        }

        // Generate created date (1-365 days ago)
        var daysAgo = rnd.Next(1, 365);
        var createdDate = DateTime.UtcNow.AddDays(-daysAgo);

        // Generate description
        var description = civilization.name != "The Lone Order of the Hermit" &&
                          civilization.name != "The Great Unity of All Faiths"
            ? $"A {(memberCount == 1 ? "solitary" : "united")} civilization founded {daysAgo} days ago, " +
              $"bringing together {memberCount} {(memberCount == 1 ? "religion" : "religions")} under common purpose."
            : string.Empty;

        return new CivilizationDetailVM(
            civilization.civId,
            civilization.name,
            civilization.founderUID,
            founderName,
            civilization.founderReligionUID,
            founderReligionName,
            members,
            createdDate,
            civilization.icon,
            description
        );
    }

    private static Guid GenerateGuid(Random rnd)
    {
        var bytes = new byte[16];
        rnd.NextBytes(bytes);
        return new Guid(bytes);
    }
}