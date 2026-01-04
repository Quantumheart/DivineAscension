using System;
using DivineAscension.Models.Enum;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace DivineAscension.GUI.Utilities;

/// <summary>
///     Helper class for displaying diplomacy-related notifications to players
/// </summary>
public static class DiplomacyNotificationHelper
{
    /// <summary>
    ///     Show notification when a new proposal is received
    /// </summary>
    public static void NotifyProposalReceived(ICoreClientAPI capi, string proposerCivName, DiplomaticStatus proposedStatus)
    {
        var statusText = GetStatusDisplayName(proposedStatus);
        capi.ShowChatMessage($"📨 New diplomacy proposal: {proposerCivName} proposes {statusText}");
    }

    /// <summary>
    ///     Show notification when a proposal is accepted
    /// </summary>
    public static void NotifyProposalAccepted(ICoreClientAPI capi, string targetCivName, DiplomaticStatus status)
    {
        var statusText = GetStatusDisplayName(status);
        capi.ShowChatMessage($"✅ {targetCivName} accepted your {statusText} proposal");
    }

    /// <summary>
    ///     Show notification when a proposal is declined
    /// </summary>
    public static void NotifyProposalDeclined(ICoreClientAPI capi, string targetCivName, DiplomaticStatus status)
    {
        var statusText = GetStatusDisplayName(status);
        capi.ShowChatMessage($"❌ {targetCivName} declined your {statusText} proposal");
    }

    /// <summary>
    ///     Show notification when a proposal expires
    /// </summary>
    public static void NotifyProposalExpired(ICoreClientAPI capi, string targetCivName, DiplomaticStatus status)
    {
        var statusText = GetStatusDisplayName(status);
        capi.ShowChatMessage($"⏰ Your {statusText} proposal to {targetCivName} has expired");
    }

    /// <summary>
    ///     Show notification when a treaty is established
    /// </summary>
    public static void NotifyTreatyEstablished(ICoreClientAPI capi, string otherCivName, DiplomaticStatus status)
    {
        var statusText = GetStatusDisplayName(status);
        var icon = GetStatusIcon(status);
        capi.ShowChatMessage($"{icon} Treaty established: Now {statusText} with {otherCivName}");
    }

    /// <summary>
    ///     Show warning when a treaty is scheduled to break
    /// </summary>
    public static void NotifyTreatyBreaking(ICoreClientAPI capi, string otherCivName, int hoursRemaining)
    {
        capi.ShowChatMessage($"⚠️ Treaty with {otherCivName} will end in {hoursRemaining} hours");
    }

    /// <summary>
    ///     Show notification when a treaty has ended
    /// </summary>
    public static void NotifyTreatyBroken(ICoreClientAPI capi, string otherCivName, DiplomaticStatus previousStatus)
    {
        var statusText = GetStatusDisplayName(previousStatus);
        capi.ShowChatMessage($"💔 {statusText} with {otherCivName} has ended");
    }

    /// <summary>
    ///     Show prominent notification when war is declared
    /// </summary>
    public static void NotifyWarDeclared(ICoreClientAPI capi, string declarerCivName, string targetCivName, bool isInvolved)
    {
        if (isInvolved)
        {
            // Prominent notification for involved players
            capi.ShowChatMessage($"⚔️ WAR DECLARED: {declarerCivName} has declared war on {targetCivName}!");
        }
        else
        {
            // Standard notification for observers
            capi.ShowChatMessage($"[Diplomacy] {declarerCivName} declared war on {targetCivName}");
        }
    }

    /// <summary>
    ///     Show urgent warning when a PvP violation occurs
    /// </summary>
    public static void NotifyPvPViolation(ICoreClientAPI capi, string allyCivName, int violationCount, int maxViolations)
    {
        capi.ShowChatMessage(
            $"⚠️ PvP VIOLATION: Attacked member of allied civilization {allyCivName}! ({violationCount}/{maxViolations})");
    }

    /// <summary>
    ///     Show notification when NAP is expiring soon
    /// </summary>
    public static void NotifyNapExpiringSoon(ICoreClientAPI capi, string otherCivName, int hoursRemaining)
    {
        capi.ShowChatMessage(
            $"⏰ Non-Aggression Pact with {otherCivName} expires in {hoursRemaining} hours");
    }

    /// <summary>
    ///     Show notification when peace is declared
    /// </summary>
    public static void NotifyPeaceDeclared(ICoreClientAPI capi, string otherCivName)
    {
        capi.ShowChatMessage($"🕊️ Peace declared with {otherCivName}");
    }

    /// <summary>
    ///     Show notification when Alliance prestige bonus is awarded
    /// </summary>
    public static void NotifyAlliancePrestigeBonus(ICoreClientAPI capi, string allyCivName, int prestigeBonus)
    {
        capi.ShowChatMessage(
            $"✨ Alliance formed with {allyCivName}! Your religion gained +{prestigeBonus} prestige");
    }

    /// <summary>
    ///     Get display name for diplomatic status
    /// </summary>
    private static string GetStatusDisplayName(DiplomaticStatus status)
    {
        return status switch
        {
            DiplomaticStatus.NonAggressionPact => "Non-Aggression Pact",
            DiplomaticStatus.Alliance => "Alliance",
            DiplomaticStatus.War => "War",
            DiplomaticStatus.Neutral => "Neutral",
            _ => status.ToString()
        };
    }

    /// <summary>
    ///     Get icon for diplomatic status
    /// </summary>
    private static string GetStatusIcon(DiplomaticStatus status)
    {
        return status switch
        {
            DiplomaticStatus.NonAggressionPact => "🤝",
            DiplomaticStatus.Alliance => "⭐",
            DiplomaticStatus.War => "⚔️",
            DiplomaticStatus.Neutral => "🔘",
            _ => "📜"
        };
    }

    /// <summary>
    ///     Format time remaining for countdown displays
    /// </summary>
    public static string FormatTimeRemaining(DateTime expiresDate)
    {
        var timeRemaining = expiresDate - DateTime.UtcNow;

        if (timeRemaining.TotalSeconds < 0)
            return "Expired";

        if (timeRemaining.TotalDays >= 1)
            return $"{(int)timeRemaining.TotalDays}d {timeRemaining.Hours}h";

        if (timeRemaining.TotalHours >= 1)
            return $"{(int)timeRemaining.TotalHours}h {timeRemaining.Minutes}m";

        return $"{(int)timeRemaining.TotalMinutes}m";
    }

    /// <summary>
    ///     Check if time remaining is critical (less than 1 hour)
    /// </summary>
    public static bool IsTimeCritical(DateTime expiresDate)
    {
        var timeRemaining = expiresDate - DateTime.UtcNow;
        return timeRemaining.TotalHours < 1 && timeRemaining.TotalSeconds > 0;
    }
}
