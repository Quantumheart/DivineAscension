using System.Collections.Generic;
using System.IO;
using DivineAscension.Data;
using ProtoBuf;
using Vintagestory.API.MathTools;
using Xunit;

namespace DivineAscension.Tests.Data;

public class HolySiteDataTests
{
    [Fact]
    public void SerializableBlockPos_ToBlockPos_ConvertsCorrectly()
    {
        // Arrange
        var serializable = new SerializableBlockPos(100, 50, 200);

        // Act
        var blockPos = serializable.ToBlockPos();

        // Assert
        Assert.Equal(100, blockPos.X);
        Assert.Equal(50, blockPos.Y);
        Assert.Equal(200, blockPos.Z);
    }

    [Fact]
    public void SerializableBlockPos_FromBlockPos_ConvertsCorrectly()
    {
        // Arrange
        var blockPos = new BlockPos(100, 50, 200);

        // Act
        var serializable = SerializableBlockPos.FromBlockPos(blockPos);

        // Assert
        Assert.Equal(100, serializable.X);
        Assert.Equal(50, serializable.Y);
        Assert.Equal(200, serializable.Z);
    }

    [Fact]
    public void SerializableBlockPos_Equals_MatchingPosition_ReturnsTrue()
    {
        // Arrange
        var serializable = new SerializableBlockPos(100, 50, 200);
        var blockPos = new BlockPos(100, 50, 200);

        // Act
        var result = serializable.Equals(blockPos);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void SerializableBlockPos_Equals_DifferentPosition_ReturnsFalse()
    {
        // Arrange
        var serializable = new SerializableBlockPos(100, 50, 200);
        var blockPos = new BlockPos(100, 51, 200); // Different Y

        // Act
        var result = serializable.Equals(blockPos);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void SerializableBlockPos_Serialization_RoundTrip()
    {
        // Arrange
        var original = new SerializableBlockPos(123, 45, 678);

        // Act
        byte[] serialized;
        using (var ms = new MemoryStream())
        {
            Serializer.Serialize(ms, original);
            serialized = ms.ToArray();
        }

        SerializableBlockPos deserialized;
        using (var ms = new MemoryStream(serialized))
        {
            deserialized = Serializer.Deserialize<SerializableBlockPos>(ms);
        }

        // Assert
        Assert.Equal(original.X, deserialized.X);
        Assert.Equal(original.Y, deserialized.Y);
        Assert.Equal(original.Z, deserialized.Z);
    }

    [Fact]
    public void HolySiteData_WithAltarPosition_Serializes()
    {
        // Arrange
        var altarPos = SerializableBlockPos.FromBlockPos(new BlockPos(100, 50, 200));
        var original = new HolySiteData(
            "site1",
            "rel1",
            "Test Site",
            new List<SerializableCuboidi> { new SerializableCuboidi(90, 40, 90, 110, 60, 110) },
            "player1",
            "TestPlayer")
        {
            AltarPosition = altarPos
        };

        // Act
        byte[] serialized;
        using (var ms = new MemoryStream())
        {
            Serializer.Serialize(ms, original);
            serialized = ms.ToArray();
        }

        HolySiteData deserialized;
        using (var ms = new MemoryStream(serialized))
        {
            deserialized = Serializer.Deserialize<HolySiteData>(ms);
        }

        // Assert
        Assert.NotNull(deserialized.AltarPosition);
        Assert.Equal(100, deserialized.AltarPosition.X);
        Assert.Equal(50, deserialized.AltarPosition.Y);
        Assert.Equal(200, deserialized.AltarPosition.Z);
    }

    [Fact]
    public void HolySiteData_WithoutAltarPosition_SerializesAsNull()
    {
        // Arrange
        var original = new HolySiteData(
            "site1",
            "rel1",
            "Test Site",
            new List<SerializableCuboidi> { new SerializableCuboidi(90, 40, 90, 110, 60, 110) },
            "player1",
            "TestPlayer");
        // No AltarPosition set

        // Act
        byte[] serialized;
        using (var ms = new MemoryStream())
        {
            Serializer.Serialize(ms, original);
            serialized = ms.ToArray();
        }

        HolySiteData deserialized;
        using (var ms = new MemoryStream(serialized))
        {
            deserialized = Serializer.Deserialize<HolySiteData>(ms);
        }

        // Assert
        Assert.Null(deserialized.AltarPosition);
    }

    [Fact]
    public void IsAltarSite_WithAltarPosition_ReturnsTrue()
    {
        // Arrange
        var site = new HolySiteData(
            "site1",
            "rel1",
            "Test Site",
            new List<SerializableCuboidi> { new SerializableCuboidi(90, 40, 90, 110, 60, 110) },
            "player1",
            "TestPlayer")
        {
            AltarPosition = SerializableBlockPos.FromBlockPos(new BlockPos(100, 50, 200))
        };

        // Act
        var result = site.IsAltarSite();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsAltarSite_WithoutAltarPosition_ReturnsFalse()
    {
        // Arrange
        var site = new HolySiteData(
            "site1",
            "rel1",
            "Test Site",
            new List<SerializableCuboidi> { new SerializableCuboidi(90, 40, 90, 110, 60, 110) },
            "player1",
            "TestPlayer");

        // Act
        var result = site.IsAltarSite();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsAtAltarPosition_MatchingPosition_ReturnsTrue()
    {
        // Arrange
        var altarPos = new BlockPos(100, 50, 200);
        var site = new HolySiteData(
            "site1",
            "rel1",
            "Test Site",
            new List<SerializableCuboidi> { new SerializableCuboidi(90, 40, 90, 110, 60, 110) },
            "player1",
            "TestPlayer")
        {
            AltarPosition = SerializableBlockPos.FromBlockPos(altarPos)
        };

        // Act
        var result = site.IsAtAltarPosition(altarPos);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsAtAltarPosition_DifferentPosition_ReturnsFalse()
    {
        // Arrange
        var altarPos = new BlockPos(100, 50, 200);
        var differentPos = new BlockPos(101, 50, 200);
        var site = new HolySiteData(
            "site1",
            "rel1",
            "Test Site",
            new List<SerializableCuboidi> { new SerializableCuboidi(90, 40, 90, 110, 60, 110) },
            "player1",
            "TestPlayer")
        {
            AltarPosition = SerializableBlockPos.FromBlockPos(altarPos)
        };

        // Act
        var result = site.IsAtAltarPosition(differentPos);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsAtAltarPosition_NoAltarPosition_ReturnsFalse()
    {
        // Arrange
        var site = new HolySiteData(
            "site1",
            "rel1",
            "Test Site",
            new List<SerializableCuboidi> { new SerializableCuboidi(90, 40, 90, 110, 60, 110) },
            "player1",
            "TestPlayer");

        // Act
        var result = site.IsAtAltarPosition(new BlockPos(100, 50, 200));

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HolySiteWorldData_WithAltarSites_Serializes()
    {
        // Arrange
        var original = new HolySiteWorldData
        {
            HolySites = new List<HolySiteData>
            {
                new HolySiteData("site1", "rel1", "Site 1",
                    new List<SerializableCuboidi> { new SerializableCuboidi(0, 0, 0, 10, 10, 10) },
                    "p1", "P1")
                {
                    AltarPosition = SerializableBlockPos.FromBlockPos(new BlockPos(5, 5, 5))
                },
                new HolySiteData("site2", "rel1", "Site 2",
                    new List<SerializableCuboidi> { new SerializableCuboidi(20, 20, 20, 30, 30, 30) },
                    "p1", "P1")
                // No altar position (legacy site)
            }
        };

        // Act
        byte[] serialized;
        using (var ms = new MemoryStream())
        {
            Serializer.Serialize(ms, original);
            serialized = ms.ToArray();
        }

        HolySiteWorldData deserialized;
        using (var ms = new MemoryStream(serialized))
        {
            deserialized = Serializer.Deserialize<HolySiteWorldData>(ms);
        }

        // Assert
        Assert.Equal(2, deserialized.HolySites.Count);
        Assert.NotNull(deserialized.HolySites[0].AltarPosition);
        Assert.Null(deserialized.HolySites[1].AltarPosition);
    }
}
