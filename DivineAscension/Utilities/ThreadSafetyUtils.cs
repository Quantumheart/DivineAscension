using System;
using System.Diagnostics;
using Vintagestory.API.Common;

namespace DivineAscension.Utilities;

/// <summary>
/// Utility class for thread-safe operations with contention telemetry support.
/// Logs warnings when locks are held or waited on for longer than the threshold.
/// </summary>
internal static class ThreadSafetyUtils
{
    private const int LockContentionThresholdMs = 100;
    private static ICoreAPI? _api;

    /// <summary>
    /// Initializes the thread safety utilities with an API reference for logging.
    /// Call this from both StartServerSide and StartClientSide.
    /// </summary>
    internal static void Initialize(ICoreAPI api)
    {
        _api = api;
    }

    /// <summary>
    /// Executes an action within a lock with contention telemetry.
    /// Logs warnings if waiting for or holding the lock exceeds the threshold.
    /// </summary>
    /// <param name="lockObj">The lock object to synchronize on.</param>
    /// <param name="action">The action to execute within the lock.</param>
    /// <param name="operationName">A descriptive name for the operation (used in log messages).</param>
    internal static void WithLock(object lockObj, Action action, string operationName)
    {
        var sw = Stopwatch.StartNew();
        lock (lockObj)
        {
            var waitTime = sw.ElapsedMilliseconds;
            if (waitTime > LockContentionThresholdMs)
            {
                _api?.Logger.Warning(
                    $"[DivineAscension] Lock contention detected: {operationName} waited {waitTime}ms to acquire lock");
            }

            sw.Restart();
            try
            {
                action();
            }
            finally
            {
                var holdTime = sw.ElapsedMilliseconds;
                if (holdTime > LockContentionThresholdMs)
                {
                    _api?.Logger.Warning(
                        $"[DivineAscension] Long lock hold: {operationName} held lock for {holdTime}ms");
                }
            }
        }
    }

    /// <summary>
    /// Executes a function within a lock with contention telemetry, returning the result.
    /// Logs warnings if waiting for or holding the lock exceeds the threshold.
    /// </summary>
    /// <typeparam name="T">The return type of the function.</typeparam>
    /// <param name="lockObj">The lock object to synchronize on.</param>
    /// <param name="func">The function to execute within the lock.</param>
    /// <param name="operationName">A descriptive name for the operation (used in log messages).</param>
    /// <returns>The result of the function.</returns>
    internal static T WithLock<T>(object lockObj, Func<T> func, string operationName)
    {
        var sw = Stopwatch.StartNew();
        lock (lockObj)
        {
            var waitTime = sw.ElapsedMilliseconds;
            if (waitTime > LockContentionThresholdMs)
            {
                _api?.Logger.Warning(
                    $"[DivineAscension] Lock contention detected: {operationName} waited {waitTime}ms to acquire lock");
            }

            sw.Restart();
            try
            {
                return func();
            }
            finally
            {
                var holdTime = sw.ElapsedMilliseconds;
                if (holdTime > LockContentionThresholdMs)
                {
                    _api?.Logger.Warning(
                        $"[DivineAscension] Long lock hold: {operationName} held lock for {holdTime}ms");
                }
            }
        }
    }
}
