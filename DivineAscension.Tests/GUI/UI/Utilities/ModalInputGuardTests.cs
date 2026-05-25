using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DivineAscension.GUI.Events;
using DivineAscension.GUI.UI.Utilities;

namespace DivineAscension.Tests.GUI.UI.Utilities;

/// <summary>
///     Tests for <see cref="ModalInputGuard.FilterBackground{T}" /> — the gate that drops a
///     pane's click-through behind an open confirm modal while keeping the modal's own
///     confirm/cancel events live (#455). Each test drives the static guard explicitly via
///     <see cref="ModalInputGuard.MarkOpen" /> / <see cref="ModalInputGuard.BeginFrame" />.
/// </summary>
[ExcludeFromCodeCoverage]
public class ModalInputGuardTests
{
    private record Background : ITestEvent;

    private record ModalControl : ITestEvent, IModalControlEvent;

    private interface ITestEvent;

    /// <summary>Rolls the guard into the not-blocking state for a clean baseline.</summary>
    private static void SetNotBlocking() => ModalInputGuard.BeginFrame();

    /// <summary>Marks a modal open and rolls it into IsBlocking (mirrors a steady-state modal frame).</summary>
    private static void SetBlocking()
    {
        ModalInputGuard.MarkOpen();
        ModalInputGuard.BeginFrame();
    }

    [Fact]
    public void FilterBackground_WhenNotBlocking_ReturnsEventsUntouched()
    {
        SetNotBlocking();
        var events = new List<ITestEvent> { new Background(), new ModalControl(), new Background() };

        var result = ModalInputGuard.FilterBackground(events);

        Assert.Equal(3, result.Count);
        Assert.Same(events, result);
    }

    [Fact]
    public void FilterBackground_WhenBlocking_DropsBackgroundButKeepsModalControl()
    {
        SetBlocking();
        var events = new List<ITestEvent> { new Background(), new ModalControl(), new Background() };

        var result = ModalInputGuard.FilterBackground(events);

        Assert.Single(result);
        Assert.IsType<ModalControl>(result[0]);
    }

    [Fact]
    public void FilterBackground_WhenBlocking_DropsAllWhenNoModalControl()
    {
        SetBlocking();
        var events = new List<ITestEvent> { new Background(), new Background() };

        Assert.Empty(ModalInputGuard.FilterBackground(events));
    }

    [Fact]
    public void FilterBackground_NullOrEmpty_ReturnsEmptyNeverNull()
    {
        SetBlocking();
        Assert.Empty(ModalInputGuard.FilterBackground<ITestEvent>(null));

        SetNotBlocking();
        Assert.Empty(ModalInputGuard.FilterBackground(new List<ITestEvent>()));
    }

    [Fact]
    public void IsBlocking_LagsMarkByOneFrame()
    {
        // Opening frame: mark is set but IsBlocking still reflects the prior (clear) frame.
        SetNotBlocking();
        ModalInputGuard.MarkOpen();
        Assert.False(ModalInputGuard.IsBlocking);

        // Next frame promotes the mark.
        ModalInputGuard.BeginFrame();
        Assert.True(ModalInputGuard.IsBlocking);

        // Modal closed (no mark this frame) — gate clears on the following frame.
        ModalInputGuard.BeginFrame();
        Assert.False(ModalInputGuard.IsBlocking);
    }
}
