using System;
using DivineAscension.API.Interfaces;

namespace DivineAscension.Tests.Helpers;

/// <summary>
/// Fake implementation of ITimeService for testing time-dependent logic.
/// Provides manual control over elapsed time for deterministic tests.
/// </summary>
public class FakeTimeService : ITimeService
{
    private long _elapsedMs;
    private DateTime _utcNow = DateTime.UtcNow;

    /// <inheritdoc />
    public long ElapsedMilliseconds => _elapsedMs;

    /// <inheritdoc />
    public DateTime UtcNow => _utcNow;

    /// <summary>
    /// Sets the elapsed milliseconds to a specific value.
    /// </summary>
    /// <param name="ms">The new elapsed time in milliseconds</param>
    public void SetElapsedMilliseconds(long ms)
    {
        _elapsedMs = ms;
    }

    /// <summary>
    /// Sets the UTC time to a specific value.
    /// </summary>
    /// <param name="utcNow">The new UTC time</param>
    public void SetUtcNow(DateTime utcNow)
    {
        _utcNow = utcNow;
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
    /// Advances the UTC time by a specified amount.
    /// </summary>
    /// <param name="duration">The duration to advance</param>
    public void AdvanceUtcBy(TimeSpan duration)
    {
        _utcNow = _utcNow.Add(duration);
    }

    /// <summary>
    /// Resets the elapsed time to zero.
    /// </summary>
    public void Reset()
    {
        _elapsedMs = 0;
    }

    /// <summary>
    /// Resets both elapsed time and UTC time.
    /// </summary>
    public void ResetAll()
    {
        _elapsedMs = 0;
        _utcNow = DateTime.UtcNow;
    }
}
