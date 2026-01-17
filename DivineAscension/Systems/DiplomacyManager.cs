using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DivineAscension.Constants;
using DivineAscension.Data;
using DivineAscension.Models.Enum;
using DivineAscension.Systems.Interfaces;
using Vintagestory.API.Server;

namespace DivineAscension.Systems;

/// <summary>
/// Manages diplomatic relationships between civilizations
/// </summary>
public class DiplomacyManager : IDiplomacyManager
{
    private readonly CivilizationManager _civilizationManager;
    private readonly IReligionPrestigeManager _prestigeManager;
    private readonly IReligionManager _religionManager;
    private readonly ICoreServerAPI _sapi;
    private readonly ICooldownManager _cooldownManager;
    private DiplomacyWorldData _data = new();

    /// <summary>
    ///     Lazy-initialized lock object for thread safety using Interlocked.CompareExchange
    /// </summary>
    private object? _lock;
    private object Lock
    {
        get
        {
            if (_lock == null)
            {
                Interlocked.CompareExchange(ref _lock, new object(), null);
            }
            return _lock;
        }
    }

    public DiplomacyManager(
        ICoreServerAPI sapi,
        CivilizationManager civilizationManager,
        IReligionPrestigeManager prestigeManager,
        IReligionManager religionManager,
        ICooldownManager cooldownManager)
    {
        _sapi = sapi ?? throw new ArgumentNullException(nameof(sapi));
        _civilizationManager = civilizationManager ?? throw new ArgumentNullException(nameof(civilizationManager));
        _prestigeManager = prestigeManager ?? throw new ArgumentNullException(nameof(prestigeManager));
        _religionManager = religionManager ?? throw new ArgumentNullException(nameof(religionManager));
        _cooldownManager = cooldownManager ?? throw new ArgumentNullException(nameof(cooldownManager));
    }

    public event Action<string, string, DiplomaticStatus>? OnRelationshipEstablished;
    public event Action<string, string, DiplomaticStatus>? OnRelationshipEnded;
    public event Action<string, string>? OnWarDeclared;

    public void Initialize()
    {
        _sapi.Logger.Notification($"{DiplomacyConstants.LogPrefix} Initializing Diplomacy Manager...");

        // Register save/load events
        _sapi.Event.SaveGameLoaded += OnSaveGameLoaded;
        _sapi.Event.GameWorldSave += OnGameWorldSave;

        // Subscribe to civilization disbanded events
        _civilizationManager.OnCivilizationDisbanded += HandleCivilizationDisbanded;

        _sapi.Logger.Notification($"{DiplomacyConstants.LogPrefix} Diplomacy Manager initialized");
    }

    public void Dispose()
    {
        _sapi.Event.SaveGameLoaded -= OnSaveGameLoaded;
        _sapi.Event.GameWorldSave -= OnGameWorldSave;
        _civilizationManager.OnCivilizationDisbanded -= HandleCivilizationDisbanded;
        OnRelationshipEstablished = null;
        OnRelationshipEnded = null;
        OnWarDeclared = null;
    }

    #region Event Handlers

    private void OnSaveGameLoaded()
    {
        lock (Lock)
        {
            _data = _sapi.WorldManager.SaveGame.GetData<DiplomacyWorldData>(DiplomacyConstants.DataKey);
            if (_data == null)
            {
                _data = new DiplomacyWorldData();
                _sapi.Logger.Notification($"{DiplomacyConstants.LogPrefix} No existing diplomacy data found, creating new");
            }
            else
            {
                _sapi.Logger.Notification(
                    $"{DiplomacyConstants.LogPrefix} Loaded diplomacy data: {_data.Relationships.Count} relationships");
            }

            // Run cleanup on load
            CleanupExpiredData_Unlocked();
        }
    }

    private void OnGameWorldSave()
    {
        lock (Lock)
        {
            CleanupExpiredData_Unlocked();
            _sapi.WorldManager.SaveGame.StoreData(DiplomacyConstants.DataKey, _data);
            _sapi.Logger.Debug(
                $"{DiplomacyConstants.LogPrefix} Saved diplomacy data: {_data.Relationships.Count} relationships");
        }
    }

    private void HandleCivilizationDisbanded(string civId)
    {
        lock (Lock)
        {
            _sapi.Logger.Debug($"{DiplomacyConstants.LogPrefix} Handling civilization disbanded: {civId}");

            // Remove all relationships involving this civilization
            _data.RemoveRelationshipsForCiv(civId);

            // Remove all proposals involving this civilization
            _data.RemoveProposalsForCiv(civId);

            _sapi.Logger.Debug(
                $"{DiplomacyConstants.LogPrefix} Cleaned up diplomacy data for disbanded civilization: {civId}");
        }
    }

    #endregion

    #region Proposal Management

    public (bool success, string message, string? proposalId) ProposeRelationship(
        string proposerCivId,
        string targetCivId,
        DiplomaticStatus proposedStatus,
        string proposerFounderUID,
        int? duration = null)
    {
        // Validate civilizations exist
        var proposerCiv = _civilizationManager.GetCivilization(proposerCivId);
        var targetCiv = _civilizationManager.GetCivilization(targetCivId);

        if (proposerCiv == null)
            return (false, "Proposer civilization not found", null);
        if (targetCiv == null)
            return (false, "Target civilization not found", null);

        // Validate founder
        if (proposerCiv.FounderUID != proposerFounderUID)
            return (false, "Only the civilization founder can propose diplomatic relationships", null);

        // Can't propose to yourself
        if (proposerCivId == targetCivId)
            return (false, "Cannot propose a diplomatic relationship with your own civilization", null);

        // Validate civilizations are valid (have 1-4 religions)
        if (!proposerCiv.IsValid)
            return (false, "Your civilization is invalid (must have 1-4 religions with different deities)", null);
        if (!targetCiv.IsValid)
            return (false, "Target civilization is invalid", null);

        // War doesn't require proposals
        if (proposedStatus == DiplomaticStatus.War)
            return (false, "Use DeclareWar to enter war status", null);

        // Check cooldown (30 seconds)
        if (!_cooldownManager.CanPerformOperation(proposerFounderUID, CooldownType.Proposal, out var cooldownError))
            return (false, cooldownError!, null);

        lock (Lock)
        {
            // Check if relationship already exists
            var existingRelationship = _data.GetRelationship(proposerCivId, targetCivId);
            if (existingRelationship != null)
                return (false, $"A diplomatic relationship already exists: {existingRelationship.Status}", null);

            // Check if there's already a pending proposal
            if (_data.HasPendingProposal(proposerCivId, targetCivId))
                return (false, "You already have a pending proposal to this civilization", null);

            // Check rank requirements
            var (hasRank, rankMessage) = ValidateRankRequirement(proposerCivId, proposedStatus);
            if (!hasRank)
                return (false, rankMessage, null);

            // Create proposal
            var proposalId = Guid.NewGuid().ToString();
            var proposal = new DiplomaticProposal(
                proposalId,
                proposerCivId,
                targetCivId,
                proposedStatus,
                proposerFounderUID,
                duration
            );

            _data.AddProposal(proposal);

            // Record cooldown after successful proposal
            _cooldownManager.RecordOperation(proposerFounderUID, CooldownType.Proposal);

            _sapi.Logger.Notification(
                $"{DiplomacyConstants.LogPrefix} {proposerCiv.Name} proposed {proposedStatus} to {targetCiv.Name}");

            return (true, $"Proposal sent to {targetCiv.Name}", proposalId);
        }
    }

    public (bool success, string message, string? relationshipId) AcceptProposal(string proposalId,
        string acceptorFounderUID)
    {
        lock (Lock)
        {
            var proposal = _data.GetProposal(proposalId);
            if (proposal == null)
                return (false, "Proposal not found", null);

            if (!proposal.IsValid)
            {
                _data.RemoveProposal(proposalId);
                return (false, "Proposal has expired", null);
            }

            var targetCiv = _civilizationManager.GetCivilization(proposal.TargetCivId);
            if (targetCiv == null)
                return (false, "Target civilization not found", null);

            // Validate founder
            if (targetCiv.FounderUID != acceptorFounderUID)
                return (false, "Only the civilization founder can accept diplomatic proposals", null);

            // Verify both civilizations still meet rank requirements
            var (proposerHasRank, proposerMessage) =
                ValidateRankRequirement(proposal.ProposerCivId, proposal.ProposedStatus);
            if (!proposerHasRank)
            {
                _data.RemoveProposal(proposalId);
                return (false, $"Proposer no longer meets requirements: {proposerMessage}", null);
            }

            var (targetHasRank, targetMessage) = ValidateRankRequirement(proposal.TargetCivId, proposal.ProposedStatus);
            if (!targetHasRank)
                return (false, $"Your civilization does not meet requirements: {targetMessage}", null);

            // Create relationship
            var relationshipId = Guid.NewGuid().ToString();
            DateTime? expiresDate = null;

            if (proposal.ProposedStatus == DiplomaticStatus.NonAggressionPact)
            {
                expiresDate = DateTime.UtcNow.AddDays(DiplomacyConstants.NonAggressionPactDurationDays);
            }

            var relationship = new DiplomaticRelationship(
                relationshipId,
                proposal.ProposerCivId,
                proposal.TargetCivId,
                proposal.ProposedStatus,
                proposal.ProposerCivId,
                expiresDate
            );

            _data.AddRelationship(relationship);
            _data.RemoveProposal(proposalId);

            // Fire event
            OnRelationshipEstablished?.Invoke(proposal.ProposerCivId, proposal.TargetCivId, proposal.ProposedStatus);

            var proposerCiv = _civilizationManager.GetCivilization(proposal.ProposerCivId);
            _sapi.Logger.Notification(
                $"{DiplomacyConstants.LogPrefix} {targetCiv.Name} accepted {proposal.ProposedStatus} with {proposerCiv?.Name}");

            return (true, $"Diplomatic relationship established: {proposal.ProposedStatus}", relationshipId);
        }
    }

    public (bool success, string message) DeclineProposal(string proposalId, string declinerFounderUID)
    {
        lock (Lock)
        {
            var proposal = _data.GetProposal(proposalId);
            if (proposal == null)
                return (false, "Proposal not found");

            var targetCiv = _civilizationManager.GetCivilization(proposal.TargetCivId);
            if (targetCiv == null)
                return (false, "Target civilization not found");

            // Validate founder
            if (targetCiv.FounderUID != declinerFounderUID)
                return (false, "Only the civilization founder can decline diplomatic proposals");

            _data.RemoveProposal(proposalId);

            _sapi.Logger.Debug(
                $"{DiplomacyConstants.LogPrefix} {targetCiv.Name} declined proposal from {proposal.ProposerCivId}");

            return (true, "Proposal declined");
        }
    }

    #endregion

    #region Relationship Management

    public (bool success, string message) ScheduleBreak(string civId, string otherCivId, string founderUID)
    {
        lock (Lock)
        {
            var relationship = _data.GetRelationship(civId, otherCivId);
            if (relationship == null)
                return (false, "No diplomatic relationship exists");

            var civ = _civilizationManager.GetCivilization(civId);
            if (civ == null || !civ.IsFounder(founderUID))
                return (false, "Only the civilization founder can schedule treaty breaks");

            if (relationship.Status == DiplomaticStatus.War || relationship.Status == DiplomaticStatus.Neutral)
                return (false, "Cannot schedule break for this relationship type");

            if (relationship.BreakScheduledDate != null)
                return (false, "Treaty break is already scheduled");

            relationship.BreakScheduledDate = DateTime.UtcNow.AddHours(DiplomacyConstants.TreatyBreakWarningHours);

            _sapi.Logger.Notification(
                $"{DiplomacyConstants.LogPrefix} {civ.Name} scheduled treaty break with {otherCivId} (24 hours)");

            return (true, $"Treaty will break in {DiplomacyConstants.TreatyBreakWarningHours} hours");
        }
    }

    public (bool success, string message) CancelScheduledBreak(string civId, string otherCivId, string founderUID)
    {
        lock (Lock)
        {
            var relationship = _data.GetRelationship(civId, otherCivId);
            if (relationship == null)
                return (false, "No diplomatic relationship exists");

            var civ = _civilizationManager.GetCivilization(civId);
            if (civ == null || !civ.IsFounder(founderUID))
                return (false, "Only the civilization founder can cancel scheduled treaty breaks");

            if (relationship.BreakScheduledDate == null)
                return (false, "No treaty break is scheduled");

            relationship.BreakScheduledDate = null;

            _sapi.Logger.Debug(
                $"{DiplomacyConstants.LogPrefix} {civ.Name} canceled scheduled treaty break with {otherCivId}");

            return (true, "Scheduled treaty break canceled");
        }
    }

    public (bool success, string message) DeclareWar(string declarerCivId, string targetCivId, string founderUID)
    {
        var declarerCiv = _civilizationManager.GetCivilization(declarerCivId);
        var targetCiv = _civilizationManager.GetCivilization(targetCivId);

        if (declarerCiv == null)
            return (false, "Declarer civilization not found");
        if (targetCiv == null)
            return (false, "Target civilization not found");

        if (declarerCiv.FounderUID != founderUID)
            return (false, "Only the civilization founder can declare war");

        if (declarerCivId == targetCivId)
            return (false, "Cannot declare war on your own civilization");

        // Validate civilizations are valid (have 1-4 religions)
        if (!declarerCiv.IsValid)
            return (false, "Your civilization is invalid (must have 1-4 religions with different deities)");
        if (!targetCiv.IsValid)
            return (false, "Target civilization is invalid");

        // Check cooldown (60 seconds)
        if (!_cooldownManager.CanPerformOperation(founderUID, CooldownType.WarDeclaration, out var cooldownError))
            return (false, cooldownError!);

        lock (Lock)
        {
            // Remove existing relationship if any
            var existingRelationship = _data.GetRelationship(declarerCivId, targetCivId);
            if (existingRelationship != null)
            {
                OnRelationshipEnded?.Invoke(declarerCivId, targetCivId, existingRelationship.Status);
                _data.RemoveRelationship(existingRelationship.RelationshipId);
            }

            // Cancel any pending proposals
            var pendingProposals = _data.PendingProposals
                .Where(p => (p.ProposerCivId == declarerCivId && p.TargetCivId == targetCivId) ||
                            (p.ProposerCivId == targetCivId && p.TargetCivId == declarerCivId))
                .ToList();

            foreach (var proposal in pendingProposals)
            {
                _data.RemoveProposal(proposal.ProposalId);
            }

            // Create war relationship
            var relationshipId = Guid.NewGuid().ToString();
            var relationship = new DiplomaticRelationship(
                relationshipId,
                declarerCivId,
                targetCivId,
                DiplomaticStatus.War,
                declarerCivId,
                null // War doesn't expire
            );

            _data.AddRelationship(relationship);

            // Fire events
            OnWarDeclared?.Invoke(declarerCivId, targetCivId);
            OnRelationshipEstablished?.Invoke(declarerCivId, targetCivId, DiplomaticStatus.War);

            // Record cooldown after successful war declaration
            _cooldownManager.RecordOperation(founderUID, CooldownType.WarDeclaration);

            _sapi.Logger.Notification(
                $"{DiplomacyConstants.LogPrefix} {declarerCiv.Name} declared WAR on {targetCiv.Name}!");

            return (true, $"War declared on {targetCiv.Name}");
        }
    }

    public (bool success, string message) DeclarePeace(string civId, string otherCivId, string founderUID)
    {
        lock (Lock)
        {
            var relationship = _data.GetRelationship(civId, otherCivId);
            if (relationship == null)
                return (false, "No diplomatic relationship exists");

            if (relationship.Status != DiplomaticStatus.War)
                return (false, "Can only declare peace from war status");

            var civ = _civilizationManager.GetCivilization(civId);
            if (civ == null || !civ.IsFounder(founderUID))
                return (false, "Only the civilization founder can declare peace");

            // Remove war relationship
            OnRelationshipEnded?.Invoke(civId, otherCivId, DiplomaticStatus.War);
            _data.RemoveRelationship(relationship.RelationshipId);

            var otherCiv = _civilizationManager.GetCivilization(otherCivId);
            _sapi.Logger.Notification($"{DiplomacyConstants.LogPrefix} {civ.Name} declared peace with {otherCiv?.Name}");

            return (true, "Peace declared - relationship returned to Neutral");
        }
    }

    #endregion

    #region PvP Integration

    public int RecordPvPViolation(string attackerCivId, string victimCivId)
    {
        lock (Lock)
        {
            var relationship = _data.GetRelationship(attackerCivId, victimCivId);
            if (relationship == null)
                return 0;

            if (relationship.Status != DiplomaticStatus.Alliance &&
                relationship.Status != DiplomaticStatus.NonAggressionPact)
                return 0;

            relationship.RecordViolation();

            _sapi.Logger.Warning(
                $"{DiplomacyConstants.LogPrefix} PvP violation recorded: {attackerCivId} attacked {victimCivId} (count: {relationship.ViolationCount}/{DiplomacyConstants.MaxViolations})");

            // Auto-break on 3rd violation
            if (relationship.ViolationCount >= DiplomacyConstants.MaxViolations)
            {
                _sapi.Logger.Notification(
                    $"{DiplomacyConstants.LogPrefix} Treaty auto-broken due to {DiplomacyConstants.MaxViolations} violations");
                OnRelationshipEnded?.Invoke(attackerCivId, victimCivId, relationship.Status);
                _data.RemoveRelationship(relationship.RelationshipId);
                return 0; // Relationship no longer exists
            }

            return relationship.ViolationCount;
        }
    }

    public double GetFavorMultiplier(string attackerCivId, string victimCivId)
    {
        lock (Lock)
        {
            var status = _data.GetDiplomaticStatus(attackerCivId, victimCivId);

            return status switch
            {
                DiplomaticStatus.War => DiplomacyConstants.WarFavorMultiplier,
                DiplomaticStatus.Alliance or DiplomaticStatus.NonAggressionPact => 0.0, // No rewards for attacking allies
                _ => 1.0 // Neutral
            };
        }
    }

    #endregion

    #region Query Methods

    public DiplomaticStatus GetDiplomaticStatus(string civId1, string civId2)
    {
        lock (Lock)
        {
            return _data.GetDiplomaticStatus(civId1, civId2);
        }
    }

    public List<DiplomaticRelationship> GetRelationshipsForCiv(string civId)
    {
        lock (Lock)
        {
            return _data.GetRelationshipsForCiv(civId);
        }
    }

    public List<DiplomaticProposal> GetProposalsForCiv(string civId)
    {
        lock (Lock)
        {
            return _data.GetProposalsForCiv(civId);
        }
    }

    public DiplomaticRelationship? GetRelationship(string relationshipId)
    {
        lock (Lock)
        {
            return _data.Relationships.TryGetValue(relationshipId, out var relationship) ? relationship : null;
        }
    }

    #endregion

    #region Helper Methods

    private (bool hasRank, string message) ValidateRankRequirement(string civId, DiplomaticStatus status)
    {
        // No rank requirement for Neutral or War
        if (status == DiplomaticStatus.Neutral || status == DiplomaticStatus.War)
            return (true, "");

        var civ = _civilizationManager.GetCivilization(civId);
        if (civ == null)
            return (false, "Civilization not found");

        // Get the prestige rank of all religions in the civilization
        var religions = civ.MemberReligionIds.ToList()
            .Select(id => _religionManager.GetReligion(id))
            .Where(r => r != null)
            .ToList();

        if (!religions.Any())
            return (false, "Civilization has no valid religions");

        // Check if at least one religion meets the rank requirement
        foreach (var religion in religions)
        {
            var (current, nextThreshold, nextRank) = _prestigeManager.GetPrestigeProgress(religion!.ReligionUID);
            var currentRank = GetCurrentRank(current);

            if (status == DiplomaticStatus.NonAggressionPact &&
                currentRank >= DiplomacyConstants.NonAggressionPactRequiredRank)
                return (true, "");

            if (status == DiplomaticStatus.Alliance && currentRank >= DiplomacyConstants.AllianceRequiredRank)
                return (true, "");
        }

        // No religion meets the requirement
        var requiredRank = status == DiplomaticStatus.NonAggressionPact
            ? DiplomacyConstants.NonAggressionPactRankName
            : DiplomacyConstants.AllianceRankName;

        return (false, $"Requires at least one religion with {requiredRank} rank");
    }

    private int GetCurrentRank(int prestigePoints)
    {
        // Prestige thresholds (from ReligionPrestigeManager)
        // 0 = Fledgling, 1 = Established (100), 2 = Renowned (500), 3 = Exalted (1500), 4 = Divine (3500)
        if (prestigePoints >= 3500) return 4;
        if (prestigePoints >= 1500) return 3;
        if (prestigePoints >= 500) return 2;
        if (prestigePoints >= 100) return 1;
        return 0;
    }

    private void CleanupExpiredData()
    {
        lock (Lock)
        {
            CleanupExpiredData_Unlocked();
        }
    }

    /// <summary>
    ///     Internal unlocked version of CleanupExpiredData (caller must hold lock)
    /// </summary>
    private void CleanupExpiredData_Unlocked()
    {
        // Cleanup expired proposals
        var expiredProposals = _data.CleanupExpiredProposals();
        if (expiredProposals > 0)
            _sapi.Logger.Debug($"{DiplomacyConstants.LogPrefix} Cleaned up {expiredProposals} expired proposals");

        // Cleanup expired relationships (NAP)
        var expiredRelationships = _data.CleanupExpiredRelationships();
        if (expiredRelationships > 0)
            _sapi.Logger.Debug(
                $"{DiplomacyConstants.LogPrefix} Cleaned up {expiredRelationships} expired relationships");

        // Process scheduled breaks
        var relationshipsToBreak = _data.Relationships.Values
            .Where(r => r.BreakScheduledDate.HasValue && DateTime.UtcNow >= r.BreakScheduledDate.Value)
            .ToList();

        foreach (var relationship in relationshipsToBreak)
        {
            _sapi.Logger.Notification(
                $"{DiplomacyConstants.LogPrefix} Executing scheduled treaty break for relationship {relationship.RelationshipId}");
            OnRelationshipEnded?.Invoke(relationship.CivId1, relationship.CivId2, relationship.Status);
            _data.RemoveRelationship(relationship.RelationshipId);
        }
    }

    #endregion
}