using ProtoBuf;

namespace DivineAscension.Network;

/// <summary>
///     Server push fired alongside the chat broadcast on the day a religion
///     or civilization holiday is kept. The client queues a transient toast
///     via NotificationManager. The toast dismisses on click but, unlike
///     rank-up toasts, does not open the main dialog — the chronicle and
///     Letters page are the durable surfaces for this information.
/// </summary>
[ProtoContract]
public class HolidayKeptToastPacket
{
    public HolidayKeptToastPacket()
    {
    }

    public HolidayKeptToastPacket(string feastName, string description, string domain)
    {
        FeastName = feastName;
        Description = description;
        Domain = domain;
    }

    /// <summary>Display title for the toast (e.g. "Founding Day").</summary>
    [ProtoMember(1)] public string FeastName { get; set; } = string.Empty;

    /// <summary>One-line body (e.g. "Kept by the Order of the Forge today").</summary>
    [ProtoMember(2)] public string Description { get; set; } = string.Empty;

    /// <summary>
    ///     <see cref="DivineAscension.Models.Enum.DeityDomain"/> name; drives
    ///     the toast's deity glyph. Empty for civilization-wide holidays
    ///     that don't belong to a single domain.
    /// </summary>
    [ProtoMember(3)] public string Domain { get; set; } = string.Empty;
}
