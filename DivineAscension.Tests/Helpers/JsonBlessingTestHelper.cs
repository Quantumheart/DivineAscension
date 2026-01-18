using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using DivineAscension.Models;
using DivineAscension.Models.Dto;
using DivineAscension.Models.Enum;

namespace DivineAscension.Tests.Helpers;

/// <summary>
///     Helper for loading blessing definitions from JSON files in tests.
///     Loads directly from the project's assets directory.
/// </summary>
[ExcludeFromCodeCoverage]
public static class JsonBlessingTestHelper
{
    private static List<Blessing>? _cachedBlessings;

    /// <summary>
    ///     Gets all blessings loaded from JSON files.
    /// </summary>
    public static List<Blessing> GetAllBlessings()
    {
        if (_cachedBlessings != null)
            return _cachedBlessings;

        _cachedBlessings = new List<Blessing>();
        var domains = new[] { "craft", "wild", "conquest", "harvest", "stone" };

        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var blessingsPath = Path.Combine(projectRoot, "DivineAscension", "assets", "divineascension", "config", "blessings");

        foreach (var domain in domains)
        {
            var filePath = Path.Combine(blessingsPath, $"{domain}.json");
            if (!File.Exists(filePath))
                continue;

            var json = File.ReadAllText(filePath);
            var fileDto = JsonSerializer.Deserialize<BlessingFileDto>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (fileDto == null)
                continue;

            if (!Enum.TryParse<DeityDomain>(fileDto.Domain, true, out var deityDomain))
                continue;

            foreach (var dto in fileDto.Blessings)
            {
                var blessing = ConvertToBlessing(dto, deityDomain);
                if (blessing != null)
                    _cachedBlessings.Add(blessing);
            }
        }

        return _cachedBlessings;
    }

    private static Blessing? ConvertToBlessing(BlessingJsonDto dto, DeityDomain domain)
    {
        if (string.IsNullOrWhiteSpace(dto.BlessingId) || string.IsNullOrWhiteSpace(dto.Name))
            return null;

        if (!Enum.TryParse<BlessingKind>(dto.Kind, true, out var kind))
            return null;

        Enum.TryParse<BlessingCategory>(dto.Category, true, out var category);

        return new Blessing(dto.BlessingId, dto.Name, domain)
        {
            Description = dto.Description ?? string.Empty,
            Kind = kind,
            Category = category,
            IconName = dto.IconName ?? string.Empty,
            RequiredFavorRank = dto.RequiredFavorRank,
            RequiredPrestigeRank = dto.RequiredPrestigeRank,
            PrerequisiteBlessings = dto.PrerequisiteBlessings ?? new List<string>(),
            StatModifiers = dto.StatModifiers ?? new Dictionary<string, float>(),
            SpecialEffects = dto.SpecialEffects ?? new List<string>()
        };
    }

    /// <summary>
    ///     Clears the cached blessings to force a reload.
    /// </summary>
    public static void ClearCache()
    {
        _cachedBlessings = null;
    }
}
