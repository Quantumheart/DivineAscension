using System;
using DivineAscension.Models;
using DivineAscension.Services;
using DivineAscension.Systems.Interfaces;

namespace DivineAscension.Systems;

/// <summary>
///     Provides civilization-wide bonus multipliers for favor and prestige calculations.
///     Acts as a facade over CivilizationMilestoneManager for simpler integration.
/// </summary>
public class CivilizationBonusSystem : ICivilizationBonusSystem
{
    private readonly ILoggerWrapper _logger;
    private readonly ICivilizationManager _civilizationManager;
    private readonly ICivilizationMilestoneManager _milestoneManager;
    private readonly IReligionManager _religionManager;

    public CivilizationBonusSystem(
        ILoggerWrapper logger,
        ICivilizationManager civilizationManager,
        ICivilizationMilestoneManager milestoneManager,
        IReligionManager religionManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _civilizationManager = civilizationManager ?? throw new ArgumentNullException(nameof(civilizationManager));
        _milestoneManager = milestoneManager ?? throw new ArgumentNullException(nameof(milestoneManager));
        _religionManager = religionManager ?? throw new ArgumentNullException(nameof(religionManager));
    }

    /// <inheritdoc />
    public float GetFavorMultiplier(string civId)
    {
        if (string.IsNullOrEmpty(civId))
            return 1.0f;

        return _milestoneManager.GetActiveBonuses(civId).FavorMultiplier;
    }

    /// <inheritdoc />
    public float GetPrestigeMultiplier(string civId)
    {
        if (string.IsNullOrEmpty(civId))
            return 1.0f;

        return _milestoneManager.GetActiveBonuses(civId).PrestigeMultiplier;
    }

    /// <inheritdoc />
    public float GetConquestMultiplier(string civId)
    {
        if (string.IsNullOrEmpty(civId))
            return 1.0f;

        return _milestoneManager.GetActiveBonuses(civId).ConquestMultiplier;
    }

    /// <inheritdoc />
    public CivilizationBonuses GetAllBonuses(string civId)
    {
        if (string.IsNullOrEmpty(civId))
            return CivilizationBonuses.None;

        return _milestoneManager.GetActiveBonuses(civId);
    }

    /// <inheritdoc />
    public float GetFavorMultiplierForPlayer(string playerUID)
    {
        var civId = GetPlayerCivilizationId(playerUID);
        return GetFavorMultiplier(civId);
    }

    /// <inheritdoc />
    public float GetPrestigeMultiplierForPlayer(string playerUID)
    {
        var civId = GetPlayerCivilizationId(playerUID);
        return GetPrestigeMultiplier(civId);
    }

    /// <inheritdoc />
    public int GetBonusHolySiteSlotsForReligion(string religionUID)
    {
        if (string.IsNullOrEmpty(religionUID))
            return 0;

        var civ = _civilizationManager.GetCivilizationByReligion(religionUID);
        if (civ == null)
            return 0;

        return _milestoneManager.GetBonusHolySiteSlots(civ.CivId);
    }

    private string GetPlayerCivilizationId(string playerUID)
    {
        if (string.IsNullOrEmpty(playerUID))
            return string.Empty;

        var religion = _religionManager.GetPlayerReligion(playerUID);
        if (religion == null)
            return string.Empty;

        var civ = _civilizationManager.GetCivilizationByReligion(religion.ReligionUID);
        return civ?.CivId ?? string.Empty;
    }
}
