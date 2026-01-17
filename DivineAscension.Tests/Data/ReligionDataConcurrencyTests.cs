using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DivineAscension.Data;
using DivineAscension.Models;
using DivineAscension.Models.Enum;
using Vintagestory.API.Common;
using Xunit;

namespace DivineAscension.Tests.Data;

/// <summary>
///     Thread safety tests for ReligionData model under concurrent access
/// </summary>
public class ReligionDataConcurrencyTests
{
    [Fact]
    public void ConcurrentMemberOperations_ShouldBeThreadSafe()
    {
        // Arrange
        var religion = new ReligionData(
            "religion-uid",
            "Test Religion",
            "Test Deity",
            "founder-uid",
            "Description",
            DeityDomain.Craft
        );

        const int concurrentAdds = 100;
        var tasks = new Task[concurrentAdds];

        // Act - Add members concurrently
        for (int i = 0; i < concurrentAdds; i++)
        {
            var memberUid = $"member_{i}";
            tasks[i] = Task.Run(() => religion.AddMemberUID(memberUid));
        }

        Task.WaitAll(tasks);

        // Assert
        var members = religion.MemberUIDs;
        // +1 for founder
        Assert.Equal(concurrentAdds + 1, members.Count);

        // Verify no duplicates
        Assert.Equal(members.Count, members.Distinct().Count());
    }

    [Fact]
    public void ConcurrentBlessingUnlocks_ShouldBeThreadSafe()
    {
        // Arrange
        var religion = new ReligionData(
            "religion-uid",
            "Test Religion",
            "Test Deity",
            "founder-uid",
            "Description",
            DeityDomain.Wild
        );

        const int concurrentUnlocks = 50;
        var tasks = new Task[concurrentUnlocks];

        // Act - Unlock blessings concurrently
        for (int i = 0; i < concurrentUnlocks; i++)
        {
            var blessingId = $"blessing_{i}";
            tasks[i] = Task.Run(() => religion.UnlockBlessing(blessingId));
        }

        Task.WaitAll(tasks);

        // Assert
        var unlocked = religion.UnlockedBlessings;
        Assert.Equal(concurrentUnlocks, unlocked.Count);

        // Verify all are unlocked
        for (int i = 0; i < concurrentUnlocks; i++)
        {
            var blessingId = $"blessing_{i}";
            Assert.True(religion.IsBlessingUnlocked(blessingId));
        }
    }

    [Fact]
    public void ConcurrentRoleOperations_ShouldBeThreadSafe()
    {
        // Arrange
        var religion = new ReligionData(
            "religion-uid",
            "Test Religion",
            "Test Deity",
            "founder-uid",
            "Description",
            DeityDomain.Harvest
        );

        const int concurrentRoles = 30;
        var tasks = new Task[concurrentRoles];

        // Act - Set roles concurrently
        for (int i = 0; i < concurrentRoles; i++)
        {
            var roleUid = $"role_{i}";
            var role = new RoleData(roleUid, $"Role {i}", false, false, i);
            tasks[i] = Task.Run(() => religion.SetRole(roleUid, role));
        }

        Task.WaitAll(tasks);

        // Assert
        var roles = religion.Roles;
        // +2 for default Founder and Member roles
        Assert.Equal(concurrentRoles + 2, roles.Count);
    }

    [Fact]
    public void ConcurrentActivityLogEntries_ShouldBeThreadSafe()
    {
        // Arrange
        var religion = new ReligionData(
            "religion-uid",
            "Test Religion",
            "Test Deity",
            "founder-uid",
            "Description",
            DeityDomain.Stone
        );

        const int concurrentEntries = 200;
        const int maxEntries = 100;
        var tasks = new Task[concurrentEntries];

        // Act - Add activity entries concurrently
        for (int i = 0; i < concurrentEntries; i++)
        {
            var entry = new ActivityLogEntry(
                $"player_{i}",
                $"Action {i}",
                i % 10,
                i % 5,
                DeityDomain.Stone,
                DateTime.UtcNow
            );
            tasks[i] = Task.Run(() => religion.AddActivityEntry(entry, maxEntries));
        }

        Task.WaitAll(tasks);

        // Assert - Should maintain max entries limit
        var log = religion.ActivityLog;
        Assert.True(log.Count <= maxEntries);

        // Verify FIFO behavior - newest entries should be first
        if (log.Count > 1)
        {
            for (int i = 0; i < log.Count - 1; i++)
            {
                Assert.True(log[i].Timestamp >= log[i + 1].Timestamp);
            }
        }
    }

    [Fact]
    public void ConcurrentReadsDuringWrites_ShouldNotThrowExceptions()
    {
        // Arrange
        var religion = new ReligionData(
            "religion-uid",
            "Test Religion",
            "Test Deity",
            "founder-uid",
            "Description",
            DeityDomain.Craft
        );

        const int iterations = 1000;
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var exceptionCount = 0;

        // Act - Concurrent writes
        var writeTask = Task.Run(() =>
        {
            for (int i = 0; i < iterations; i++)
            {
                if (cts.Token.IsCancellationRequested) break;
                religion.AddMemberUID($"member_{i}");
                religion.UnlockBlessing($"blessing_{i}");
            }
        }, cts.Token);

        // Concurrent reads
        var readTask = Task.Run(() =>
        {
            for (int i = 0; i < iterations * 2; i++)
            {
                if (cts.Token.IsCancellationRequested) break;
                try
                {
                    var members = religion.MemberUIDs.ToList();
                    var blessings = religion.UnlockedBlessings.ToList();
                    var roles = religion.Roles.ToList();
                    var log = religion.ActivityLog.ToList();

                    // Should never throw
                    Assert.NotNull(members);
                    Assert.NotNull(blessings);
                }
                catch (Exception)
                {
                    Interlocked.Increment(ref exceptionCount);
                }
            }
        }, cts.Token);

        Task.WaitAll(writeTask, readTask);

        // Assert - No exceptions should occur
        Assert.Equal(0, exceptionCount);
    }

    [Fact]
    public void ConcurrentMemberRoleAssignments_ShouldBeThreadSafe()
    {
        // Arrange
        var religion = new ReligionData(
            "religion-uid",
            "Test Religion",
            "Test Deity",
            "founder-uid",
            "Description",
            DeityDomain.Wild
        );

        // Pre-add members
        const int memberCount = 50;
        for (int i = 0; i < memberCount; i++)
        {
            religion.AddMemberUID($"member_{i}");
        }

        // Create roles
        const int roleCount = 5;
        for (int i = 0; i < roleCount; i++)
        {
            var role = new RoleData($"role_{i}", $"Role {i}", false, false, i);
            religion.SetRole($"role_{i}", role);
        }

        var tasks = new Task[memberCount];

        // Act - Assign roles concurrently
        for (int i = 0; i < memberCount; i++)
        {
            var memberUid = $"member_{i}";
            var roleUid = $"role_{i % roleCount}";
            tasks[i] = Task.Run(() => religion.SetMemberRole(memberUid, roleUid));
        }

        Task.WaitAll(tasks);

        // Assert - All members should have roles assigned
        for (int i = 0; i < memberCount; i++)
        {
            var memberUid = $"member_{i}";
            var assignedRole = religion.GetPlayerRole(memberUid);
            Assert.NotNull(assignedRole);
        }
    }

    [Fact]
    public void StressTest_MixedConcurrentOperations_ShouldMaintainConsistency()
    {
        // Arrange
        var religion = new ReligionData(
            "religion-uid",
            "Test Religion",
            "Test Deity",
            "founder-uid",
            "Description",
            DeityDomain.Harvest
        );

        const int duration = 5; // 5 seconds
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(duration));
        var errorCount = 0;

        // Act - Multiple concurrent operations
        var memberTask = Task.Run(() =>
        {
            int i = 0;
            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    religion.AddMemberUID($"member_{i}");
                    i++;
                }
                catch
                {
                    Interlocked.Increment(ref errorCount);
                }
                Thread.Sleep(1);
            }
        }, cts.Token);

        var blessingTask = Task.Run(() =>
        {
            int i = 0;
            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    religion.UnlockBlessing($"blessing_{i}");
                    i++;
                }
                catch
                {
                    Interlocked.Increment(ref errorCount);
                }
                Thread.Sleep(2);
            }
        }, cts.Token);

        var activityTask = Task.Run(() =>
        {
            int i = 0;
            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    var entry = new ActivityLogEntry(
                        $"player_{i}",
                        "Action",
                        10,
                        5,
                        DeityDomain.Harvest,
                        DateTime.UtcNow
                    );
                    religion.AddActivityEntry(entry, 100);
                    i++;
                }
                catch
                {
                    Interlocked.Increment(ref errorCount);
                }
                Thread.Sleep(3);
            }
        }, cts.Token);

        var readTask = Task.Run(() =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    var _ = religion.MemberUIDs.ToList();
                    var __ = religion.UnlockedBlessings.ToList();
                    var ___ = religion.ActivityLog.ToList();
                }
                catch
                {
                    Interlocked.Increment(ref errorCount);
                }
            }
        }, cts.Token);

        Task.WaitAll(memberTask, blessingTask, activityTask, readTask);

        // Assert - No errors should occur
        Assert.Equal(0, errorCount);

        // Verify state consistency
        Assert.NotEmpty(religion.MemberUIDs);
        Assert.NotEmpty(religion.UnlockedBlessings);
    }
}
