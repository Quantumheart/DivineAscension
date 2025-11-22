using System.Diagnostics.CodeAnalysis;
using PantheonWars.GUI.State;
using Xunit;

namespace PantheonWars.Tests.GUI.State;

/// <summary>
///     Unit tests for GuildDialogState
/// </summary>
[ExcludeFromCodeCoverage]
public class GuildDialogStateTests
{
    #region Constructor and Default Values Tests

    [Fact]
    public void Constructor_InitializesWithDefaultValues()
    {
        // Act
        var state = new GuildDialogState();

        // Assert
        Assert.False(state.IsOpen);
        Assert.False(state.IsReady);
        Assert.Equal(0f, state.WindowPosX);
        Assert.Equal(0f, state.WindowPosY);
    }

    #endregion

    #region Property Tests

    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        // Arrange
        var state = new GuildDialogState();

        // Act
        state.IsOpen = true;
        state.IsReady = true;
        state.WindowPosX = 100.5f;
        state.WindowPosY = 200.75f;

        // Assert
        Assert.True(state.IsOpen);
        Assert.True(state.IsReady);
        Assert.Equal(100.5f, state.WindowPosX);
        Assert.Equal(200.75f, state.WindowPosY);
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Reset_ResetsAllPropertiesToDefaults()
    {
        // Arrange
        var state = new GuildDialogState
        {
            IsOpen = true,
            IsReady = true,
            WindowPosX = 100f,
            WindowPosY = 200f
        };

        // Act
        state.Reset();

        // Assert
        Assert.False(state.IsOpen);
        Assert.False(state.IsReady);
        Assert.Equal(0f, state.WindowPosX);
        Assert.Equal(0f, state.WindowPosY);
    }

    [Fact]
    public void Reset_CanBeCalledMultipleTimes()
    {
        // Arrange
        var state = new GuildDialogState();
        state.IsOpen = true;
        state.WindowPosX = 50f;

        // Act
        state.Reset();
        state.Reset();

        // Assert
        Assert.False(state.IsOpen);
        Assert.Equal(0f, state.WindowPosX);
    }

    #endregion
}
