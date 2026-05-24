using ProtoBuf;

namespace DivineAscension.Network;

/// <summary>
/// Server -> client: instruct the recipient to close the DivineAscension dialog.
/// Sent when the player walks away from the lectern they opened the menu at.
/// </summary>
[ProtoContract]
public class CloseMenuPacket
{
    public CloseMenuPacket()
    {
    }
}
