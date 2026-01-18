using DivineAscension.Tests.Helpers;
using Xunit;

namespace DivineAscension.Tests.API;

public sealed class FakePersistenceServiceTests
{
    [Fact]
    public void Save_And_Load_StoresAndRetrievesData()
    {
        // Arrange
        var service = new FakePersistenceService();
        var testData = new TestData { Value = "Hello World", Number = 42 };

        // Act
        service.Save("test-key", testData);
        var loaded = service.Load<TestData>("test-key");

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal("Hello World", loaded.Value);
        Assert.Equal(42, loaded.Number);
    }

    [Fact]
    public void Load_NonExistentKey_ReturnsNull()
    {
        // Arrange
        var service = new FakePersistenceService();

        // Act
        var result = service.Load<TestData>("non-existent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void SaveRaw_And_LoadRaw_StoresAndRetrievesByteData()
    {
        // Arrange
        var service = new FakePersistenceService();
        var testBytes = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        service.SaveRaw("raw-key", testBytes);
        var loaded = service.LoadRaw("raw-key");

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal(testBytes, loaded);
    }

    [Fact]
    public void Exists_ExistingKey_ReturnsTrue()
    {
        // Arrange
        var service = new FakePersistenceService();
        service.Save("test-key", new TestData());

        // Act
        var exists = service.Exists("test-key");

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public void Exists_NonExistentKey_ReturnsFalse()
    {
        // Arrange
        var service = new FakePersistenceService();

        // Act
        var exists = service.Exists("non-existent");

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public void Delete_RemovesData()
    {
        // Arrange
        var service = new FakePersistenceService();
        service.Save("test-key", new TestData());

        // Act
        service.Delete("test-key");

        // Assert
        Assert.False(service.Exists("test-key"));
        Assert.Null(service.Load<TestData>("test-key"));
    }

    [Fact]
    public void Clear_RemovesAllData()
    {
        // Arrange
        var service = new FakePersistenceService();
        service.Save("key1", new TestData());
        service.Save("key2", new TestData());
        service.SaveRaw("key3", new byte[] { 1, 2, 3 });

        // Act
        service.Clear();

        // Assert
        Assert.Equal(0, service.Count);
        Assert.False(service.Exists("key1"));
        Assert.False(service.Exists("key2"));
        Assert.False(service.Exists("key3"));
    }

    [Fact]
    public void GetAllKeys_ReturnsAllStoredKeys()
    {
        // Arrange
        var service = new FakePersistenceService();
        service.Save("key1", new TestData());
        service.Save("key2", new TestData());
        service.SaveRaw("key3", new byte[] { 1, 2, 3 });

        // Act
        var keys = service.GetAllKeys().ToList();

        // Assert
        Assert.Equal(3, keys.Count);
        Assert.Contains("key1", keys);
        Assert.Contains("key2", keys);
        Assert.Contains("key3", keys);
    }

    [Fact]
    public void Save_OverwritesExistingData()
    {
        // Arrange
        var service = new FakePersistenceService();
        service.Save("test-key", new TestData { Value = "Original" });

        // Act
        service.Save("test-key", new TestData { Value = "Updated" });
        var loaded = service.Load<TestData>("test-key");

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal("Updated", loaded.Value);
    }

    private sealed class TestData
    {
        public string Value { get; set; } = string.Empty;
        public int Number { get; set; }
    }
}
