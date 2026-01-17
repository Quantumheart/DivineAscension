using System.Diagnostics.CodeAnalysis;
using DivineAscension.Data;
using DivineAscension.Models.Enum;

namespace DivineAscension.Tests.ThreadSafety;

/// <summary>
/// Concurrency tests for ReligionData thread-safety
/// </summary>
[ExcludeFromCodeCoverage]
public class ReligionDataConcurrencyTests
{
    [Fact]
    public void ConcurrentMemberAddRemove_NoExceptions()
    {
        // Arrange
        var religion = new ReligionData("test-religion", "Test Religion", DeityDomain.Craft, "Khoras", "founder-1", "Founder");
        var exceptions = new List<Exception>();
        var threads = new List<Thread>();
        const int threadCount = 50;
        const int operationsPerThread = 100;

        // Act - Half threads add, half remove members
        for (int t = 0; t < threadCount; t++)
        {
            var threadIndex = t;
            var thread = new Thread(() =>
            {
                try
                {
                    for (int i = 0; i < operationsPerThread; i++)
                    {
                        var playerId = $"player-{threadIndex}-{i}";
                        if (threadIndex % 2 == 0)
                        {
                            religion.AddMember(playerId, $"Player {threadIndex}-{i}");
                        }
                        else
                        {
                            religion.RemoveMember($"player-{threadIndex - 1}-{i}");
                        }
                        // Also iterate during modifications
                        var snapshot = religion.MemberUIDs.ToList();
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
    public void ConcurrentBlessingUnlockLock_NoExceptions()
    {
        // Arrange
        var religion = new ReligionData("test-religion", "Test Religion", DeityDomain.Craft, "Khoras", "founder-1", "Founder");
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
                            religion.UnlockBlessing(blessingId);
                        }
                        else
                        {
                            religion.LockBlessing($"blessing-{threadIndex - 1}-{i}");
                        }
                        // Check if blessing is unlocked
                        _ = religion.IsBlessingUnlocked(blessingId);
                        // Iterate during modifications
                        var snapshot = religion.UnlockedBlessings.ToList();
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
    public void ConcurrentRoleAssignment_NoExceptions()
    {
        // Arrange
        var religion = new ReligionData("test-religion", "Test Religion", DeityDomain.Craft, "Khoras", "founder-1", "Founder");
        // Create some roles first
        for (int i = 0; i < 10; i++)
        {
            religion.SetRole($"role-{i}", new RoleData($"role-{i}", $"Role {i}", false, false, i));
        }

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
                        var playerId = $"player-{threadIndex}-{i}";
                        var roleId = $"role-{i % 10}";
                        religion.AssignMemberRole(playerId, roleId);
                        // Read during modifications
                        var snapshot = religion.MemberRoles.ToList();
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
    public void ConcurrentActivityLogOperations_NoExceptions()
    {
        // Arrange
        var religion = new ReligionData("test-religion", "Test Religion", DeityDomain.Craft, "Khoras", "founder-1", "Founder");
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
                        var entry = new ActivityLogEntry(
                            $"player-{threadIndex}",
                            $"Player {threadIndex}",
                            "test-action",
                            i,
                            i / 10,
                            "Craft"
                        );
                        religion.AddActivityEntry(entry, 100);
                        // Read during modifications
                        var snapshot = religion.ActivityLog.ToList();
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
    public void IterateDuringModification_ReturnsConsistentSnapshot()
    {
        // Arrange
        var religion = new ReligionData("test-religion", "Test Religion", DeityDomain.Craft, "Khoras", "founder-1", "Founder");
        var exceptions = new List<Exception>();
        var inconsistencies = new List<string>();
        var threads = new List<Thread>();
        const int threadCount = 10;
        const int operationsPerThread = 1000;

        // Pre-populate with some members
        for (int i = 0; i < 50; i++)
        {
            religion.AddMember($"initial-player-{i}", $"Initial Player {i}");
        }

        // Act - Writer threads and reader threads
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
                            var playerId = $"player-{threadIndex}-{i}";
                            if (i % 2 == 0)
                            {
                                religion.AddMember(playerId, $"Player {threadIndex}-{i}");
                            }
                            else
                            {
                                religion.RemoveMember($"player-{threadIndex}-{i - 1}");
                            }
                        }
                        else
                        {
                            // Reader: take snapshot and verify consistency
                            var snapshot = religion.MemberUIDs.ToList();
                            // Verify no duplicates in snapshot
                            var distinctCount = snapshot.Distinct().Count();
                            if (distinctCount != snapshot.Count)
                            {
                                lock (inconsistencies)
                                {
                                    inconsistencies.Add($"Found duplicates in snapshot: {snapshot.Count} vs {distinctCount}");
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
}
