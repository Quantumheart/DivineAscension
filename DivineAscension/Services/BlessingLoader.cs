using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using DivineAscension.Models;
using DivineAscension.Models.Dto;
using DivineAscension.Models.Enum;
using DivineAscension.Services.Interfaces;
using Vintagestory.API.Common;

namespace DivineAscension.Services;

/// <summary>
///     Loads blessing definitions from JSON asset files.
///     Follows the LocalizationService pattern for asset loading.
/// </summary>
public class BlessingLoader : IBlessingLoader
{
    private static readonly string[] DomainFiles = { "craft", "wild", "conquest", "harvest", "stone" };

    private readonly ICoreAPI _api;
    private readonly List<Blessing> _loadedBlessings = new();

    public BlessingLoader(ICoreAPI api)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
    }

    /// <inheritdoc />
    public bool LoadedSuccessfully { get; private set; }

    /// <inheritdoc />
    public int LoadedCount => _loadedBlessings.Count;

    /// <inheritdoc />
    public List<Blessing> LoadBlessings()
    {
        _loadedBlessings.Clear();
        LoadedSuccessfully = false;

        var totalLoaded = 0;
        var filesLoaded = 0;

        foreach (var domainFile in DomainFiles)
        {
            try
            {
                var blessingsFromFile = LoadBlessingsFromFile(domainFile);
                if (blessingsFromFile.Count > 0)
                {
                    _loadedBlessings.AddRange(blessingsFromFile);
                    totalLoaded += blessingsFromFile.Count;
                    filesLoaded++;
                    _api.Logger.Debug(
                        $"[DivineAscension BlessingLoader] Loaded {blessingsFromFile.Count} blessings from {domainFile}.json");
                }
            }
            catch (Exception ex)
            {
                _api.Logger.Error(
                    $"[DivineAscension BlessingLoader] Failed to load blessings from {domainFile}.json: {ex.Message}");
            }
        }

        if (filesLoaded > 0)
        {
            LoadedSuccessfully = true;
            _api.Logger.Notification(
                $"[DivineAscension BlessingLoader] Successfully loaded {totalLoaded} blessings from {filesLoaded} files");
        }
        else
        {
            _api.Logger.Warning(
                "[DivineAscension BlessingLoader] No blessing files were loaded. Fallback to hardcoded definitions may be needed.");
        }

        return new List<Blessing>(_loadedBlessings);
    }

    /// <summary>
    ///     Loads blessings from a specific domain JSON file.
    /// </summary>
    private List<Blessing> LoadBlessingsFromFile(string domainFileName)
    {
        var blessings = new List<Blessing>();
        var assetPath = $"config/blessings/{domainFileName}.json";

        var asset = _api.Assets.Get(new AssetLocation("divineascension", assetPath));
        if (asset == null)
        {
            _api.Logger.Debug($"[DivineAscension BlessingLoader] Asset not found: {assetPath}");
            return blessings;
        }

        var json = Encoding.UTF8.GetString(asset.Data);
        var fileDto = JsonSerializer.Deserialize<BlessingFileDto>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (fileDto == null)
        {
            _api.Logger.Warning($"[DivineAscension BlessingLoader] Failed to parse {domainFileName}.json");
            return blessings;
        }

        // Validate and convert domain
        if (!TryParseDomain(fileDto.Domain, out var domain))
        {
            _api.Logger.Error(
                $"[DivineAscension BlessingLoader] Invalid domain '{fileDto.Domain}' in {domainFileName}.json");
            return blessings;
        }

        foreach (var dto in fileDto.Blessings)
        {
            try
            {
                var blessing = ConvertToBlessing(dto, domain);
                if (blessing != null)
                {
                    blessings.Add(blessing);
                }
            }
            catch (Exception ex)
            {
                _api.Logger.Warning(
                    $"[DivineAscension BlessingLoader] Failed to convert blessing '{dto.BlessingId}': {ex.Message}");
            }
        }

        return blessings;
    }

    /// <summary>
    ///     Converts a BlessingJsonDto to a Blessing model.
    /// </summary>
    private Blessing? ConvertToBlessing(BlessingJsonDto dto, DeityDomain domain)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(dto.BlessingId))
        {
            _api.Logger.Warning("[DivineAscension BlessingLoader] Blessing has empty BlessingId, skipping");
            return null;
        }

        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            _api.Logger.Warning($"[DivineAscension BlessingLoader] Blessing '{dto.BlessingId}' has empty Name, skipping");
            return null;
        }

        // Parse Kind enum
        if (!TryParseKind(dto.Kind, out var kind))
        {
            _api.Logger.Warning(
                $"[DivineAscension BlessingLoader] Blessing '{dto.BlessingId}' has invalid Kind '{dto.Kind}', skipping");
            return null;
        }

        // Parse Category enum
        if (!TryParseCategory(dto.Category, out var category))
        {
            _api.Logger.Warning(
                $"[DivineAscension BlessingLoader] Blessing '{dto.BlessingId}' has invalid Category '{dto.Category}', using Utility as default");
            category = BlessingCategory.Utility;
        }

        var blessing = new Blessing(dto.BlessingId, dto.Name, domain)
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

        // Log warning for unknown stat keys (but still include them)
        if (dto.StatModifiers != null)
        {
            foreach (var key in dto.StatModifiers.Keys)
            {
                if (!IsKnownStatKey(key))
                {
                    _api.Logger.Debug(
                        $"[DivineAscension BlessingLoader] Blessing '{dto.BlessingId}' has unknown stat key '{key}'");
                }
            }
        }

        return blessing;
    }

    /// <summary>
    ///     Tries to parse a domain string to DeityDomain enum.
    /// </summary>
    private static bool TryParseDomain(string domainStr, out DeityDomain domain)
    {
        return Enum.TryParse(domainStr, ignoreCase: true, out domain);
    }

    /// <summary>
    ///     Tries to parse a kind string to BlessingKind enum.
    /// </summary>
    private static bool TryParseKind(string kindStr, out BlessingKind kind)
    {
        return Enum.TryParse(kindStr, ignoreCase: true, out kind);
    }

    /// <summary>
    ///     Tries to parse a category string to BlessingCategory enum.
    /// </summary>
    private static bool TryParseCategory(string categoryStr, out BlessingCategory category)
    {
        return Enum.TryParse(categoryStr, ignoreCase: true, out category);
    }

    /// <summary>
    ///     Checks if a stat key is known in VintageStoryStats.
    ///     This is a best-effort check using reflection.
    /// </summary>
    private static bool IsKnownStatKey(string key)
    {
        // Check against known stat keys from VintageStoryStats
        // This uses a HashSet for O(1) lookup
        return KnownStatKeys.Contains(key);
    }

    /// <summary>
    ///     Set of all known stat keys from VintageStoryStats for validation.
    /// </summary>
    private static readonly HashSet<string> KnownStatKeys = new()
    {
        // Combat Stats
        "meleeWeaponsDamage",
        "rangedWeaponsDamage",
        "meleeWeaponsSpeed",
        "rangedWeaponsAcc",
        "rangedWeaponsRange",

        // War Stats
        "killHealthRestore",
        "damageReduction",
        "criticalHitChance",
        "criticalHitDamage",

        // Defense Stats
        "meleeWeaponArmor",
        "maxhealthExtraPoints",
        "maxhealthExtraMultiplier",
        "armorEffectiveness",

        // Movement Stats
        "walkspeed",

        // Utility Stats
        "healingeffectivness",

        // Craft Stats
        "toolDurability",
        "oreDropRate",
        "coldResistance",
        "miningSpeedMul",
        "repairCostReduction",
        "repairEfficiency",
        "smithingCostReduction",
        "metalArmorBonus",
        "hungerrate",
        "armorDurabilityLoss",
        "armorWalkSpeedAffectedness",

        // Wild Stats
        "doubleHarvestChance",
        "animalDamage",
        "animalLootDropRate",
        "forageDropRate",
        "foodSpoilage",
        "satiety",
        "temperatureResistance",
        "animalHarvestingTime",
        "foragingYield",

        // Harvest Stats
        "cropYield",
        "seedDropChance",
        "cookingYield",
        "heatResistance",
        "rareCropChance",
        "wildCropDropRate",
        "cookedFoodSatiety",

        // Stone Stats
        "stoneYield",
        "clayYield",
        "clayFormingVoxelChance",
        "potteryBatchCompletionChance",
        "storageVesselCapacity",
        "diggingSpeed",
        "pickDurability",
        "fallDamageReduction",
        "rareStoneChance",
        "oreInStoneChance",
        "gravelYield",

        // Other
        "animalSeekingRange",

        // CombatOverhaul Stats
        "meleeDamageTierBonusSlashingAttack",
        "meleeDamageTierBonusPiercingAttack",
        "meleeDamageTierBonusBluntAttack",
        "rangedDamageTierBonusSlashingAttack",
        "rangedDamageTierBonusPiercingAttack",
        "rangedDamageTierBonusBluntAttack",
        "armorManipulationSpeedAffectedness",
        "armorHungerRateAffectedness",
        "manipulationSpeed",
        "steadyAim",
        "mechanicalsDamage",
        "playerHeadDamageFactor",
        "playerFaceDamageFactor",
        "playerNeckDamageFactor",
        "playerTorsoDamageFactor",
        "playerArmsDamageFactor",
        "playerLegsDamageFactor",
        "playerHandsDamageFactor",
        "playerFeetDamageFactor",
        "bowsProficiency",
        "crossbowsProficiency",
        "firearmsProficiency",
        "oneHandedSwordsProficiency",
        "twoHandedSwordsProficiency",
        "spearsProficiency",
        "javelinsProficiency",
        "macesProficiency",
        "clubsProficiency",
        "halberdsProficiency",
        "axesProficiency",
        "quarterstaffProficiency",
        "slingsProficiency",
        "secondChanceCooldown",
        "secondChanceGracePeriod"
    };
}
