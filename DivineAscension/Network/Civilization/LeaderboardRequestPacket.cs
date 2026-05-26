using ProtoBuf;

namespace DivineAscension.Network.Civilization;

/// <summary>
///     Client request for the Standing of Realms leaderboard. Slice 1 ships a
///     single board (Standing), so the packet carries no board selector yet.
/// </summary>
[ProtoContract]
public class LeaderboardRequestPacket
{
    public LeaderboardRequestPacket() { }
}
