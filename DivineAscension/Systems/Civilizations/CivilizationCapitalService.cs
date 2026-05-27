using System;
using DivineAscension.Data;
using DivineAscension.Services;
using DivineAscension.Systems.Interfaces;

namespace DivineAscension.Systems.Civilizations;

/// <summary>
///     Owns the civilization capital: the founder-set display name plus the optional
///     binding to a member religion's holy site, and the cascades that null that
///     binding when the site is removed or its owning religion leaves the civ.
///     Lock-free — the facade serializes all access.
/// </summary>
internal sealed class CivilizationCapitalService
{
    private readonly ILoggerWrapper _logger;

    // Wired after construction to break the HolySiteManager <-> CivilizationManager
    // circular init order. Used only for capital binding validation and cascades.
    private IHolySiteManager? _holySiteManager;

    public CivilizationCapitalService(ILoggerWrapper logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void SetHolySiteManager(IHolySiteManager holySiteManager)
    {
        _holySiteManager = holySiteManager ?? throw new ArgumentNullException(nameof(holySiteManager));
    }

    public bool SetCapital(CivilizationWorldData data, string civId, string requestorUID, string capitalName,
        string? holySiteId)
    {
        try
        {
            var civ = data.Civilizations.GetValueOrDefault(civId);
            if (civ == null)
            {
                _logger.Warning($"[DivineAscension] Civilization '{civId}' not found");
                return false;
            }

            if (!civ.IsFounder(requestorUID))
            {
                _logger.Warning("[DivineAscension] Only civilization founder can set capital");
                return false;
            }

            if (!CivilizationValidator.TryNormalizeCapitalName(capitalName, out var trimmed))
            {
                _logger.Warning("[DivineAscension] Capital name must be 1-64 characters");
                return false;
            }

            if (!string.IsNullOrEmpty(holySiteId))
            {
                var site = _holySiteManager?.GetHolySite(holySiteId);
                if (site == null)
                {
                    _logger.Warning($"[DivineAscension] Holy site '{holySiteId}' not found");
                    return false;
                }

                if (!civ.HasReligion(site.ReligionUID))
                {
                    _logger.Warning("[DivineAscension] Holy site does not belong to a member religion");
                    return false;
                }
            }

            civ.CapitalName = trimmed;
            civ.CapitalHolySiteId = string.IsNullOrEmpty(holySiteId) ? null : holySiteId;

            _logger.Notification($"[DivineAscension] Civilization '{civ.Name}' capital set to '{trimmed}'");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"[DivineAscension] Error setting capital: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    ///     Cascade: when a religion leaves a civ, if it owned the civ's bound capital
    ///     site, null the binding. Capital name is preserved.
    /// </summary>
    public void ClearCapitalIfOwnedByReligion(Civilization civ, string religionId)
    {
        if (civ.CapitalHolySiteId == null) return;
        var site = _holySiteManager?.GetHolySite(civ.CapitalHolySiteId);
        if (site != null && site.ReligionUID == religionId)
        {
            civ.CapitalHolySiteId = null;
            _logger.Debug(
                $"[DivineAscension] Cleared capital binding on civ '{civ.Name}' — religion {religionId} left");
        }
    }

    /// <summary>
    ///     Cascade hook: when a holy site is removed, null the binding on any civ whose
    ///     capital points at it. Capital name is preserved.
    /// </summary>
    public void HandleHolySiteRemoved(CivilizationWorldData data, string siteUID)
    {
        foreach (var civ in data.Civilizations.Values)
        {
            if (civ.CapitalHolySiteId == siteUID)
            {
                civ.CapitalHolySiteId = null;
                _logger.Debug(
                    $"[DivineAscension] Cleared capital binding on civ '{civ.Name}' — site {siteUID} removed");
            }
        }
    }
}
