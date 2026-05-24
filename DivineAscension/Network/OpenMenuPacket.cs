using ProtoBuf;

namespace DivineAscension.Network;

/// <summary>
/// Server -> client: instruct the recipient to open the DivineAscension dialog.
/// Sent after a lectern interaction passes server validation.
/// </summary>
[ProtoContract]
public class OpenMenuPacket
{
    public OpenMenuPacket()
    {
    }

    /// <summary>X coordinate of the lectern used to open the menu.</summary>
    [ProtoMember(1)]
    public int LecternX { get; set; }

    /// <summary>Y coordinate of the lectern used to open the menu.</summary>
    [ProtoMember(2)]
    public int LecternY { get; set; }

    /// <summary>Z coordinate of the lectern used to open the menu.</summary>
    [ProtoMember(3)]
    public int LecternZ { get; set; }
}
