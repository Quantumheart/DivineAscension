using DivineAscension.API.Interfaces;

namespace DivineAscension.Tests.Helpers;

/// <summary>
/// Fake implementation of ITimeService for testing time-dependent logic.
/// Provides manual control over elapsed time for deterministic tests.
/// </summary>
public class FakeTimeService : ITimeService
{
    private long _elapsedMs;

    /// <inheritdoc />
    public long ElapsedMilliseconds => _elapsedMs;

    /// <summary>
    /// Sets the elapsed milliseconds to a specific value.
    /// </summary>
    /// <param name="ms">The new elapsed time in milliseconds</param>
    public void SetElapsedMilliseconds(long ms)
    {
        _elapsedMs = ms;
    }

    /// <summary>
    /// Advances the elapsed time by a specified amount.
    /// </summary>
    /// <param name="ms">The number of milliseconds to advance</param>
    public void AdvanceTimeBy(long ms)
    {
        _elapsedMs += ms;
    }

    /// <summary>
    /// Resets the elapsed time to zero.
    /// </summary>
    public void Reset()
    {
        _elapsedMs = 0;
    }
}
