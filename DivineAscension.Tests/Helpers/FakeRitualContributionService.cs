using DivineAscension.Data;
using DivineAscension.Services.Interfaces;
using Vintagestory.API.Common;

namespace DivineAscension.Tests.Helpers;

/// <summary>
/// Fake implementation of IRitualContributionService for testing.
/// Provides configurable results and records all calls for verification.
/// </summary>
public class FakeRitualContributionService : IRitualContributionService
{
    /// <summary>
    /// Default result to return when no specific configuration is set.
    /// </summary>
    public RitualAttemptResult DefaultResult { get; set; } = new(
        Success: false,
        RitualStarted: false,
        RitualCompleted: false,
        Message: string.Empty);

    /// <summary>
    /// When set, this result will be returned for the next call only, then cleared.
    /// </summary>
    public RitualAttemptResult? NextResult { get; set; }

    /// <summary>
    /// List of all calls made to TryContributeToRitual for verification.
    /// </summary>
    public List<RitualContributionCall> Calls { get; } = new();

    /// <inheritdoc />
    public RitualAttemptResult TryContributeToRitual(
        HolySiteData holySite,
        ItemStack offering,
        ReligionData religion,
        string playerUID,
        string playerName)
    {
        Calls.Add(new RitualContributionCall(holySite, offering, religion, playerUID, playerName));

        if (NextResult != null)
        {
            var result = NextResult;
            NextResult = null;
            return result;
        }

        return DefaultResult;
    }

    /// <summary>
    /// Clears all recorded calls and resets configuration.
    /// </summary>
    public void Reset()
    {
        Calls.Clear();
        NextResult = null;
        DefaultResult = new RitualAttemptResult(
            Success: false,
            RitualStarted: false,
            RitualCompleted: false,
            Message: string.Empty);
    }

    /// <summary>
    /// Record of a single call to TryContributeToRitual.
    /// </summary>
    public record RitualContributionCall(
        HolySiteData HolySite,
        ItemStack Offering,
        ReligionData Religion,
        string PlayerUID,
        string PlayerName);
}
