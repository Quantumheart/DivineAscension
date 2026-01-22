using DivineAscension.Blocks;
using DivineAscension.Systems.Altar;
using Moq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace DivineAscension.Tests.Blocks;

public class BlockBehaviorAltarTests
{
    [Fact]
    public void OnBlockInteractStart_ServerSide_RaisesAltarUsedEvent()
    {
        // Arrange
        var mockBlock = new Mock<Block>();
        var mockWorld = new Mock<IWorldAccessor>();
        mockWorld.Setup(x => x.Side).Returns(EnumAppSide.Server);
        var mockPlayer = new Mock<IServerPlayer>();
        var blockSel = new BlockSelection();
        var emitterSpy = new AltarEventEmitterSpy();

        BlockBehaviorAltar.SetEventEmitter(emitterSpy);
        var behavior = new BlockBehaviorAltar(mockBlock.Object);
        var handling = EnumHandling.PreventDefault;

        // Act
        var result = behavior.OnBlockInteractStart(
            mockWorld.Object, mockPlayer.Object, blockSel, ref handling);

        // Assert
        Assert.True(result); // Returns true to indicate we handled the interaction
        Assert.Equal(EnumHandling.PreventSubsequent, handling);
        Assert.Single(emitterSpy.AltarUsedEvents);
        Assert.Equal(mockPlayer.Object, emitterSpy.AltarUsedEvents[0].Item1);
        Assert.Equal(blockSel, emitterSpy.AltarUsedEvents[0].Item2);
    }

    [Fact]
    public void OnBlockInteractStart_ClientSide_DoesNotRaiseEvent()
    {
        // Arrange
        var mockBlock = new Mock<Block>();
        var mockWorld = new Mock<IWorldAccessor>();
        mockWorld.Setup(x => x.Side).Returns(EnumAppSide.Client);
        var mockPlayer = new Mock<IPlayer>();
        var blockSel = new BlockSelection();
        var emitterSpy = new AltarEventEmitterSpy();

        BlockBehaviorAltar.SetEventEmitter(emitterSpy);
        var behavior = new BlockBehaviorAltar(mockBlock.Object);
        var handled = EnumHandling.PassThrough;

        // Act
        var result = behavior.OnBlockInteractStart(
            mockWorld.Object, mockPlayer.Object, blockSel, ref handled);

        // Assert
        Assert.True(result); // Still returns true even on client side
        Assert.Equal(EnumHandling.PreventSubsequent, handled);
        Assert.Empty(emitterSpy.AltarUsedEvents); // But event is not raised on client
    }

    [Fact]
    public void OnBlockBroken_ServerSide_RaisesAltarBrokenEvent()
    {
        // Arrange
        var mockBlock = new Mock<Block>();
        var mockWorld = new Mock<IWorldAccessor>();
        mockWorld.Setup(x => x.Side).Returns(EnumAppSide.Server);
        var mockPlayer = new Mock<IServerPlayer>();
        var pos = new BlockPos(10, 20, 30);
        var emitterSpy = new AltarEventEmitterSpy();

        BlockBehaviorAltar.SetEventEmitter(emitterSpy);
        var behavior = new BlockBehaviorAltar(mockBlock.Object);
        var handling = EnumHandling.PassThrough;

        // Act
        behavior.OnBlockBroken(mockWorld.Object, pos, mockPlayer.Object, ref handling);

        // Assert
        Assert.Single(emitterSpy.AltarBrokenEvents);
        Assert.Equal(mockPlayer.Object, emitterSpy.AltarBrokenEvents[0].Item1);
        Assert.Equal(pos, emitterSpy.AltarBrokenEvents[0].Item2);
    }

    [Fact]
    public void OnBlockBroken_ClientSide_DoesNotRaiseEvent()
    {
        // Arrange
        var mockBlock = new Mock<Block>();
        var mockWorld = new Mock<IWorldAccessor>();
        mockWorld.Setup(x => x.Side).Returns(EnumAppSide.Client);
        var mockPlayer = new Mock<IPlayer>();
        var pos = new BlockPos(10, 20, 30);
        var emitterSpy = new AltarEventEmitterSpy();

        BlockBehaviorAltar.SetEventEmitter(emitterSpy);
        var behavior = new BlockBehaviorAltar(mockBlock.Object);
        var handling = EnumHandling.PassThrough;

        // Act
        behavior.OnBlockBroken(mockWorld.Object, pos, mockPlayer.Object, ref handling);

        // Assert
        Assert.Empty(emitterSpy.AltarBrokenEvents);
    }

    [Fact]
    public void OnBlockInteractStart_AlwaysSetsHandlingToPreventSubsequent()
    {
        // Arrange
        var mockBlock = new Mock<Block>();
        var mockWorld = new Mock<IWorldAccessor>();
        mockWorld.Setup(x => x.Side).Returns(EnumAppSide.Server);
        var mockPlayer = new Mock<IServerPlayer>();
        var blockSel = new BlockSelection();
        var emitter = new AltarEventEmitter();

        BlockBehaviorAltar.SetEventEmitter(emitter);
        var behavior = new BlockBehaviorAltar(mockBlock.Object);
        var handling = EnumHandling.PreventDefault; // Start with different value

        // Act
        var result = behavior.OnBlockInteractStart(mockWorld.Object, mockPlayer.Object, blockSel, ref handling);

        // Assert
        Assert.True(result); // Returns true to indicate we handled the interaction
        Assert.Equal(EnumHandling.PreventSubsequent, handling);
    }

    [Fact]
    public void DoPlaceBlock_ServerSide_RaisesAltarPlacedEvent()
    {
        // Arrange
        var mockBlock = new Mock<Block>();
        var mockWorld = new Mock<IWorldAccessor>();
        mockWorld.Setup(x => x.Side).Returns(EnumAppSide.Server);
        var mockPlayer = new Mock<IServerPlayer>();
        var blockSel = new BlockSelection { Position = new BlockPos(10, 20, 30) };
        var itemStack = new ItemStack();
        var emitterSpy = new AltarEventEmitterSpy();

        BlockBehaviorAltar.SetEventEmitter(emitterSpy);
        var behavior = new BlockBehaviorAltar(mockBlock.Object);
        var handling = EnumHandling.PassThrough;

        // Act
        behavior.DoPlaceBlock(mockWorld.Object, mockPlayer.Object, blockSel, itemStack, ref handling);

        // Assert
        Assert.Single(emitterSpy.AltarPlacedEvents);
        Assert.Equal(mockPlayer.Object, emitterSpy.AltarPlacedEvents[0].Item1);
        Assert.Equal(blockSel, emitterSpy.AltarPlacedEvents[0].Item3);
        Assert.Equal(itemStack, emitterSpy.AltarPlacedEvents[0].Item4);
    }

    [Fact]
    public void DoPlaceBlock_ClientSide_DoesNotRaiseEvent()
    {
        // Arrange
        var mockBlock = new Mock<Block>();
        var mockWorld = new Mock<IWorldAccessor>();
        mockWorld.Setup(x => x.Side).Returns(EnumAppSide.Client);
        var mockPlayer = new Mock<IPlayer>();
        var blockSel = new BlockSelection();
        var itemStack = new ItemStack();
        var emitterSpy = new AltarEventEmitterSpy();

        BlockBehaviorAltar.SetEventEmitter(emitterSpy);
        var behavior = new BlockBehaviorAltar(mockBlock.Object);
        var handling = EnumHandling.PassThrough;

        // Act
        behavior.DoPlaceBlock(mockWorld.Object, mockPlayer.Object, blockSel, itemStack, ref handling);

        // Assert
        Assert.Empty(emitterSpy.AltarPlacedEvents);
    }
}

/// <summary>
/// Spy for AltarEventEmitter that records all events for test verification.
/// </summary>
internal class AltarEventEmitterSpy : AltarEventEmitter
{
    public List<(IPlayer, BlockSelection)> AltarUsedEvents { get; } = new();
    public List<(IServerPlayer, BlockPos)> AltarBrokenEvents { get; } = new();
    public List<(IServerPlayer, int, BlockSelection, ItemStack)> AltarPlacedEvents { get; } = new();

    public override void RaiseAltarUsed(IPlayer player, BlockSelection sel)
    {
        AltarUsedEvents.Add((player, sel));
        base.RaiseAltarUsed(player, sel);
    }

    public override void RaiseAltarBroken(IServerPlayer player, BlockPos pos)
    {
        AltarBrokenEvents.Add((player, pos));
        base.RaiseAltarBroken(player, pos);
    }

    public override void RaiseAltarPlaced(IServerPlayer player, int oldBlockId, BlockSelection blockSelection,
        ItemStack withItemStack)
    {
        AltarPlacedEvents.Add((player, oldBlockId, blockSelection, withItemStack));
        base.RaiseAltarPlaced(player, oldBlockId, blockSelection, withItemStack);
    }
}