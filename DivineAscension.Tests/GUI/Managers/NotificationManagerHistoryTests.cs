using System.Diagnostics.CodeAnalysis;
using DivineAscension.GUI.Interfaces;
using DivineAscension.GUI.Managers;
using DivineAscension.GUI.State;
using DivineAscension.Models.Enum;
using Moq;

namespace DivineAscension.Tests.GUI.Managers;

[ExcludeFromCodeCoverage]
public class NotificationManagerHistoryTests
{
    private readonly NotificationManager _sut;

    public NotificationManagerHistoryTests()
    {
        var mockSound = new Mock<ISoundManager>();
        _sut = new NotificationManager(mockSound.Object);
        // Disable the show-callback path so QueueRankUp does not eagerly recurse into sound.
        _sut.SetShowImGuiCallback(() => { });
    }

    [Fact]
    public void QueueRankUpNotification_PushesEntryIntoHistory()
    {
        _sut.QueueRankUpNotification(NotificationType.FavorRankUp, "Disciple", "desc", DeityDomain.Craft);

        Assert.Single(_sut.State.History);
        var entry = _sut.State.History[0];
        Assert.Equal(NotificationType.FavorRankUp, entry.Type);
        Assert.Equal("Disciple", entry.Title);
        Assert.Equal("desc", entry.Body);
        Assert.Equal(DeityDomain.Craft, entry.Deity);
        Assert.False(entry.Read);
    }

    [Fact]
    public void QueueRankUpNotification_BeyondCap_KeepsNewestFifty()
    {
        for (var i = 0; i < NotificationState.HistoryCap + 10; i++)
        {
            _sut.QueueRankUpNotification(NotificationType.FavorRankUp, $"R{i}", "d", DeityDomain.Craft);
        }

        Assert.Equal(NotificationState.HistoryCap, _sut.State.History.Count);
        Assert.Equal("R10", _sut.State.History[0].Title);
        Assert.Equal($"R{NotificationState.HistoryCap + 9}", _sut.State.History[^1].Title);
    }

    [Fact]
    public void MarkRead_SetsReadTrue()
    {
        _sut.QueueRankUpNotification(NotificationType.FavorRankUp, "Disciple", "d", DeityDomain.Craft);

        _sut.MarkRead(0);

        Assert.True(_sut.State.History[0].Read);
    }

    [Fact]
    public void MarkRead_OutOfRange_IsNoOp()
    {
        _sut.QueueRankUpNotification(NotificationType.FavorRankUp, "Disciple", "d", DeityDomain.Craft);

        _sut.MarkRead(-1);
        _sut.MarkRead(99);

        Assert.False(_sut.State.History[0].Read);
    }

    [Fact]
    public void ClearHistory_EmptiesHistory_LeavesPendingQueueAlone()
    {
        _sut.QueueRankUpNotification(NotificationType.FavorRankUp, "A", "d", DeityDomain.Craft);
        _sut.QueueRankUpNotification(NotificationType.FavorRankUp, "B", "d", DeityDomain.Craft);

        _sut.ClearHistory();

        Assert.Empty(_sut.State.History);
        // PendingNotifications is independent of history; the first toast becomes
        // visible immediately (ShowNextNotification dequeues it), the second stays queued.
        Assert.Single(_sut.State.PendingNotifications);
    }
}
