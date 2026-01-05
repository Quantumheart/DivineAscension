using System.Collections.Generic;
using System.Linq;
using DivineAscension.Models.Enum;
using ProtoBuf;

namespace DivineAscension.Data;

/// <summary>
/// World-level data container for all diplomacy data
/// </summary>
[ProtoContract]
public class DiplomacyWorldData
{
    /// <summary>
    /// Parameterless constructor for serialization
    /// </summary>
    public DiplomacyWorldData()
    {
        Relationships = new Dictionary<string, DiplomaticRelationship>();
        PendingProposals = new List<DiplomaticProposal>();
        CivRelationshipMap = new Dictionary<string, List<string>>();
    }

    /// <summary>
    /// All diplomatic relationships indexed by RelationshipId
    /// </summary>
    [ProtoMember(1)]
    public Dictionary<string, DiplomaticRelationship> Relationships { get; set; }

    /// <summary>
    /// Pending diplomatic proposals
    /// </summary>
    [ProtoMember(2)]
    public List<DiplomaticProposal> PendingProposals { get; set; }

    /// <summary>
    /// Quick lookup map: CivId -> List of RelationshipIds
    /// </summary>
    [ProtoMember(3)]
    public Dictionary<string, List<string>> CivRelationshipMap { get; set; }

    /// <summary>
    /// Gets the relationship between two civilizations (order-independent)
    /// </summary>
    public DiplomaticRelationship? GetRelationship(string civId1, string civId2)
    {
        return Relationships.Values.FirstOrDefault(r =>
            (r.CivId1 == civId1 && r.CivId2 == civId2) ||
            (r.CivId1 == civId2 && r.CivId2 == civId1));
    }

    /// <summary>
    /// Gets all proposals for a civilization (both incoming and outgoing)
    /// </summary>
    public List<DiplomaticProposal> GetProposalsForCiv(string civId)
    {
        return PendingProposals.Where(p =>
            (p.ProposerCivId == civId || p.TargetCivId == civId) && p.IsValid).ToList();
    }

    /// <summary>
    /// Gets all incoming proposals for a civilization
    /// </summary>
    public List<DiplomaticProposal> GetIncomingProposals(string civId)
    {
        return PendingProposals.Where(p => p.TargetCivId == civId && p.IsValid).ToList();
    }

    /// <summary>
    /// Gets all outgoing proposals from a civilization
    /// </summary>
    public List<DiplomaticProposal> GetOutgoingProposals(string civId)
    {
        return PendingProposals.Where(p => p.ProposerCivId == civId && p.IsValid).ToList();
    }

    /// <summary>
    /// Gets a specific proposal by ID
    /// </summary>
    public DiplomaticProposal? GetProposal(string proposalId)
    {
        return PendingProposals.FirstOrDefault(p => p.ProposalId == proposalId);
    }

    /// <summary>
    /// Checks if there's a pending proposal between two civilizations
    /// </summary>
    public bool HasPendingProposal(string proposerCivId, string targetCivId)
    {
        return PendingProposals.Any(p =>
            p.ProposerCivId == proposerCivId &&
            p.TargetCivId == targetCivId &&
            p.IsValid);
    }

    /// <summary>
    /// Adds a relationship and updates lookup maps
    /// </summary>
    public void AddRelationship(DiplomaticRelationship relationship)
    {
        Relationships[relationship.RelationshipId] = relationship;

        // Initialize CivRelationshipMap if null (for old save file compatibility)
        CivRelationshipMap ??= new Dictionary<string, List<string>>();

        // Update lookup map for both civilizations
        if (!CivRelationshipMap.ContainsKey(relationship.CivId1))
            CivRelationshipMap[relationship.CivId1] = new List<string>();
        if (!CivRelationshipMap.ContainsKey(relationship.CivId2))
            CivRelationshipMap[relationship.CivId2] = new List<string>();

        CivRelationshipMap[relationship.CivId1].Add(relationship.RelationshipId);
        CivRelationshipMap[relationship.CivId2].Add(relationship.RelationshipId);
    }

    /// <summary>
    /// Removes a relationship and updates lookup maps
    /// </summary>
    public void RemoveRelationship(string relationshipId)
    {
        if (Relationships.TryGetValue(relationshipId, out var relationship))
        {
            // Clean up lookup maps
            if (CivRelationshipMap.TryGetValue(relationship.CivId1, out var list1))
                list1.Remove(relationshipId);
            if (CivRelationshipMap.TryGetValue(relationship.CivId2, out var list2))
                list2.Remove(relationshipId);

            Relationships.Remove(relationshipId);
        }
    }

    /// <summary>
    /// Gets all relationships for a civilization
    /// </summary>
    public List<DiplomaticRelationship> GetRelationshipsForCiv(string civId)
    {
        // Initialize if null (for old save file compatibility)
        CivRelationshipMap ??= new Dictionary<string, List<string>>();

        if (!CivRelationshipMap.TryGetValue(civId, out var relationshipIds))
            return new List<DiplomaticRelationship>();

        return relationshipIds
            .Select(id => Relationships.TryGetValue(id, out var rel) ? rel : null)
            .Where(rel => rel != null)
            .ToList()!;
    }

    /// <summary>
    /// Adds a proposal
    /// </summary>
    public void AddProposal(DiplomaticProposal proposal)
    {
        PendingProposals.Add(proposal);
    }

    /// <summary>
    /// Removes a proposal
    /// </summary>
    public void RemoveProposal(string proposalId)
    {
        PendingProposals.RemoveAll(p => p.ProposalId == proposalId);
    }

    /// <summary>
    /// Removes all proposals involving a civilization
    /// </summary>
    public void RemoveProposalsForCiv(string civId)
    {
        PendingProposals.RemoveAll(p => p.ProposerCivId == civId || p.TargetCivId == civId);
    }

    /// <summary>
    /// Removes all relationships involving a civilization
    /// </summary>
    public void RemoveRelationshipsForCiv(string civId)
    {
        var relationshipsToRemove = GetRelationshipsForCiv(civId)
            .Select(r => r.RelationshipId)
            .ToList();

        foreach (var relationshipId in relationshipsToRemove)
        {
            RemoveRelationship(relationshipId);
        }

        CivRelationshipMap.Remove(civId);
    }

    /// <summary>
    /// Cleans up expired proposals
    /// </summary>
    public int CleanupExpiredProposals()
    {
        var expiredCount = PendingProposals.RemoveAll(p => !p.IsValid);
        return expiredCount;
    }

    /// <summary>
    /// Cleans up expired relationships
    /// </summary>
    public int CleanupExpiredRelationships()
    {
        var expiredRelationships = Relationships.Values
            .Where(r => r.IsExpired)
            .Select(r => r.RelationshipId)
            .ToList();

        foreach (var relationshipId in expiredRelationships)
        {
            RemoveRelationship(relationshipId);
        }

        return expiredRelationships.Count;
    }

    /// <summary>
    /// Gets the diplomatic status between two civilizations
    /// </summary>
    public DiplomaticStatus GetDiplomaticStatus(string civId1, string civId2)
    {
        var relationship = GetRelationship(civId1, civId2);
        return relationship?.Status ?? DiplomaticStatus.Neutral;
    }
}