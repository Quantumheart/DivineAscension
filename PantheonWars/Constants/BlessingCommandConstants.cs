using System.Diagnostics.CodeAnalysis;

namespace PantheonWars.Constants;

// Command and subcommand names
[ExcludeFromCodeCoverage]
public static class BlessingCommandConstants
{
    public const string CommandName = "blessings";
    public const string SubCommandList = "list";
    public const string SubCommandPlayer = "player";
    public const string SubCommandReligion = "religion";
    public const string SubCommandInfo = "info";
    public const string SubCommandTree = "tree";
    public const string SubCommandUnlock = "unlock";
    public const string SubCommandActive = "active";
}

// Parameter names
[ExcludeFromCodeCoverage]
public static class ParameterConstants
{
    public const string ParamBlessingId = "blessingid";
    public const string ParamType = "type";
}

// Command descriptions
[ExcludeFromCodeCoverage]
public static class BlessingDescriptionConstants
{
    public const string CommandDescription = "Manage blessings and view blessing information";
    public const string DescriptionList = "List all available blessings for your deity";
    public const string DescriptionPlayer = "Show your unlocked player blessings";
    public const string DescriptionReligion = "Show your religion's unlocked blessings";
    public const string DescriptionInfo = "Show detailed information about a blessing";
    public const string DescriptionTree = "Display blessing tree (player or religion)";
    public const string DescriptionUnlock = "Unlock a blessing";
    public const string DescriptionActive = "Show all active blessings and stat modifiers";
}

// Common messages
[ExcludeFromCodeCoverage]
public static class ErrorMessageConstants
{
    public const string ErrorPlayerNotFound = "Player not found";
    public const string ErrorNoReligion = "You are not in a religion.";

    public const string ErrorMustJoinReligion =
        "You must join a religion to view blessings. Use /religion to get started.";

    public const string ErrorMustJoinReligionForTree = "You must join a religion to view blessing trees.";
    public const string ErrorMustBeInReligionToUnlock = "You must be in a religion to unlock player blessings.";
    public const string ErrorOnlyFounderCanUnlock = "Only the religion founder can unlock religion blessings.";
    public const string ErrorBlessingNotFound = "Blessing not found: {0}";
    public const string ErrorCannotUnlockBlessing = "Cannot unlock blessing: {0}";
    public const string ErrorFailedToUnlock = "Failed to unlock blessing (may already be unlocked)";
}

// Usage messages
[ExcludeFromCodeCoverage]
public static class UsageMessageConstants
{
    public const string UsageBlessingsInfo = "Usage: /blessings info <blessingid>";
    public const string UsageBlessingsUnlock = "Usage: /blessings unlock <blessingid>";
}

// Success messages
[ExcludeFromCodeCoverage]
public static class SuccessMessageConstants
{
    public const string SuccessUnlockedPlayerBlessing = "Unlocked player blessing: {0}!";
    public const string SuccessUnlockedReligionBlessing = "Unlocked religion blessing: {0} for all members!";
    public const string NotificationBlessingUnlocked = "Your religion unlocked: {0}!";
}

// Info messages
[ExcludeFromCodeCoverage]
public static class InfoMessageConstants
{
    public const string InfoNoPlayerBlessings = "You have no unlocked player blessings yet.";
    public const string InfoNoReligionBlessings = "Your religion has no unlocked blessings yet.";
}

// Log messages
public static class LogMessageConstants
{
    public const string LogBlessingCommandsRegistered = "[PantheonWars] Blessing commands registered";
}

// Format strings
public static class FormatStringConstants
{
    public const string HeaderBlessingsForDeity = "=== Blessings for {0} ===";
    public const string HeaderPlayerBlessings = "--- Player Blessings ---";
    public const string HeaderReligionBlessings = "--- Religion Blessings ---";
    public const string HeaderUnlockedPlayerBlessings = "=== Your Unlocked Player Blessings ({0}) ===";
    public const string HeaderReligionBlessingsWithName = "=== {0} Blessings ({1}) ===";
    public const string HeaderBlessingInfo = "=== {0} ===";
    public const string HeaderBlessingTree = "=== {0} {1} Blessing Tree ===";
    public const string HeaderActiveBlessings = "=== Active Blessings & Stat Modifiers ===";
    public const string HeaderRankSection = "--- {0} Rank ---";
    public const string LabelUnlocked = "[UNLOCKED]";
    public const string LabelChecked = "[✓]";
    public const string LabelUnchecked = "[ ]";
    public const string FormatBlessingId = "  ID: {0}";
    public const string FormatRequiredRank = "  Required Rank: {0}";
    public const string FormatDescription = "  {0}";
    public const string FormatBlessingNameCategory = "{0} ({1})";
    public const string FormatStatModifier = "    {0}: +{1:F1}%";
    public const string FormatStatModifierPercent = "  {0}: +{1:F1}%";
    public const string LabelId = "ID: {0}";
    public const string LabelDeity = "Deity: {0}";
    public const string LabelType = "Type: {0}";
    public const string LabelCategory = "Category: {0}";
    public const string LabelDescriptionStandalone = "Description: {0}";
    public const string LabelRequiredFavorRank = "Required Favor Rank: {0}";
    public const string LabelRequiredPrestigeRank = "Required Prestige Rank: {0}";
    public const string LabelPrerequisites = "Prerequisites:";
    public const string LabelStatModifiers = "Stat Modifiers:";
    public const string LabelSpecialEffects = "Special Effects:";
    public const string LabelEffects = "  Effects:";
    public const string LabelEffectsForAllMembers = "  Effects (for all members):";
    public const string LabelRequires = "    Requires: ";
    public const string LabelPrerequisiteItem = "  - {0}";
    public const string LabelSpecialEffectItem = "  - {0}";
    public const string LabelPlayerBlessingsSection = "Player Blessings ({0}):";
    public const string LabelReligionBlessingsSection = "Religion Blessings ({0}):";
    public const string LabelCombinedStatModifiers = "Combined Stat Modifiers:";
    public const string LabelNoActiveModifiers = "  No active modifiers";
    public const string LabelNone = "  None";
    public const string LabelBlessingItem = "  - {0}";
    public const string TypePlayer = "player";
    public const string TypeReligion = "religion";
}