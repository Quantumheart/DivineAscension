using DivineAscension.Tests.Helpers;
using Moq;
using Vintagestory.API.Server;
using Xunit;

namespace DivineAscension.Tests.API;

public sealed class FakeEventServiceTests
{
    [Fact]
    public void OnSaveGameLoaded_SubscribesAndTriggersCallback()
    {
        // Arrange
        var service = new FakeEventService();
        var callbackInvoked = false;

        // Act
        service.OnSaveGameLoaded(() => callbackInvoked = true);
        service.TriggerSaveGameLoaded();

        // Assert
        Assert.True(callbackInvoked);
    }

    [Fact]
    public void OnGameWorldSave_SubscribesAndTriggersCallback()
    {
        // Arrange
        var service = new FakeEventService();
        var callbackInvoked = false;

        // Act
        service.OnGameWorldSave(() => callbackInvoked = true);
        service.TriggerGameWorldSave();

        // Assert
        Assert.True(callbackInvoked);
    }

    [Fact]
    public void OnPlayerJoin_SubscribesAndTriggersCallbackWithPlayer()
    {
        // Arrange
        var service = new FakeEventService();
        var player = new Mock<IServerPlayer>();
        player.Setup(p => p.PlayerUID).Returns("test-uid");
        IServerPlayer? receivedPlayer = null;

        // Act
        service.OnPlayerJoin(p => receivedPlayer = p);
        service.TriggerPlayerJoin(player.Object);

        // Assert
        Assert.NotNull(receivedPlayer);
        Assert.Equal("test-uid", receivedPlayer.PlayerUID);
    }

    [Fact]
    public void RegisterCallback_ReturnsUniqueIds()
    {
        // Arrange
        var service = new FakeEventService();

        // Act
        var id1 = service.RegisterCallback(_ => { }, 1000);
        var id2 = service.RegisterCallback(_ => { }, 1000);
        var id3 = service.RegisterGameTickListener(_ => { }, 1000);

        // Assert
        Assert.NotEqual(id1, id2);
        Assert.NotEqual(id2, id3);
        Assert.NotEqual(id1, id3);
    }

    [Fact]
    public void TriggerCallback_InvokesSpecificCallback()
    {
        // Arrange
        var service = new FakeEventService();
        var callback1Invoked = false;
        var callback2Invoked = false;

        var id1 = service.RegisterCallback(_ => callback1Invoked = true, 1000);
        var id2 = service.RegisterCallback(_ => callback2Invoked = true, 1000);

        // Act
        service.TriggerCallback(id1, 0.1f);

        // Assert
        Assert.True(callback1Invoked);
        Assert.False(callback2Invoked);
    }

    [Fact]
    public void TriggerPeriodicCallbacks_InvokesAllCallbacks()
    {
        // Arrange
        var service = new FakeEventService();
        var callback1Count = 0;
        var callback2Count = 0;

        service.RegisterCallback(_ => callback1Count++, 1000);
        service.RegisterGameTickListener(_ => callback2Count++, 1000);

        // Act
        service.TriggerPeriodicCallbacks(0.1f);

        // Assert
        Assert.Equal(1, callback1Count);
        Assert.Equal(1, callback2Count);
    }

    [Fact]
    public void UnregisterCallback_RemovesCallback()
    {
        // Arrange
        var service = new FakeEventService();
        var callbackInvoked = false;

        var id = service.RegisterCallback(_ => callbackInvoked = true, 1000);

        // Act
        service.UnregisterCallback(id);
        service.TriggerCallback(id, 0.1f);

        // Assert
        Assert.False(callbackInvoked);
    }

    [Fact]
    public void UnsubscribeSaveGameLoaded_RemovesCallback()
    {
        // Arrange
        var service = new FakeEventService();
        var callbackInvoked = false;
        Action callback = () => callbackInvoked = true;

        service.OnSaveGameLoaded(callback);

        // Act
        service.UnsubscribeSaveGameLoaded(callback);
        service.TriggerSaveGameLoaded();

        // Assert
        Assert.False(callbackInvoked);
    }

    [Fact]
    public void MultipleSubscribers_AllReceiveEvents()
    {
        // Arrange
        var service = new FakeEventService();
        var callback1Invoked = false;
        var callback2Invoked = false;

        service.OnSaveGameLoaded(() => callback1Invoked = true);
        service.OnSaveGameLoaded(() => callback2Invoked = true);

        // Act
        service.TriggerSaveGameLoaded();

        // Assert
        Assert.True(callback1Invoked);
        Assert.True(callback2Invoked);
    }

    [Fact]
    public void TriggerCallback_PassesDeltaTime()
    {
        // Arrange
        var service = new FakeEventService();
        float receivedDeltaTime = 0f;

        var id = service.RegisterCallback(dt => receivedDeltaTime = dt, 1000);

        // Act
        service.TriggerCallback(id, 0.5f);

        // Assert
        Assert.Equal(0.5f, receivedDeltaTime);
    }
}
