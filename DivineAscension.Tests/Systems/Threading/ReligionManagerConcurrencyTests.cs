using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DivineAscension.Data;
using DivineAscension.Models.Enum;
using DivineAscension.Systems;
using Moq;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Xunit;

namespace DivineAscension.Tests.Systems.Threading;

/// <summary>
///     Thread safety tests for ReligionManager under concurrent access
/// </summary>
public class ReligionManagerConcurrencyTests
{
    private readonly Mock<ICoreServerAPI> _mockSapi;
    private readonly Mock<ILogger> _mockLogger;

    public ReligionManagerConcurrencyTests()
    {
        _mockSapi = new Mock<ICoreServerAPI>();
        _mockLogger = new Mock<ILogger>();
        _mockSapi.Setup(x => x.Logger).Returns(_mockLogger.Object);
    }

    [Fact]
    public void ConcurrentReligionCreation_ShouldNotCorruptState()
    {
        // Arrange
        var manager = new ReligionManager(_mockSapi.Object);
        const int concurrentCreations = 50;
        var tasks = new Task<(bool success, string religionUID, string error)>[concurrentCreations];

        // Act - Create 50 religions concurrently
        for (int i = 0; i < concurrentCreations; i++)
        {
            var index = i;
            tasks[i] = Task.Run(() => manager.CreateReligion(
                $"player_{index}",
                $"Religion {index}",
                $"Deity {index}",
                $"Description {index}",
                DeityDomain.Craft
            ));
        }

        Task.WaitAll(tasks);

        // Assert
        var successCount = tasks.Count(t => t.Result.success);
        var allReligions = manager.GetAllReligions();

        Assert.Equal(concurrentCreations, successCount);
        Assert.Equal(concurrentCreations, allReligions.Count);

        // Verify no duplicate UIDs
        var uids = allReligions.Select(r => r.ReligionUID).ToList();
        Assert.Equal(uids.Count, uids.Distinct().Count());
    }

    [Fact]
    public void ConcurrentMembershipOperations_ShouldMaintainConsistency()
    {
        // Arrange
        var manager = new ReligionManager(_mockSapi.Object);
        var (success, religionUID, _) = manager.CreateReligion(
            "founder",
            "Test Religion",
            "Test Deity",
            "Description",
            DeityDomain.Craft
        );
        Assert.True(success);

        const int concurrentPlayers = 100;
        var tasks = new Task<(bool success, string error)>[concurrentPlayers];

        // Act - Add 100 players concurrently
        for (int i = 0; i < concurrentPlayers; i++)
        {
            var playerUid = $"player_{i}";
            tasks[i] = Task.Run(() => manager.AddMember(religionUID, playerUid));
        }

        Task.WaitAll(tasks);

        // Assert
        var religion = manager.GetReligion(religionUID);
        Assert.NotNull(religion);

        // +1 for founder
        Assert.Equal(concurrentPlayers + 1, religion.MemberUIDs.Count);

        // Verify all players are indexed correctly
        for (int i = 0; i < concurrentPlayers; i++)
        {
            var playerUid = $"player_{i}";
            Assert.True(manager.HasReligion(playerUid));
            Assert.Equal(religionUID, manager.GetPlayerReligion(playerUid)?.ReligionUID);
        }
    }

    [Fact]
    public void ConcurrentAddAndRemoveMembers_ShouldNotDeadlock()
    {
        // Arrange
        var manager = new ReligionManager(_mockSapi.Object);
        var (success, religionUID, _) = manager.CreateReligion(
            "founder",
            "Test Religion",
            "Test Deity",
            "Description",
            DeityDomain.Wild
        );
        Assert.True(success);

        // Pre-add some members
        for (int i = 0; i < 50; i++)
        {
            manager.AddMember(religionUID, $"player_{i}");
        }

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        const int operations = 200;

        // Act - Concurrent adds and removes
        var addTask = Task.Run(() =>
        {
            for (int i = 50; i < 50 + operations; i++)
            {
                if (cts.Token.IsCancellationRequested) break;
                manager.AddMember(religionUID, $"player_{i}");
            }
        }, cts.Token);

        var removeTask = Task.Run(() =>
        {
            for (int i = 0; i < operations; i++)
            {
                if (cts.Token.IsCancellationRequested) break;
                manager.RemoveMember(religionUID, $"player_{i}");
            }
        }, cts.Token);

        // Should complete without deadlock
        Assert.True(Task.WaitAll(new[] { addTask, removeTask }, TimeSpan.FromSeconds(10)));

        // Assert - State should be consistent (no corrupted data)
        var religion = manager.GetReligion(religionUID);
        Assert.NotNull(religion);

        // Verify index consistency
        foreach (var memberUid in religion.MemberUIDs)
        {
            Assert.True(manager.HasReligion(memberUid));
            Assert.Equal(religionUID, manager.GetPlayerReligion(memberUid)?.ReligionUID);
        }
    }

    [Fact]
    public void ConcurrentReadsAndWrites_ShouldProduceConsistentResults()
    {
        // Arrange
        var manager = new ReligionManager(_mockSapi.Object);
        var (success, religionUID, _) = manager.CreateReligion(
            "founder",
            "Test Religion",
            "Test Deity",
            "Description",
            DeityDomain.Harvest
        );
        Assert.True(success);

        const int iterations = 1000;
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var readErrors = 0;

        // Act - Concurrent reads while writing
        var writeTask = Task.Run(() =>
        {
            for (int i = 0; i < iterations; i++)
            {
                if (cts.Token.IsCancellationRequested) break;
                manager.AddMember(religionUID, $"player_{i}");
            }
        }, cts.Token);

        var readTask = Task.Run(() =>
        {
            for (int i = 0; i < iterations * 2; i++)
            {
                if (cts.Token.IsCancellationRequested) break;
                try
                {
                    var religion = manager.GetReligion(religionUID);
                    var members = religion?.MemberUIDs.ToList(); // Snapshot iteration
                    var allReligions = manager.GetAllReligions();

                    // Should never get null or inconsistent state
                    Assert.NotNull(religion);
                    Assert.NotNull(members);
                }
                catch (Exception)
                {
                    Interlocked.Increment(ref readErrors);
                }
            }
        }, cts.Token);

        Task.WaitAll(writeTask, readTask);

        // Assert - No read errors should occur
        Assert.Equal(0, readErrors);
    }

    [Fact]
    public void ConcurrentReligionDeletion_ShouldNotCauseRaceConditions()
    {
        // Arrange
        var manager = new ReligionManager(_mockSapi.Object);
        const int religionCount = 20;
        var religionUids = new List<string>();

        for (int i = 0; i < religionCount; i++)
        {
            var (success, uid, _) = manager.CreateReligion(
                $"founder_{i}",
                $"Religion {i}",
                $"Deity {i}",
                "Description",
                DeityDomain.Stone
            );
            Assert.True(success);
            religionUids.Add(uid);

            // Add members to each religion
            for (int j = 0; j < 5; j++)
            {
                manager.AddMember(uid, $"religion_{i}_member_{j}");
            }
        }

        var tasks = new Task<(bool success, string error)>[religionCount];

        // Act - Delete all religions concurrently
        for (int i = 0; i < religionCount; i++)
        {
            var uid = religionUids[i];
            var founderUid = $"founder_{i}";
            tasks[i] = Task.Run(() => manager.DeleteReligion(uid, founderUid));
        }

        Task.WaitAll(tasks);

        // Assert
        Assert.Empty(manager.GetAllReligions());

        // Verify all players are no longer indexed
        for (int i = 0; i < religionCount; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                var playerUid = $"religion_{i}_member_{j}";
                Assert.False(manager.HasReligion(playerUid));
            }
        }
    }

    [Fact]
    public void StressTest_MixedOperations_ShouldMaintainConsistency()
    {
        // Arrange
        var manager = new ReligionManager(_mockSapi.Object);
        const int duration = 5; // 5 seconds
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(duration));
        var createdReligions = new System.Collections.Concurrent.ConcurrentBag<string>();
        var operationCounts = new int[4]; // create, delete, addMember, removeMember

        // Act - Mixed concurrent operations
        var createTask = Task.Run(() =>
        {
            int i = 0;
            while (!cts.Token.IsCancellationRequested)
            {
                var (success, uid, _) = manager.CreateReligion(
                    $"founder_{i}",
                    $"Religion {i}",
                    $"Deity {i}",
                    "Test",
                    (DeityDomain)(i % 4 + 1)
                );
                if (success) createdReligions.Add(uid);
                Interlocked.Increment(ref operationCounts[0]);
                i++;
                Thread.Sleep(10);
            }
        }, cts.Token);

        var memberTask = Task.Run(() =>
        {
            int i = 0;
            while (!cts.Token.IsCancellationRequested)
            {
                var religions = manager.GetAllReligions();
                if (religions.Any())
                {
                    var religion = religions.First();
                    manager.AddMember(religion.ReligionUID, $"member_{i}");
                    Interlocked.Increment(ref operationCounts[2]);
                }
                i++;
                Thread.Sleep(5);
            }
        }, cts.Token);

        var readTask = Task.Run(() =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                var _ = manager.GetAllReligions();
                Thread.Sleep(1);
            }
        }, cts.Token);

        Task.WaitAll(createTask, memberTask, readTask);

        // Assert - State should be consistent
        var finalReligions = manager.GetAllReligions();
        Assert.NotEmpty(finalReligions);

        // Verify index consistency for all religions
        foreach (var religion in finalReligions)
        {
            foreach (var memberUid in religion.MemberUIDs)
            {
                Assert.True(manager.HasReligion(memberUid));
                var playerReligion = manager.GetPlayerReligion(memberUid);
                Assert.Equal(religion.ReligionUID, playerReligion?.ReligionUID);
            }
        }

        // Log operation counts for visibility
        Assert.True(operationCounts[0] > 0); // Creates
        Assert.True(operationCounts[2] > 0); // Member adds
    }
}
