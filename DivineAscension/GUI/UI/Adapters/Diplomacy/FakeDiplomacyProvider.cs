using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DivineAscension.Models.Enum;
using DivineAscension.Network.Diplomacy;

namespace DivineAscension.GUI.UI.Adapters.Diplomacy;

/// <summary>
///     Dev-only diplomacy provider. Picks a deterministic subset of the
///     supplied other realms and assigns them roles (alliance / NAP / war /
///     incoming proposal / outgoing proposal) so the ledger Accords chapter
///     and Propose page have something to render without a running server.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class FakeDiplomacyProvider : IDiplomacyProvider
{
    private int _seed = 20260522;

    public void ConfigureDevSeed(int seed)
    {
        _seed = seed;
    }

    public DiplomacyInfoResponsePacket GetDiplomacyInfo(
        string currentCivId,
        IReadOnlyList<(string CivId, string Name)> otherRealms)
    {
        var rnd = new Random(_seed);
        var now = DateTime.UtcNow;

        // Shuffle a copy of the roster so each call grabs the same realms for
        // each slot, but the slots themselves rotate when the dev tweaks the
        // seed.
        var roster = otherRealms.ToList();
        for (var i = roster.Count - 1; i > 0; i--)
        {
            var j = rnd.Next(i + 1);
            (roster[i], roster[j]) = (roster[j], roster[i]);
        }

        var relationships = new List<DiplomacyInfoResponsePacket.RelationshipInfo>();
        var incoming = new List<DiplomacyInfoResponsePacket.ProposalInfo>();
        var outgoing = new List<DiplomacyInfoResponsePacket.ProposalInfo>();

        // Slots are picked by index so the layout reads predictably:
        //  0: alliance (no scheduled break)
        //  1: alliance with a scheduled break in 14 hours
        //  2: NAP, permanent
        //  3: NAP, expiring soon, two grievances
        //  4: war
        //  5: incoming proposal — alliance offer
        //  6: incoming proposal — NAP offer
        //  7: outgoing proposal — alliance offer
        TryAddRelationship(roster, 0, rel => relationships.Add(MakeRelationship(rel,
            DiplomaticStatus.Alliance,
            established: now.AddDays(-90),
            expires: now.AddDays(275),
            violations: 0,
            breakScheduled: null)));

        TryAddRelationship(roster, 1, rel => relationships.Add(MakeRelationship(rel,
            DiplomaticStatus.Alliance,
            established: now.AddDays(-160),
            expires: null,
            violations: 1,
            breakScheduled: now.AddHours(14))));

        TryAddRelationship(roster, 2, rel => relationships.Add(MakeRelationship(rel,
            DiplomaticStatus.NonAggressionPact,
            established: now.AddDays(-200),
            expires: null,
            violations: 0,
            breakScheduled: null)));

        TryAddRelationship(roster, 3, rel => relationships.Add(MakeRelationship(rel,
            DiplomaticStatus.NonAggressionPact,
            established: now.AddDays(-40),
            expires: now.AddDays(2),
            violations: 2,
            breakScheduled: null)));

        TryAddRelationship(roster, 4, rel => relationships.Add(MakeRelationship(rel,
            DiplomaticStatus.War,
            established: now.AddDays(-7),
            expires: null,
            violations: 0,
            breakScheduled: null)));

        TryAddRelationship(roster, 5, rel => incoming.Add(MakeProposal(rel,
            DiplomaticStatus.Alliance,
            sent: now.AddHours(-6),
            expires: now.AddHours(66))));

        TryAddRelationship(roster, 6, rel => incoming.Add(MakeProposal(rel,
            DiplomaticStatus.NonAggressionPact,
            sent: now.AddHours(-1),
            expires: now.AddMinutes(35))));

        TryAddRelationship(roster, 7, rel => outgoing.Add(MakeProposal(rel,
            DiplomaticStatus.Alliance,
            sent: now.AddHours(-12),
            expires: now.AddHours(60))));

        return new DiplomacyInfoResponsePacket
        {
            CivId = currentCivId,
            Relationships = relationships,
            IncomingProposals = incoming,
            OutgoingProposals = outgoing,
        };
    }

    private static void TryAddRelationship(
        List<(string CivId, string Name)> roster,
        int index,
        Action<(string CivId, string Name)> add)
    {
        if (index < roster.Count) add(roster[index]);
    }

    private static DiplomacyInfoResponsePacket.RelationshipInfo MakeRelationship(
        (string CivId, string Name) other,
        DiplomaticStatus status,
        DateTime established,
        DateTime? expires,
        int violations,
        DateTime? breakScheduled)
    {
        return new DiplomacyInfoResponsePacket.RelationshipInfo
        {
            RelationshipId = Guid.NewGuid().ToString("N"),
            OtherCivId = other.CivId,
            OtherCivName = other.Name,
            Status = status,
            EstablishedDate = established,
            ExpiresDate = expires,
            ViolationCount = violations,
            BreakScheduledDate = breakScheduled,
        };
    }

    private static DiplomacyInfoResponsePacket.ProposalInfo MakeProposal(
        (string CivId, string Name) other,
        DiplomaticStatus proposed,
        DateTime sent,
        DateTime expires)
    {
        return new DiplomacyInfoResponsePacket.ProposalInfo
        {
            ProposalId = Guid.NewGuid().ToString("N"),
            OtherCivId = other.CivId,
            OtherCivName = other.Name,
            ProposedStatus = proposed,
            SentDate = sent,
            ExpiresDate = expires,
            Duration = proposed == DiplomaticStatus.NonAggressionPact ? 3 : null,
        };
    }
}
