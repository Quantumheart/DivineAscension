using DivineAscension.Tests.Helpers;
using Moq;
using Vintagestory.API.Server;
using Xunit;

namespace DivineAscension.Tests.API;

public sealed class SpyNetworkServiceTests
{
    [Fact]
    public void SendToPlayer_RecordsMessage()
    {
        // Arrange
        var service = new SpyNetworkService();
        var player = new Mock<IServerPlayer>();
        player.Setup(p => p.PlayerUID).Returns("test-uid");
        var message = new TestMessage { Content = "Hello" };

        // Act
        service.SendToPlayer(player.Object, message);

        // Assert
        Assert.Equal(1, service.MessageCount);
        var sentMessage = service.GetLastSentMessage<TestMessage>();
        Assert.NotNull(sentMessage);
        Assert.Equal("Hello", sentMessage.Content);
    }

    [Fact]
    public void SendToAllPlayers_RecordsMessageWithCorrectType()
    {
        // Arrange
        var service = new SpyNetworkService();
        var message = new TestMessage { Content = "Broadcast" };

        // Act
        service.SendToAllPlayers(message);

        // Assert
        var messages = service.GetSentMessages();
        Assert.Single(messages);
        Assert.Equal(SpyNetworkService.SendType.ToAll, messages[0].Type);
    }

    [Fact]
    public void RegisterMessageHandler_AllowsSimulateReceive()
    {
        // Arrange
        var service = new SpyNetworkService();
        var player = new Mock<IServerPlayer>();
        TestMessage? receivedMessage = null;

        service.RegisterMessageHandler<TestMessage>((p, msg) => receivedMessage = msg);

        // Act
        service.SimulateReceive(player.Object, new TestMessage { Content = "Test" });

        // Assert
        Assert.NotNull(receivedMessage);
        Assert.Equal("Test", receivedMessage.Content);
    }

    [Fact]
    public void GetSentMessages_FiltersMessagesByType()
    {
        // Arrange
        var service = new SpyNetworkService();
        var player = new Mock<IServerPlayer>();

        service.SendToPlayer(player.Object, new TestMessage { Content = "First" });
        service.SendToPlayer(player.Object, new OtherMessage { Value = 42 });
        service.SendToPlayer(player.Object, new TestMessage { Content = "Second" });

        // Act
        var testMessages = service.GetSentMessages<TestMessage>().ToList();

        // Assert
        Assert.Equal(2, testMessages.Count);
        Assert.Equal("First", testMessages[0].Content);
        Assert.Equal("Second", testMessages[1].Content);
    }

    [Fact]
    public void Clear_RemovesAllMessagesAndHandlers()
    {
        // Arrange
        var service = new SpyNetworkService();
        var player = new Mock<IServerPlayer>();

        service.RegisterMessageHandler<TestMessage>((p, msg) => { });
        service.SendToPlayer(player.Object, new TestMessage());

        // Act
        service.Clear();

        // Assert
        Assert.Equal(0, service.MessageCount);
        Assert.Empty(service.GetSentMessages());
    }

    [Fact]
    public void SendToOthers_RecordsExcludedPlayer()
    {
        // Arrange
        var service = new SpyNetworkService();
        var excludedPlayer = new Mock<IServerPlayer>();
        excludedPlayer.Setup(p => p.PlayerUID).Returns("excluded-uid");
        var message = new TestMessage { Content = "To Others" };

        // Act
        service.SendToOthers(excludedPlayer.Object, message);

        // Assert
        var sentMessage = service.GetSentMessages().Single();
        Assert.Equal(SpyNetworkService.SendType.ToOthers, sentMessage.Type);
        Assert.Equal("excluded-uid", sentMessage.Player?.PlayerUID);
    }

    [Fact]
    public void Broadcast_RecordsMessageWithBroadcastType()
    {
        // Arrange
        var service = new SpyNetworkService();
        var message = new TestMessage { Content = "Broadcast" };

        // Act
        service.Broadcast(message);

        // Assert
        var sentMessage = service.GetSentMessages().Single();
        Assert.Equal(SpyNetworkService.SendType.Broadcast, sentMessage.Type);
    }

    private sealed class TestMessage
    {
        public string Content { get; set; } = string.Empty;
    }

    private sealed class OtherMessage
    {
        public int Value { get; set; }
    }
}
