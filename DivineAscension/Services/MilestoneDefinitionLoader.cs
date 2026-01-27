using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using DivineAscension.Models;
using DivineAscension.Services.Interfaces;
using Vintagestory.API.Common;

namespace DivineAscension.Services;

/// <summary>
///     Loads milestone definitions from JSON asset files
/// </summary>
public class MilestoneDefinitionLoader : IMilestoneDefinitionLoader
{
    private const string AssetPath = "config/milestones.json";

    private readonly IAssetManager _assetManager;
    private readonly ILoggerWrapper _logger;

    private readonly Dictionary<string, MilestoneDefinition> _milestonesById = new();
    private readonly List<MilestoneDefinition> _allMilestones = new();
    private readonly List<MilestoneDefinition> _majorMilestones = new();
    private readonly List<MilestoneDefinition> _minorMilestones = new();

    public MilestoneDefinitionLoader(ILoggerWrapper logger, IAssetManager assetManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _assetManager = assetManager ?? throw new ArgumentNullException(nameof(assetManager));
    }

    /// <inheritdoc />
    public void LoadMilestones()
    {
        _milestonesById.Clear();
        _allMilestones.Clear();
        _majorMilestones.Clear();
        _minorMilestones.Clear();

        try
        {
            var asset = _assetManager.Get(new AssetLocation("divineascension", AssetPath));
            if (asset == null)
            {
                _logger.Warning($"[DivineAscension MilestoneLoader] Asset not found: {AssetPath}");
                return;
            }

            var json = Encoding.UTF8.GetString(asset.Data);
            var fileDto = JsonSerializer.Deserialize<MilestoneFileDto>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (fileDto == null)
            {
                _logger.Warning("[DivineAscension MilestoneLoader] Failed to parse milestones.json");
                return;
            }

            foreach (var dto in fileDto.Milestones)
            {
                try
                {
                    var milestone = ConvertToMilestoneDefinition(dto);
                    if (milestone != null)
                    {
                        _milestonesById[milestone.MilestoneId.ToLowerInvariant()] = milestone;
                        _allMilestones.Add(milestone);

                        if (milestone.Type == MilestoneType.Major)
                            _majorMilestones.Add(milestone);
                        else
                            _minorMilestones.Add(milestone);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warning(
                        $"[DivineAscension MilestoneLoader] Failed to convert milestone '{dto.Id}': {ex.Message}");
                }
            }

            _logger.Notification(
                $"[DivineAscension MilestoneLoader] Loaded {_allMilestones.Count} milestones " +
                $"({_majorMilestones.Count} major, {_minorMilestones.Count} minor)");
        }
        catch (Exception ex)
        {
            _logger.Error($"[DivineAscension MilestoneLoader] Failed to load milestones: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public MilestoneDefinition? GetMilestone(string milestoneId)
    {
        if (string.IsNullOrWhiteSpace(milestoneId))
            return null;

        return _milestonesById.TryGetValue(milestoneId.ToLowerInvariant(), out var milestone)
            ? milestone
            : null;
    }

    /// <inheritdoc />
    public IReadOnlyList<MilestoneDefinition> GetAllMilestones() => _allMilestones.AsReadOnly();

    /// <inheritdoc />
    public IReadOnlyList<MilestoneDefinition> GetMajorMilestones() => _majorMilestones.AsReadOnly();

    /// <inheritdoc />
    public IReadOnlyList<MilestoneDefinition> GetMinorMilestones() => _minorMilestones.AsReadOnly();

    private MilestoneDefinition? ConvertToMilestoneDefinition(MilestoneJsonDto dto)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(dto.Id))
        {
            _logger.Warning("[DivineAscension MilestoneLoader] Milestone has empty Id, skipping");
            return null;
        }

        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            _logger.Warning($"[DivineAscension MilestoneLoader] Milestone '{dto.Id}' has empty Name, skipping");
            return null;
        }

        // Parse milestone type
        if (!TryParseMilestoneType(dto.Type, out var milestoneType))
        {
            _logger.Warning(
                $"[DivineAscension MilestoneLoader] Milestone '{dto.Id}' has invalid type '{dto.Type}', skipping");
            return null;
        }

        // Parse trigger
        var trigger = ConvertToTrigger(dto.Trigger, dto.Id);
        if (trigger == null)
            return null;

        // Parse permanent benefit (optional)
        MilestoneBenefit? permanentBenefit = null;
        if (dto.PermanentBenefit != null)
        {
            permanentBenefit = ConvertToPermanentBenefit(dto.PermanentBenefit, dto.Id);
        }

        // Parse temporary benefit (optional)
        MilestoneTemporaryBenefit? temporaryBenefit = null;
        if (dto.TemporaryBenefit != null)
        {
            temporaryBenefit = ConvertToTemporaryBenefit(dto.TemporaryBenefit, dto.Id);
        }

        return new MilestoneDefinition(
            dto.Id,
            dto.Name,
            dto.Description ?? string.Empty,
            milestoneType,
            trigger,
            dto.RankReward,
            dto.PrestigePayout,
            permanentBenefit,
            temporaryBenefit
        );
    }

    private MilestoneTrigger? ConvertToTrigger(MilestoneTriggerDto dto, string milestoneId)
    {
        if (!TryParseTriggerType(dto.Type, out var triggerType))
        {
            _logger.Warning(
                $"[DivineAscension MilestoneLoader] Milestone '{milestoneId}' has invalid trigger type '{dto.Type}', skipping");
            return null;
        }

        if (dto.Threshold <= 0 && triggerType != MilestoneTriggerType.AllMajorMilestones)
        {
            _logger.Warning(
                $"[DivineAscension MilestoneLoader] Milestone '{milestoneId}' has invalid threshold {dto.Threshold}, skipping");
            return null;
        }

        return new MilestoneTrigger(triggerType, dto.Threshold);
    }

    private MilestoneBenefit? ConvertToPermanentBenefit(MilestoneBenefitDto dto, string milestoneId)
    {
        if (!TryParseBenefitType(dto.Type, out var benefitType))
        {
            _logger.Warning(
                $"[DivineAscension MilestoneLoader] Milestone '{milestoneId}' has invalid benefit type '{dto.Type}'");
            return null;
        }

        return new MilestoneBenefit(benefitType, dto.Amount, dto.BlessingId);
    }

    private MilestoneTemporaryBenefit? ConvertToTemporaryBenefit(MilestoneTemporaryBenefitDto dto, string milestoneId)
    {
        if (!TryParseBenefitType(dto.Type, out var benefitType))
        {
            _logger.Warning(
                $"[DivineAscension MilestoneLoader] Milestone '{milestoneId}' has invalid temporary benefit type '{dto.Type}'");
            return null;
        }

        if (dto.DurationDays <= 0)
        {
            _logger.Warning(
                $"[DivineAscension MilestoneLoader] Milestone '{milestoneId}' has invalid duration {dto.DurationDays}");
            return null;
        }

        return new MilestoneTemporaryBenefit(benefitType, dto.Amount, dto.DurationDays);
    }

    private static bool TryParseMilestoneType(string typeStr, out MilestoneType type)
    {
        type = MilestoneType.Major;
        return typeStr?.ToLowerInvariant() switch
        {
            "major" => true,
            "minor" => (type = MilestoneType.Minor) == MilestoneType.Minor,
            _ => false
        };
    }

    private static bool TryParseTriggerType(string typeStr, out MilestoneTriggerType type)
    {
        type = MilestoneTriggerType.ReligionCount;
        return typeStr?.ToLowerInvariant() switch
        {
            "religion_count" => true,
            "domain_count" => (type = MilestoneTriggerType.DomainCount) == type,
            "holy_site_count" => (type = MilestoneTriggerType.HolySiteCount) == type,
            "ritual_count" => (type = MilestoneTriggerType.RitualCount) == type,
            "member_count" => (type = MilestoneTriggerType.MemberCount) == type,
            "war_kill_count" => (type = MilestoneTriggerType.WarKillCount) == type,
            "holy_site_tier" => (type = MilestoneTriggerType.HolySiteTier) == type,
            "diplomatic_relationship" => (type = MilestoneTriggerType.DiplomaticRelationship) == type,
            "all_major_milestones" => (type = MilestoneTriggerType.AllMajorMilestones) == type,
            _ => false
        };
    }

    private static bool TryParseBenefitType(string typeStr, out MilestoneBenefitType type)
    {
        type = MilestoneBenefitType.PrestigeMultiplier;
        return typeStr?.ToLowerInvariant() switch
        {
            "prestige_multiplier" => true,
            "favor_multiplier" => (type = MilestoneBenefitType.FavorMultiplier) == type,
            "conquest_multiplier" => (type = MilestoneBenefitType.ConquestMultiplier) == type,
            "holy_site_slot" => (type = MilestoneBenefitType.HolySiteSlot) == type,
            "unlock_blessing" => (type = MilestoneBenefitType.UnlockBlessing) == type,
            "all_rewards_multiplier" => (type = MilestoneBenefitType.AllRewardsMultiplier) == type,
            _ => false
        };
    }
}
