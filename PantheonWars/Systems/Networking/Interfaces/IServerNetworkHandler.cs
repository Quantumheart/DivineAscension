using Vintagestory.API.Server;

namespace PantheonWars.Systems.Networking.Interfaces;

public interface IServerNetworkHandler
{
    void Initialize(ICoreServerAPI sapi);
    void RegisterHandlers(IServerNetworkChannel channel);
    void Dispose();
}