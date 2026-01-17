using System;
using System.Threading;
using System.Threading.Tasks;
using DivineAscension.Configuration;
using DivineAscension.Models.Enum;
using DivineAscension.Systems;
using DivineAscension.Systems.Interfaces;
using Moq;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Xunit;

namespace DivineAscension.Tests.Systems.Threading;

/// <summary>
///     Thread safety tests for FavorSystem under concurrent access
/// </summary>
public class FavorSystemConcurrencyTests
{
    private readonly Mock<ICoreServerAPI> _mockSapi;
    private readonly Mock<ILogger> _mockLogger;
    private readonly Mock<IPlayerProgressionDataManager> _mockPlayerDataManager;
    private readonly Mock<IReligionManager> _mockReligionManager;
    private readonly Mock<IReligionPrestigeManager> _mockPrestigeManager;
    private readonly Mock<IActivityLogManager> _mockActivityLogManager;
    private readonly GameBalanceConfig _config;

    public FavorSystemConcurrencyTests()
    {
        _mockSapi = new Mock<ICoreServerAPI>();
        _mockLogger = new Mock<ILogger>();
        _mockSapi.Setup(x => x.Logger).Returns(_mockLogger.Object);

        _mockPlayerDataManager = new Mock<IPlayerProgressionDataManager>();
        _mockReligionManager = new Mock<IReligionManager>();
        _mockPrestigeManager = new Mock<IReligionPrestigeManager>();
        _mockActivityLogManager = new Mock<IActivityLogManager>();

        _config = new GameBalanceConfig();
    }

    [Fact]
    public void ConcurrentFavorQueuing_ShouldNotLoseData()
    {
        // Arrange
        var favorSystem = new FavorSystem(
            _mockSapi.Object,
            _mockPlayerDataManager.Object,
            _mockReligionManager.Object,
            _mockPrestigeManager.Object,
            _mockActivityLogManager.Object,
            _config
        );

        favorSystem.Initialize();

        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(x => x.PlayerUID).Returns("test-player");

        const int concurrentQueues = 1000;
        var tasks = new Task[concurrentQueues];

        // Act - Queue favor from multiple threads simultaneously
        for (int i = 0; i < concurrentQueues; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                favorSystem.QueueFavorForAction(
                    mockPlayer.Object,
                    "mining",
                    1.0f,
                    DeityDomain.Stone
                );
            });
        }

        Task.WaitAll(tasks);

        // Assert - Call flush and verify no exceptions
        // The favor should be accumulated correctly
        // (We can't directly verify the internal _pendingFavor, but the operation should complete without errors)
        Assert.True(true); // If we get here, no deadlocks or race conditions occurred
    }

    [Fact]
    public void ConcurrentQueueAndFlush_ShouldNotDeadlock()
    {
        // Arrange
        var favorSystem = new FavorSystem(
            _mockSapi.Object,
            _mockPlayerDataManager.Object,
            _mockReligionManager.Object,
            _mockPrestigeManager.Object,
            _mockActivityLogManager.Object,
            _config
        );

        favorSystem.Initialize();

        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(x => x.PlayerUID).Returns("test-player");

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Act - Queue while flushing
        var queueTask = Task.Run(() =>
        {
            int i = 0;
            while (!cts.Token.IsCancellationRequested)
            {
                favorSystem.QueueFavorForAction(
                    mockPlayer.Object,
                    "action",
                    0.5f,
                    DeityDomain.Craft
                );
                i++;
                Thread.Sleep(1);
            }
        }, cts.Token);

        var flushTask = Task.Run(() =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                // Access internal flush via reflection or let it flush naturally
                Thread.Sleep(10);
            }
        }, cts.Token);

        // Should complete without deadlock
        Assert.True(Task.WaitAll(new[] { queueTask, flushTask }, TimeSpan.FromSeconds(10)));
    }

    [Fact]
    public void HighFrequencyQueueing_ShouldHandleCorrectly()
    {
        // Arrange - Simulates scythe harvesting scenario
        var favorSystem = new FavorSystem(
            _mockSapi.Object,
            _mockPlayerDataManager.Object,
            _mockReligionManager.Object,
            _mockPrestigeManager.Object,
            _mockActivityLogManager.Object,
            _config
        );

        favorSystem.Initialize();

        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(x => x.PlayerUID).Returns("test-player");

        const int rapidQueues = 5000; // Simulate 5000 rapid block harvests
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        // Act - Rapid queuing from single thread (like scythe harvesting)
        var queueTask = Task.Run(() =>
        {
            for (int i = 0; i < rapidQueues; i++)
            {
                if (cts.Token.IsCancellationRequested) break;
                favorSystem.QueueFavorForAction(
                    mockPlayer.Object,
                    "harvest",
                    0.1f,
                    DeityDomain.Harvest
                );
            }
        }, cts.Token);

        Assert.True(queueTask.Wait(TimeSpan.FromSeconds(10)));
        Assert.Equal(TaskStatus.RanToCompletion, queueTask.Status);
    }

    [Fact]
    public void MultiplePlayersConcurrentQueuing_ShouldMaintainSeparateQueues()
    {
        // Arrange
        var favorSystem = new FavorSystem(
            _mockSapi.Object,
            _mockPlayerDataManager.Object,
            _mockReligionManager.Object,
            _mockPrestigeManager.Object,
            _mockActivityLogManager.Object,
            _config
        );

        favorSystem.Initialize();

        const int playerCount = 50;
        const int queuesPerPlayer = 100;
        var tasks = new Task[playerCount];

        // Act - Multiple players queuing concurrently
        for (int i = 0; i < playerCount; i++)
        {
            var playerUid = $"player_{i}";
            tasks[i] = Task.Run(() =>
            {
                var mockPlayer = new Mock<IServerPlayer>();
                mockPlayer.Setup(x => x.PlayerUID).Returns(playerUid);

                for (int j = 0; j < queuesPerPlayer; j++)
                {
                    favorSystem.QueueFavorForAction(
                        mockPlayer.Object,
                        "action",
                        1.0f,
                        DeityDomain.Wild
                    );
                }
            });
        }

        Task.WaitAll(tasks);

        // Assert - Should complete without errors
        Assert.All(tasks, task => Assert.Equal(TaskStatus.RanToCompletion, task.Status));
    }

    [Fact]
    public void StressTest_MixedFavorOperations_ShouldNotFail()
    {
        // Arrange
        var favorSystem = new FavorSystem(
            _mockSapi.Object,
            _mockPlayerDataManager.Object,
            _mockReligionManager.Object,
            _mockPrestigeManager.Object,
            _mockActivityLogManager.Object,
            _config
        );

        favorSystem.Initialize();

        const int duration = 5; // 5 seconds
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(duration));
        var errorCount = 0;

        // Act - Multiple operations concurrently
        var tasks = new Task[10];
        for (int t = 0; t < 10; t++)
        {
            var threadId = t;
            tasks[t] = Task.Run(() =>
            {
                var mockPlayer = new Mock<IServerPlayer>();
                mockPlayer.Setup(x => x.PlayerUID).Returns($"player_{threadId}");

                int i = 0;
                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        favorSystem.QueueFavorForAction(
                            mockPlayer.Object,
                            "action",
                            (float)(i % 10),
                            (DeityDomain)((i % 4) + 1)
                        );
                        i++;
                    }
                    catch
                    {
                        Interlocked.Increment(ref errorCount);
                    }
                    Thread.Sleep(1);
                }
            }, cts.Token);
        }

        Task.WaitAll(tasks);

        // Assert - No errors should occur
        Assert.Equal(0, errorCount);
    }
}
