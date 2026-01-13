using DivineAscension.Services;

namespace DivineAscension.Tests.Services;

[Collection("ClipboardServiceTests")]
public class ClipboardServiceTests
{
    public ClipboardServiceTests()
    {
        // Reset the service before each test
        ClipboardService.Instance.ResetForTesting();
    }

    #region Singleton Tests

    [Fact]
    public void Instance_AlwaysReturnsSameInstance()
    {
        // Act
        var instance1 = ClipboardService.Instance;
        var instance2 = ClipboardService.Instance;

        // Assert
        Assert.Same(instance1, instance2);
    }

    #endregion

    #region Basic Clipboard Operations

    [Fact]
    public void SetText_ThenGetText_ReturnsText()
    {
        // Arrange
        var service = ClipboardService.Instance;
        const string testText = "test";

        // Act
        service.SetText(testText);
        var result = service.GetText();

        // Assert
        Assert.Equal(testText, result);
    }

    [Fact]
    public void GetText_WhenEmpty_ReturnsEmptyString()
    {
        // Arrange
        var service = ClipboardService.Instance;

        // Act
        var result = service.GetText();

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void SetText_WithNull_TreatsAsEmptyString()
    {
        // Arrange
        var service = ClipboardService.Instance;

        // Act
        service.SetText(null!);
        var result = service.GetText();

        // Assert
        Assert.Equal(string.Empty, result);
    }

    #endregion

    #region Various String Types

    [Theory]
    [InlineData("")]
    [InlineData("Hello World")]
    [InlineData("Hello\nWorld\nMultiline")]
    [InlineData("Unicode: 你好 世界")]
    [InlineData("Emoji: 🎉 🎊 🎈")]
    [InlineData("Special chars: !@#$%^&*()")]
    [InlineData("   Leading and trailing spaces   ")]
    public void Clipboard_HandlesVariousStrings(string input)
    {
        // Arrange
        var service = ClipboardService.Instance;

        // Act
        service.SetText(input);
        var result = service.GetText();

        // Assert
        Assert.Equal(input, result);
    }

    [Fact]
    public void Clipboard_HandlesVeryLongText()
    {
        // Arrange
        var service = ClipboardService.Instance;
        var longText = new string('a', 1000);

        // Act
        service.SetText(longText);
        var result = service.GetText();

        // Assert
        Assert.Equal(longText, result);
        Assert.Equal(1000, result.Length);
    }

    #endregion

    #region Sequential Operations

    [Fact]
    public void SetText_MultipleTimes_OverwritesPreviousValue()
    {
        // Arrange
        var service = ClipboardService.Instance;

        // Act
        service.SetText("first");
        service.SetText("second");
        service.SetText("third");
        var result = service.GetText();

        // Assert
        Assert.Equal("third", result);
    }

    [Fact]
    public void GetText_MultipleTimes_ReturnsConsistentValue()
    {
        // Arrange
        var service = ClipboardService.Instance;
        service.SetText("test");

        // Act
        var result1 = service.GetText();
        var result2 = service.GetText();
        var result3 = service.GetText();

        // Assert
        Assert.Equal("test", result1);
        Assert.Equal("test", result2);
        Assert.Equal("test", result3);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void SetText_WithEmptyString_GetTextReturnsEmpty()
    {
        // Arrange
        var service = ClipboardService.Instance;

        // Act
        service.SetText(string.Empty);
        var result = service.GetText();

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void SetText_AfterReset_Works()
    {
        // Arrange
        var service = ClipboardService.Instance;
        service.SetText("initial");

        // Act
        service.ResetForTesting();
        service.SetText("after reset");
        var result = service.GetText();

        // Assert
        Assert.Equal("after reset", result);
    }

    #endregion

    #region System Clipboard Availability

    [Fact]
    public void IsSystemClipboardAvailable_AfterReset_ReturnsFalse()
    {
        // Arrange
        var service = ClipboardService.Instance;

        // Act & Assert
        // After reset, system clipboard should be unavailable (not initialized)
        Assert.False(service.IsSystemClipboardAvailable);
    }

    #endregion
}
