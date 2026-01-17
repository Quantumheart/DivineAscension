using System.Diagnostics.CodeAnalysis;
using DivineAscension.Data;

namespace DivineAscension.Tests.ThreadSafety;

/// <summary>
/// Concurrency tests for PlayerProgressionData thread-safety
/// </summary>
[ExcludeFromCodeCoverage]
public class PlayerProgressionConcurrencyTests
{
    [Fact]
    public void ConcurrentFavorOperations_NoExceptions()
    {
        // Arrange
        var data = new PlayerProgressionData("player-1");
        var exceptions = new List<Exception>();
        var threads = new List<Thread>();
        const int threadCount = 50;
        const int operationsPerThread = 100;

        // Act - Half threads add, half remove favor
        for (int t = 0; t < threadCount; t++)
        {
            var threadIndex = t;
            var thread = new Thread(() =>
            {
                try
                {
                    for (int i = 0; i < operationsPerThread; i++)
                    {
                        if (threadIndex % 2 == 0)
                        {
                            data.AddFavor(10);
                        }
                        else
                        {
                            data.RemoveFavor(5);
                        }

                        // Read during modifications
                        _ = data.Favor;
                        _ = data.TotalFavorEarned;
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            });
            threads.Add(thread);
        }

        foreach (var thread in threads) thread.Start();
        foreach (var thread in threads) thread.Join();

        // Assert
        Assert.Empty(exceptions);
    }

    [Fact]
    public void ConcurrentBlessingUnlockLock_NoExceptions()
    {
        // Arrange
        var data = new PlayerProgressionData("player-1");
        var exceptions = new List<Exception>();
        var threads = new List<Thread>();
        const int threadCount = 50;
        const int operationsPerThread = 100;

        // Act
        for (int t = 0; t < threadCount; t++)
        {
            var threadIndex = t;
            var thread = new Thread(() =>
            {
                try
                {
                    for (int i = 0; i < operationsPerThread; i++)
                    {
                        var blessingId = $"blessing-{threadIndex}-{i}";
                        if (threadIndex % 2 == 0)
                        {
                            data.UnlockBlessing(blessingId);
                        }
                        else
                        {
                            data.LockBlessing($"blessing-{threadIndex - 1}-{i}");
                        }

                        // Check during modifications
                        _ = data.IsBlessingUnlocked(blessingId);
                        // Iterate during modifications
                        var snapshot = data.UnlockedBlessings.ToList();
                        _ = snapshot.Count;
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            });
            threads.Add(thread);
        }

        foreach (var thread in threads) thread.Start();
        foreach (var thread in threads) thread.Join();

        // Assert
        Assert.Empty(exceptions);
    }

    [Fact]
    public void ConcurrentFractionalFavorAccumulation_NoExceptions()
    {
        // Arrange
        var data = new PlayerProgressionData("player-1");
        var exceptions = new List<Exception>();
        var threads = new List<Thread>();
        const int threadCount = 50;
        const int operationsPerThread = 100;

        // Act
        for (int t = 0; t < threadCount; t++)
        {
            var thread = new Thread(() =>
            {
                try
                {
                    for (int i = 0; i < operationsPerThread; i++)
                    {
                        data.AddFractionalFavor(0.1f);
                        // Read during modifications
                        _ = data.AccumulatedFractionalFavor;
                        _ = data.Favor;
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            });
            threads.Add(thread);
        }

        foreach (var thread in threads) thread.Start();
        foreach (var thread in threads) thread.Join();

        // Assert
        Assert.Empty(exceptions);
    }

    [Fact]
    public void ConcurrentApplySwitchPenalty_NoExceptions()
    {
        // Arrange
        var data = new PlayerProgressionData("player-1");
        // Pre-populate
        data.AddFavor(1000);
        for (int i = 0; i < 50; i++)
        {
            data.UnlockBlessing($"blessing-{i}");
        }

        var exceptions = new List<Exception>();
        var threads = new List<Thread>();
        const int threadCount = 20;
        const int operationsPerThread = 50;

        // Act - Mix of switch penalties and regular operations
        for (int t = 0; t < threadCount; t++)
        {
            var threadIndex = t;
            var thread = new Thread(() =>
            {
                try
                {
                    for (int i = 0; i < operationsPerThread; i++)
                    {
                        if (threadIndex % 4 == 0)
                        {
                            data.ApplySwitchPenalty();
                        }
                        else if (threadIndex % 4 == 1)
                        {
                            data.AddFavor(10);
                        }
                        else if (threadIndex % 4 == 2)
                        {
                            data.UnlockBlessing($"blessing-new-{threadIndex}-{i}");
                        }
                        else
                        {
                            // Reader
                            _ = data.Favor;
                            var snapshot = data.UnlockedBlessings.ToList();
                            _ = snapshot.Count;
                        }
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            });
            threads.Add(thread);
        }

        foreach (var thread in threads) thread.Start();
        foreach (var thread in threads) thread.Join();

        // Assert
        Assert.Empty(exceptions);
    }

    [Fact]
    public void IterateBlessingsDuringModification_ReturnsConsistentSnapshot()
    {
        // Arrange
        var data = new PlayerProgressionData("player-1");
        var exceptions = new List<Exception>();
        var inconsistencies = new List<string>();
        var threads = new List<Thread>();
        const int threadCount = 20;
        const int operationsPerThread = 500;

        // Pre-populate
        for (int i = 0; i < 50; i++)
        {
            data.UnlockBlessing($"initial-blessing-{i}");
        }

        // Act
        for (int t = 0; t < threadCount; t++)
        {
            var threadIndex = t;
            var isWriter = t % 2 == 0;
            var thread = new Thread(() =>
            {
                try
                {
                    for (int i = 0; i < operationsPerThread; i++)
                    {
                        if (isWriter)
                        {
                            var blessingId = $"blessing-{threadIndex}-{i}";
                            if (i % 2 == 0)
                            {
                                data.UnlockBlessing(blessingId);
                            }
                            else
                            {
                                data.LockBlessing($"blessing-{threadIndex}-{i - 1}");
                            }
                        }
                        else
                        {
                            // Reader: take snapshot and verify consistency
                            var snapshot = data.UnlockedBlessings.ToList();
                            // Verify no duplicates in snapshot
                            var distinctCount = snapshot.Distinct().Count();
                            if (distinctCount != snapshot.Count)
                            {
                                lock (inconsistencies)
                                {
                                    inconsistencies.Add(
                                        $"Found duplicates in snapshot: {snapshot.Count} vs {distinctCount}");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            });
            threads.Add(thread);
        }

        foreach (var thread in threads) thread.Start();
        foreach (var thread in threads) thread.Join();

        // Assert
        Assert.Empty(exceptions);
        Assert.Empty(inconsistencies);
    }

    [Fact]
    public void ConcurrentClearBlessings_NoExceptions()
    {
        // Arrange
        var data = new PlayerProgressionData("player-1");
        var exceptions = new List<Exception>();
        var threads = new List<Thread>();
        const int threadCount = 30;
        const int operationsPerThread = 100;

        // Act
        for (int t = 0; t < threadCount; t++)
        {
            var threadIndex = t;
            var thread = new Thread(() =>
            {
                try
                {
                    for (int i = 0; i < operationsPerThread; i++)
                    {
                        if (threadIndex % 3 == 0)
                        {
                            data.ClearUnlockedBlessings();
                        }
                        else if (threadIndex % 3 == 1)
                        {
                            data.UnlockBlessing($"blessing-{threadIndex}-{i}");
                        }
                        else
                        {
                            // Reader
                            var snapshot = data.UnlockedBlessings.ToList();
                            _ = snapshot.Count;
                        }
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            });
            threads.Add(thread);
        }

        foreach (var thread in threads) thread.Start();
        foreach (var thread in threads) thread.Join();

        // Assert
        Assert.Empty(exceptions);
    }
}