using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using DivineAscension.Models;
using DivineAscension.Models.Enum;
using DivineAscension.Services.Interfaces;
using Vintagestory.API.Common;

namespace DivineAscension.Services;

/// <summary>
/// Loads ritual definitions from JSON asset files.
/// Uses focused dependencies (ILogger, IAssetManager) for better testability.
/// </summary>
public class RitualLoader(ILogger logger, IAssetManager assetManager) : IRitualLoader
{
    private static readonly string[] DomainFiles = { "craft", "wild", "conquest", "harvest", "stone" };

    private readonly IAssetManager
        _assetManager = assetManager ?? throw new ArgumentNullException(nameof(assetManager));

    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly Dictionary<DeityDomain, List<Ritual>> _ritualsByDomain = new();
    private readonly Dictionary<string, Ritual> _ritualsById = new();
    private readonly Dictionary<string, Ritual> _ritualsByTierUpgrade = new();

    /// <summary>
    /// Loads all ritual configurations from JSON files.
    /// Should be called during mod initialization.
    /// </summary>
    public void LoadRituals()
    {
        _ritualsByDomain.Clear();
        _ritualsById.Clear();
        _ritualsByTierUpgrade.Clear();

        var totalLoaded = 0;
        var filesLoaded = 0;

        foreach (var domainFile in DomainFiles)
        {
            try
            {
                var ritualsFromFile = LoadRitualsFromFile(domainFile);
                if (ritualsFromFile.Count > 0)
                {
                    totalLoaded += ritualsFromFile.Count;
                    filesLoaded++;
                    _logger.Debug(
                        $"[DivineAscension RitualLoader] Loaded {ritualsFromFile.Count} rituals from {domainFile}.json");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(
                    $"[DivineAscension RitualLoader] Failed to load rituals from {domainFile}.json: {ex.Message}");
            }
        }

        if (filesLoaded > 0)
        {
            _logger.Notification(
                $"[DivineAscension RitualLoader] Successfully loaded {totalLoaded} rituals from {filesLoaded} files");
        }
        else
        {
            _logger.Warning(
                "[DivineAscension RitualLoader] No ritual files were loaded. Holy site tier upgrades will not be available.");
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<Ritual> GetRitualsForDomain(DeityDomain domain)
    {
        return _ritualsByDomain.TryGetValue(domain, out var rituals)
            ? rituals.AsReadOnly()
            : Array.Empty<Ritual>();
    }

    /// <inheritdoc />
    public Ritual? GetRitualById(string ritualId)
    {
        if (string.IsNullOrWhiteSpace(ritualId))
            return null;

        return _ritualsById.TryGetValue(ritualId.ToLowerInvariant(), out var ritual) ? ritual : null;
    }

    /// <inheritdoc />
    public Ritual? GetRitualForTierUpgrade(DeityDomain domain, int sourceTier, int targetTier)
    {
        // Build lookup key: domain + sourceTier + targetTier
        var lookupKey = $"{domain}:{sourceTier}->{targetTier}";
        return _ritualsByTierUpgrade.TryGetValue(lookupKey, out var ritual) ? ritual : null;
    }

    /// <summary>
    /// Loads rituals from a specific domain JSON file.
    /// </summary>
    private List<Ritual> LoadRitualsFromFile(string domainFileName)
    {
        var rituals = new List<Ritual>();
        var assetPath = $"config/rituals/{domainFileName}.json";

        var asset = _assetManager.Get(new AssetLocation("divineascension", assetPath));
        if (asset == null)
        {
            _logger.Debug($"[DivineAscension RitualLoader] Asset not found: {assetPath}");
            return rituals;
        }

        var json = Encoding.UTF8.GetString(asset.Data);
        var fileDto = JsonSerializer.Deserialize<RitualFileDto>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (fileDto == null)
        {
            _logger.Warning($"[DivineAscension RitualLoader] Failed to parse {domainFileName}.json");
            return rituals;
        }

        // Validate and convert domain
        if (!TryParseDomain(fileDto.Domain, out var domain))
        {
            _logger.Error(
                $"[DivineAscension RitualLoader] Invalid domain '{fileDto.Domain}' in {domainFileName}.json");
            return rituals;
        }

        foreach (var dto in fileDto.Rituals)
        {
            try
            {
                var ritual = ConvertToRitual(dto, domain);
                if (ritual != null)
                {
                    rituals.Add(ritual);

                    // Index by domain
                    if (!_ritualsByDomain.ContainsKey(domain))
                        _ritualsByDomain[domain] = new List<Ritual>();
                    _ritualsByDomain[domain].Add(ritual);

                    // Index by ritual ID (case-insensitive)
                    var idKey = ritual.RitualId.ToLowerInvariant();
                    _ritualsById[idKey] = ritual;

                    // Index by tier upgrade lookup key
                    var tierKey = $"{domain}:{ritual.SourceTier}->{ritual.TargetTier}";
                    _ritualsByTierUpgrade[tierKey] = ritual;
                }
            }
            catch (Exception ex)
            {
                _logger.Warning(
                    $"[DivineAscension RitualLoader] Failed to convert ritual '{dto.Name}': {ex.Message}");
            }
        }

        return rituals;
    }

    /// <summary>
    /// Converts a RitualJsonDto to a Ritual model.
    /// </summary>
    private Ritual? ConvertToRitual(RitualJsonDto dto, DeityDomain domain)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(dto.RitualId))
        {
            _logger.Warning("[DivineAscension RitualLoader] Ritual has empty RitualId, skipping");
            return null;
        }

        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            _logger.Warning($"[DivineAscension RitualLoader] Ritual '{dto.RitualId}' has empty Name, skipping");
            return null;
        }

        // Validate source tier (1-2)
        if (dto.SourceTier < 1 || dto.SourceTier > 2)
        {
            _logger.Warning(
                $"[DivineAscension RitualLoader] Ritual '{dto.RitualId}' has invalid sourceTier {dto.SourceTier} (must be 1-2), skipping");
            return null;
        }

        // Validate target tier (2-3)
        if (dto.TargetTier < 2 || dto.TargetTier > 3)
        {
            _logger.Warning(
                $"[DivineAscension RitualLoader] Ritual '{dto.RitualId}' has invalid targetTier {dto.TargetTier} (must be 2-3), skipping");
            return null;
        }

        // Validate tier progression (target must be exactly source + 1)
        if (dto.TargetTier != dto.SourceTier + 1)
        {
            _logger.Warning(
                $"[DivineAscension RitualLoader] Ritual '{dto.RitualId}' has invalid tier progression {dto.SourceTier} -> {dto.TargetTier} (must upgrade by 1), skipping");
            return null;
        }

        // Validate requirements
        if (dto.Requirements == null || dto.Requirements.Count == 0)
        {
            _logger.Warning(
                $"[DivineAscension RitualLoader] Ritual '{dto.RitualId}' has no requirements, skipping");
            return null;
        }

        // Convert requirements
        var requirements = new List<RitualRequirement>();
        foreach (var reqDto in dto.Requirements)
        {
            var requirement = ConvertToRitualRequirement(reqDto, dto.RitualId);
            if (requirement != null)
            {
                requirements.Add(requirement);
            }
        }

        if (requirements.Count == 0)
        {
            _logger.Warning(
                $"[DivineAscension RitualLoader] Ritual '{dto.RitualId}' has no valid requirements, skipping");
            return null;
        }

        var ritual = new Ritual(
            dto.RitualId,
            dto.Name,
            domain,
            dto.SourceTier,
            dto.TargetTier,
            requirements.AsReadOnly(),
            dto.Description ?? string.Empty
        );

        return ritual;
    }

    /// <summary>
    /// Converts a RitualRequirementJsonDto to a RitualRequirement model.
    /// </summary>
    private RitualRequirement? ConvertToRitualRequirement(RitualRequirementJsonDto dto, string ritualId)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(dto.RequirementId))
        {
            _logger.Warning(
                $"[DivineAscension RitualLoader] Requirement in ritual '{ritualId}' has empty RequirementId, skipping");
            return null;
        }

        if (string.IsNullOrWhiteSpace(dto.DisplayName))
        {
            _logger.Warning(
                $"[DivineAscension RitualLoader] Requirement '{dto.RequirementId}' in ritual '{ritualId}' has empty DisplayName, skipping");
            return null;
        }

        // Validate quantity (must be positive)
        if (dto.Quantity <= 0)
        {
            _logger.Warning(
                $"[DivineAscension RitualLoader] Requirement '{dto.RequirementId}' in ritual '{ritualId}' has invalid quantity {dto.Quantity}, skipping");
            return null;
        }

        // Validate type
        if (!Enum.TryParse<RequirementType>(dto.Type, ignoreCase: true, out var requirementType))
        {
            _logger.Warning(
                $"[DivineAscension RitualLoader] Requirement '{dto.RequirementId}' in ritual '{ritualId}' has invalid type '{dto.Type}', skipping");
            return null;
        }

        // Validate item codes
        if (dto.ItemCodes == null || dto.ItemCodes.Count == 0)
        {
            _logger.Warning(
                $"[DivineAscension RitualLoader] Requirement '{dto.RequirementId}' in ritual '{ritualId}' has no item codes, skipping");
            return null;
        }

        var requirement = new RitualRequirement(
            dto.RequirementId,
            dto.DisplayName,
            dto.Quantity,
            requirementType,
            dto.ItemCodes.AsReadOnly()
        );

        return requirement;
    }

    /// <summary>
    /// Tries to parse a domain string to DeityDomain enum.
    /// </summary>
    private static bool TryParseDomain(string domainStr, out DeityDomain domain)
    {
        return Enum.TryParse(domainStr, ignoreCase: true, out domain);
    }
}
