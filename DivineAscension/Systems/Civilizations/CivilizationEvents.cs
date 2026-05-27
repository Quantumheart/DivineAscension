using System.Collections.Generic;

namespace DivineAscension.Systems.Civilizations;

/// <summary>
///     The kinds of side-effect events a civilization mutation can produce.
///     Collaborators record these; <see cref="CivilizationManager" /> (the facade)
///     is the sole place that raises the corresponding public events.
/// </summary>
internal enum CivEventKind
{
    Disbanded,
    ReligionAdded,
    ReligionRemoved
}

/// <summary>
///     A deferred civilization event. Lock-free collaborators append these to a
///     result; the facade replays them onto its public event delegates.
/// </summary>
internal readonly record struct CivEvent(CivEventKind Kind, string CivId, string? ReligionId)
{
    public static CivEvent Disbanded(string civId) => new(CivEventKind.Disbanded, civId, null);

    public static CivEvent ReligionAdded(string civId, string religionId) =>
        new(CivEventKind.ReligionAdded, civId, religionId);

    public static CivEvent ReligionRemoved(string civId, string religionId) =>
        new(CivEventKind.ReligionRemoved, civId, religionId);
}

/// <summary>
///     Outcome of a membership mutation: whether it succeeded and the ordered list
///     of events the facade should raise afterwards.
/// </summary>
internal readonly struct MembershipResult
{
    public MembershipResult(bool success, List<CivEvent> events)
    {
        Success = success;
        Events = events;
    }

    public bool Success { get; }
    public List<CivEvent> Events { get; }

    public static MembershipResult Failed() => new(false, new List<CivEvent>());
    public static MembershipResult Ok(List<CivEvent> events) => new(true, events);
}
