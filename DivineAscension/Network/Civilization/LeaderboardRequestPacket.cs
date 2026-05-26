using ProtoBuf;

namespace DivineAscension.Network.Civilization;

/// <summary>
///     Client request for the Standing of Realms leaderboard. The server returns
///     every board in one response and the client switches between them locally,
///     so the request carries no board selector.
/// </summary>
[ProtoContract]
public class LeaderboardRequestPacket
{
    public LeaderboardRequestPacket() { }
}
