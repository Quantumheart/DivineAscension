using ProtoBuf;

namespace PantheonWars.Network;

/// <summary>
///     Client requests their own religion information (for "Religion > Info" tab)
/// </summary>
[ProtoContract]
public class PlayerReligionInfoRequestPacket
{
}