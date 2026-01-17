namespace DivineAscension.Utilities;

/// <summary>
///     Utility class for formatting cooldown times into user-friendly strings.
/// </summary>
public static class CooldownTimeFormatter
{
    /// <summary>
    ///     Formats a remaining time in seconds into a user-friendly string.
    ///     Examples: "5 minutes", "30 seconds", "1 minute 15 seconds"
    /// </summary>
    /// <param name="remainingSeconds">The remaining time in seconds</param>
    /// <returns>Formatted time string</returns>
    public static string FormatTimeRemaining(double remainingSeconds)
    {
        var totalSeconds = (int)System.Math.Ceiling(remainingSeconds);

        if (totalSeconds >= 60)
        {
            var minutes = totalSeconds / 60;
            var seconds = totalSeconds % 60;

            if (seconds == 0)
                return minutes == 1 ? "1 minute" : $"{minutes} minutes";

            var minuteText = minutes == 1 ? "1 minute" : $"{minutes} minutes";
            var secondText = seconds == 1 ? "1 second" : $"{seconds} seconds";
            return $"{minuteText} {secondText}";
        }

        return totalSeconds == 1 ? "1 second" : $"{totalSeconds} seconds";
    }
}
