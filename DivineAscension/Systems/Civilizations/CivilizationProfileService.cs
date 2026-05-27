using System;
using System.Collections.Generic;
using DivineAscension.Data;
using DivineAscension.Services;

namespace DivineAscension.Systems.Civilizations;

/// <summary>
///     Founder-gated edits to a civilization's presentation (icon, description).
///     Lock-free — the <see cref="CivilizationManager" /> facade serializes access.
///     Profanity checks for the description happen at the command/network layer.
/// </summary>
internal sealed class CivilizationProfileService
{
    private readonly ILoggerWrapper _logger;

    public CivilizationProfileService(ILoggerWrapper logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public bool UpdateIcon(CivilizationWorldData data, string civId, string requestorUID, string icon)
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
                _logger.Warning("[DivineAscension] Only civilization founder can update icon");
                return false;
            }

            if (string.IsNullOrWhiteSpace(icon))
            {
                _logger.Warning("[DivineAscension] Icon name cannot be empty");
                return false;
            }

            civ.UpdateIcon(icon);

            _logger.Notification($"[DivineAscension] Civilization '{civ.Name}' icon updated to '{icon}'");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"[DivineAscension] Error updating civilization icon: {ex.Message}");
            return false;
        }
    }

    public bool UpdateDescription(CivilizationWorldData data, string civId, string requestorUID, string description)
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
                _logger.Warning("[DivineAscension] Only civilization founder can update description");
                return false;
            }

            if (!CivilizationValidator.IsDescriptionLengthValid(description))
            {
                _logger.Warning("[DivineAscension] Description must be 200 characters or less");
                return false;
            }

            civ.UpdateDescription(description);

            _logger.Notification($"[DivineAscension] Civilization '{civ.Name}' description updated");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"[DivineAscension] Error updating civilization description: {ex.Message}");
            return false;
        }
    }
}
