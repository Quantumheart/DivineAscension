namespace DivineAscension.API.Interfaces;

/// <summary>
/// Abstraction for accessing game world time.
/// Provides a thin wrapper over Vintage Story's time API for improved testability.
/// </summary>
public interface ITimeService
{
    /// <summary>
    /// Gets the total elapsed milliseconds since the world was created.
    /// This is a monotonically increasing value used for absolute timestamps.
    /// </summary>
    long ElapsedMilliseconds { get; }
}
