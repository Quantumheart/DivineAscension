namespace DivineAscension.Models.Enum;

public enum NotificationType
{
    None,
    FavorRankUp,
    PrestigeRankUp,
    BlessingSlotsIncreased,

    /// <summary>
    ///     A religion or civilization holiday was kept today. Unlike rank-up
    ///     toasts, clicking the holiday toast only dismisses it — it does not
    ///     open the main dialog (the chronicle / Letters page captures it).
    /// </summary>
    HolidayKept
}