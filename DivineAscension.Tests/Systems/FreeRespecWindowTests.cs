using System.Diagnostics.CodeAnalysis;
using DivineAscension.Systems;
using Xunit;

namespace DivineAscension.Tests.Systems;

/// <summary>
///     Unit tests for <see cref="FreeRespecWindow" /> (epic #425, slice 4 — #462): default-closed,
///     toggle/set semantics, and the <see cref="FreeRespecWindow.Changed" /> event firing only on
///     an actual state flip.
/// </summary>
[ExcludeFromCodeCoverage]
public class FreeRespecWindowTests
{
    [Fact]
    public void IsActive_DefaultsToFalse()
    {
        Assert.False(new FreeRespecWindow().IsActive);
    }

    [Fact]
    public void Toggle_FlipsState_AndReturnsNewValue()
    {
        var window = new FreeRespecWindow();

        Assert.True(window.Toggle());
        Assert.True(window.IsActive);
        Assert.False(window.Toggle());
        Assert.False(window.IsActive);
    }

    [Fact]
    public void SetActive_RaisesChanged_OnlyWhenValueFlips()
    {
        var window = new FreeRespecWindow();
        var fired = 0;
        window.Changed += () => fired++;

        window.SetActive(true);  // flip → fires
        window.SetActive(true);  // no-op → silent
        window.SetActive(false); // flip → fires

        Assert.Equal(2, fired);
    }

    [Fact]
    public void Toggle_RaisesChanged()
    {
        var window = new FreeRespecWindow();
        var fired = 0;
        window.Changed += () => fired++;

        window.Toggle();
        window.Toggle();

        Assert.Equal(2, fired);
    }
}
