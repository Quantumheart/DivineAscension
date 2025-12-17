using Vintagestory.API.Client;

namespace PantheonWars.Systems.Networking.Interfaces;

public interface IClientNetworkHandler
{
    void Initialize(ICoreClientAPI capi);
    void RegisterHandlers(IClientNetworkChannel channel);
    void Dispose();
}