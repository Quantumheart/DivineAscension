using System.Diagnostics.CodeAnalysis;
using DivineAscension.Data;

namespace DivineAscension.Tests.ThreadSafety;

/// <summary>
/// Concurrency tests for Civilization data class thread-safety
/// </summary>
[ExcludeFromCodeCoverage]
public class CivilizationDataConcurrencyTests
{
    [Fact]
    public void ConcurrentReligionAddRemove_NoExceptions()
    {
        // Arrange
        var civ = new Civilization("civ-1", "Test Civilization", "founder-1", "religion-founder");
        var exceptions = new List<Exception>();
        var threads = new List<Thread>();
        const int threadCount = 50;
        const int operationsPerThread = 100;

        // Act - Half threads add, half remove religions
        for (int t = 0; t < threadCount; t++)
        {
            var threadIndex = t;
            var thread = new Thread(() =>
            {
                try
                {
                    for (int i = 0; i < operationsPerThread; i++)
                    {
                        var religionId = $"religion-{threadIndex}-{i}";
                        if (threadIndex % 2 == 0)
                        {
                            civ.AddReligion(religionId);
                        }
                        else
                        {
                            civ.RemoveReligion($"religion-{threadIndex - 1}-{i}");
                        }
                        // Also iterate during modifications
                        var snapshot = civ.MemberReligionIds.ToList();
                        _ = snapshot.Count;
                        // Check validity
                        _ = civ.IsValid;
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
    public void ConcurrentHasReligionCheck_NoExceptions()
    {
        // Arrange
        var civ = new Civilization("civ-1", "Test Civilization", "founder-1", "religion-founder");
        // Add some religions
        civ.AddReligion("religion-1");
        civ.AddReligion("religion-2");

        var exceptions = new List<Exception>();
        var threads = new List<Thread>();
        const int threadCount = 50;
        const int operationsPerThread = 1000;

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
                        // Mix of reads and writes
                        if (i % 10 == 0 && threadIndex % 2 == 0)
                        {
                            civ.AddReligion($"religion-new-{threadIndex}-{i}");
                        }
                        else if (i % 10 == 5 && threadIndex % 2 == 1)
                        {
                            civ.RemoveReligion($"religion-new-{threadIndex - 1}-{i - 5}");
                        }
                        else
                        {
                            _ = civ.HasReligion("religion-1");
                            _ = civ.HasReligion("religion-2");
                            _ = civ.HasReligion("religion-nonexistent");
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
    public void SnapshotMethod_ReturnsConsistentData()
    {
        // Arrange
        var civ = new Civilization("civ-1", "Test Civilization", "founder-1", "religion-founder");
        var exceptions = new List<Exception>();
        var inconsistencies = new List<string>();
        var threads = new List<Thread>();
        const int threadCount = 20;
        const int operationsPerThread = 500;

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
                            var religionId = $"religion-{threadIndex}-{i}";
                            if (i % 2 == 0)
                            {
                                civ.AddReligion(religionId);
                            }
                            else
                            {
                                civ.RemoveReligion($"religion-{threadIndex}-{i - 1}");
                            }
                        }
                        else
                        {
                            // Reader: take snapshot and verify consistency
                            var snapshot = civ.GetMemberReligionIdsSnapshot();
                            // Verify no duplicates
                            var distinctCount = snapshot.Distinct().Count();
                            if (distinctCount != snapshot.Count)
                            {
                                lock (inconsistencies)
                                {
                                    inconsistencies.Add($"Found duplicates: {snapshot.Count} vs {distinctCount}");
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
