using System.Collections.Generic;
using System.Linq;
using DivineAscension.Constants;
using DivineAscension.Models;
using DivineAscension.Services;

namespace DivineAscension.GUI.UI.Utilities;

/// <summary>
/// Maps role permissions to manuscript-flavoured verb phrases for the
/// Vestments chapter row summary (#318). Parallel to
/// <see cref="RolePermissions.GetDisplayName"/> — display names are the
/// noun-phrased "Invite Players" label, these are the predicate fragment
/// "invite" that fits inside "May invite, strike, and inscribe."
/// </summary>
internal static class RolePermissionPhrases
{
    private const int MaxPhrases = 4;

    private static readonly string[] OrderedPermissions =
    {
        RolePermissions.INVITE_PLAYERS,
        RolePermissions.MANAGE_INVITATIONS,
        RolePermissions.KICK_MEMBERS,
        RolePermissions.BAN_PLAYERS,
        RolePermissions.EDIT_DESCRIPTION,
        RolePermissions.MANAGE_ROLES,
        RolePermissions.CHANGE_PRIVACY,
        RolePermissions.VIEW_MEMBERS,
        RolePermissions.VIEW_BAN_LIST,
        RolePermissions.TRANSFER_FOUNDER,
        RolePermissions.DISBAND_RELIGION
    };

    private static readonly Dictionary<string, string> PhraseKeys = new()
    {
        [RolePermissions.INVITE_PLAYERS] = LocalizationKeys.UI_RELIGION_ROLES_PHRASE_INVITE,
        [RolePermissions.MANAGE_INVITATIONS] = LocalizationKeys.UI_RELIGION_ROLES_PHRASE_MANAGE_INVITES,
        [RolePermissions.KICK_MEMBERS] = LocalizationKeys.UI_RELIGION_ROLES_PHRASE_STRIKE,
        [RolePermissions.BAN_PLAYERS] = LocalizationKeys.UI_RELIGION_ROLES_PHRASE_CAST_OUT,
        [RolePermissions.EDIT_DESCRIPTION] = LocalizationKeys.UI_RELIGION_ROLES_PHRASE_INSCRIBE,
        [RolePermissions.MANAGE_ROLES] = LocalizationKeys.UI_RELIGION_ROLES_PHRASE_TAILOR,
        [RolePermissions.CHANGE_PRIVACY] = LocalizationKeys.UI_RELIGION_ROLES_PHRASE_VEIL,
        [RolePermissions.VIEW_MEMBERS] = LocalizationKeys.UI_RELIGION_ROLES_PHRASE_READ_ROLL,
        [RolePermissions.VIEW_BAN_LIST] = LocalizationKeys.UI_RELIGION_ROLES_PHRASE_READ_STRIKES,
        [RolePermissions.TRANSFER_FOUNDER] = LocalizationKeys.UI_RELIGION_ROLES_PHRASE_NAME_HEIR,
        [RolePermissions.DISBAND_RELIGION] = LocalizationKeys.UI_RELIGION_ROLES_PHRASE_DISSOLVE
    };

    /// <summary>
    ///     Build the auto-phrased permission summary line for a role row.
    ///     Founder role is special-cased to a fixed rubric. Empty-permission
    ///     custom roles get a "no authority" line. All other roles join up
    ///     to <see cref="MaxPhrases"/> verb fragments with commas and "and",
    ///     overflow truncated with an ellipsis.
    /// </summary>
    public static string BuildSummary(string roleUID, IReadOnlyCollection<string> permissions)
    {
        var loc = LocalizationService.Instance;

        if (roleUID == RoleDefaults.FOUNDER_ROLE_ID)
            return loc.Get(LocalizationKeys.UI_RELIGION_ROLES_FOUNDER_SUMMARY);

        var phrases = OrderedPermissions
            .Where(permissions.Contains)
            .Select(p => loc.Get(PhraseKeys[p]))
            .ToList();

        if (phrases.Count == 0)
            return loc.Get(LocalizationKeys.UI_RELIGION_ROLES_SUMMARY_NONE);

        var overflow = phrases.Count > MaxPhrases;
        if (overflow) phrases = phrases.Take(MaxPhrases).ToList();

        string joined;
        if (phrases.Count == 1)
            joined = phrases[0];
        else
            joined = string.Join(", ", phrases.Take(phrases.Count - 1))
                     + loc.Get(LocalizationKeys.UI_RELIGION_ROLES_SUMMARY_AND)
                     + phrases[^1];

        if (overflow) joined += loc.Get(LocalizationKeys.UI_RELIGION_ROLES_SUMMARY_OVERFLOW);

        return loc.Get(LocalizationKeys.UI_RELIGION_ROLES_SUMMARY_PREFIX, joined);
    }
}
