using Vintagestory.API.Common;
using ILogger = DivineAscension.Services.Abstractions.ILogger;

namespace DivineAscension.Adapters;

/// <summary>
/// Adapts Vintage Story's ILogger to the Services ILogger abstraction.
/// </summary>
internal sealed class VintageStoryLogger(ICoreAPI api) : ILogger
{
    private const string LogPrefix = "[DivineAscension]";

    public void Debug(string message)
    {
        api.Logger.Debug($"{LogPrefix} {message}");
    }

    public void Info(string message)
    {
        api.Logger.Notification($"{LogPrefix} {message}");
    }

    public void Warning(string message)
    {
        api.Logger.Warning($"{LogPrefix} {message}");
    }

    public void Error(string message)
    {
        api.Logger.Error($"{LogPrefix} {message}");
    }
}