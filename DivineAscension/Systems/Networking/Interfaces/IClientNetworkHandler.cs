using Vintagestory.API.Client;

namespace DivineAscension.Systems.Networking.Interfaces;

public interface IClientNetworkHandler
{
    void Initialize(ICoreClientAPI capi);
    void RegisterHandlers(IClientNetworkChannel channel);
    void Dispose();
}