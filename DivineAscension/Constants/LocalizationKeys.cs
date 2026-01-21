namespace DivineAscension.Constants;

/// <summary>
///     Centralized localization keys for Divine Ascension mod.
///     All user-facing strings should use these constants to enable multi-language support.
/// </summary>
public static class LocalizationKeys
{
    #region Rank Generic

    public const string UI_RANK_UNKNOWN = "divineascension:ui.rank.unknown";

    #endregion

    #region Command Helpers

    public const string CMD_ERROR_NO_DEITY = "divineascension:cmd.error.no_deity";

    #endregion

    #region Main UI Tabs

    public const string UI_TAB_RELIGION = "divineascension:ui.tab.religion";
    public const string UI_TAB_BLESSINGS = "divineascension:ui.tab.blessings";
    public const string UI_TAB_CIVILIZATION = "divineascension:ui.tab.civilization";

    #endregion

    #region Religion Tab - Subtabs

    public const string UI_RELIGION_TAB_BROWSE = "divineascension:ui.religion.tab.browse";
    public const string UI_RELIGION_TAB_INFO = "divineascension:ui.religion.tab.info";
    public const string UI_RELIGION_TAB_ACTIVITY = "divineascension:ui.religion.tab.activity";
    public const string UI_RELIGION_TAB_ROLES = "divineascension:ui.religion.tab.roles";
    public const string UI_RELIGION_TAB_INVITES = "divineascension:ui.religion.tab.invites";
    public const string UI_RELIGION_TAB_CREATE = "divineascension:ui.religion.tab.create";

    #endregion

    #region Religion - Create

    public const string UI_RELIGION_CREATE_TITLE = "divineascension:ui.religion.create.title";
    public const string UI_RELIGION_NAME_LABEL = "divineascension:ui.religion.name.label";
    public const string UI_RELIGION_NAME_PLACEHOLDER = "divineascension:ui.religion.name.placeholder";
    public const string UI_RELIGION_NAME_ERROR_TOO_SHORT = "divineascension:ui.religion.name.error.too_short";
    public const string UI_RELIGION_NAME_ERROR_TOO_LONG = "divineascension:ui.religion.name.error.too_long";
    public const string UI_RELIGION_NAME_ERROR_PROFANITY = "divineascension:ui.religion.name.error.profanity";
    public const string UI_RELIGION_DOMAIN_LABEL = "divineascension:ui.religion.domain.label";
    public const string UI_RELIGION_DEITY_NAME_LABEL = "divineascension:ui.religion.deity_name.label";
    public const string UI_RELIGION_DEITY_NAME_PLACEHOLDER = "divineascension:ui.religion.deity_name.placeholder";

    public const string UI_RELIGION_DEITY_NAME_ERROR_TOO_SHORT =
        "divineascension:ui.religion.deity_name.error.too_short";

    public const string UI_RELIGION_DEITY_NAME_ERROR_TOO_LONG = "divineascension:ui.religion.deity_name.error.too_long";
    public const string UI_RELIGION_PUBLIC_CHECKBOX = "divineascension:ui.religion.public.checkbox";
    public const string UI_RELIGION_CREATE_BUTTON = "divineascension:ui.religion.create.button";

    #endregion

    #region Religion - Browse

    public const string UI_RELIGION_BROWSE_FILTER = "divineascension:ui.religion.browse.filter";
    public const string UI_RELIGION_BROWSE_ALL = "divineascension:ui.religion.browse.all";
    public const string UI_RELIGION_BROWSE_REFRESH = "divineascension:ui.religion.browse.refresh";
    public const string UI_RELIGION_BROWSE_NO_RELIGIONS = "divineascension:ui.religion.browse.no_religions";
    public const string UI_RELIGION_BROWSE_LOADING = "divineascension:ui.religion.browse.loading";
    public const string UI_RELIGION_LIST_INFO_SHORT = "divineascension:ui.religion.list.info.short";
    public const string UI_RELIGION_LIST_MEMBERS_LABEL = "divineascension:ui.religion.list.members_label";
    public const string UI_RELIGION_LIST_PRESTIGE_LABEL = "divineascension:ui.religion.list.prestige_label";
    public const string UI_RELIGION_LIST_STATUS_LABEL = "divineascension:ui.religion.list.status_label";
    public const string UI_RELIGION_LIST_DESCRIPTION_LABEL = "divineascension:ui.religion.list.description_label";

    #endregion

    #region Religion - Detail

    public const string UI_RELIGION_DETAIL_LOADING = "divineascension:ui.religion.detail.loading";
    public const string UI_RELIGION_DETAIL_BACK = "divineascension:ui.religion.detail.back";
    public const string UI_RELIGION_DETAIL_NAME = "divineascension:ui.religion.detail.name";
    public const string UI_RELIGION_DETAIL_DEITY = "divineascension:ui.religion.detail.deity";
    public const string UI_RELIGION_DETAIL_PRESTIGE = "divineascension:ui.religion.detail.prestige";
    public const string UI_RELIGION_DETAIL_PUBLIC = "divineascension:ui.religion.detail.public";
    public const string UI_RELIGION_DETAIL_DESCRIPTION = "divineascension:ui.religion.detail.description";
    public const string UI_RELIGION_DETAIL_MEMBERS = "divineascension:ui.religion.detail.members";
    public const string UI_RELIGION_DETAIL_NO_MEMBERS = "divineascension:ui.religion.detail.no_members";

    #endregion

    #region Religion - Roles

    public const string UI_RELIGION_ROLES_LOADING = "divineascension:ui.religion.roles.loading";
    public const string UI_RELIGION_ROLES_NOT_IN_RELIGION = "divineascension:ui.religion.roles.not_in_religion";
    public const string UI_RELIGION_ROLES_FAILED = "divineascension:ui.religion.roles.failed";
    public const string UI_RELIGION_ROLES_TITLE = "divineascension:ui.religion.roles.title";
    public const string UI_RELIGION_ROLES_CREATE_BUTTON = "divineascension:ui.religion.roles.create_button";
    public const string UI_RELIGION_ROLES_CREATE_DIALOG_TITLE = "divineascension:ui.religion.roles.create_dialog.title";
    public const string UI_RELIGION_ROLES_NAME_LABEL = "divineascension:ui.religion.roles.name.label";
    public const string UI_RELIGION_ROLES_SYSTEM_ROLE = "divineascension:ui.religion.roles.system_role";
    public const string UI_RELIGION_ROLES_DEFAULT_ROLE = "divineascension:ui.religion.roles.default_role";
    public const string UI_RELIGION_ROLES_ALL_PERMISSIONS = "divineascension:ui.religion.roles.all_permissions";
    public const string UI_RELIGION_ROLES_PERMISSIONS_COUNT = "divineascension:ui.religion.roles.permissions_count";
    public const string UI_RELIGION_ROLES_VIEW_DETAILS = "divineascension:ui.religion.roles.view_details";
    public const string UI_RELIGION_ROLES_DELETE = "divineascension:ui.religion.roles.delete";
    public const string UI_RELIGION_ROLES_EDIT_TITLE = "divineascension:ui.religion.roles.edit_title";
    public const string UI_RELIGION_ROLES_FOUNDER_NO_EDIT = "divineascension:ui.religion.roles.founder_no_edit";
    public const string UI_RELIGION_ROLES_MEMBER_COUNT = "divineascension:ui.religion.roles.member_count";
    public const string UI_RELIGION_ROLES_CREATE_BUTTON_TEXT = "divineascension:ui.religion.roles.create";
    public const string UI_RELIGION_ROLES_PERMISSIONS_LABEL = "divineascension:ui.religion.roles.permissions_label";
    public const string UI_RELIGION_ROLES_SAVE_BUTTON = "divineascension:ui.religion.roles.save";

    public const string UI_RELIGION_ROLES_DELETE_CONFIRM_TITLE =
        "divineascension:ui.religion.roles.delete_confirm.title";

    public const string UI_RELIGION_ROLES_DELETE_CONFIRM_MESSAGE =
        "divineascension:ui.religion.roles.delete_confirm.message";

    public const string UI_RELIGION_ROLES_DELETE_CONFIRM_BUTTON =
        "divineascension:ui.religion.roles.delete_confirm.button";

    #endregion

    #region Religion - Role Detail

    public const string UI_RELIGION_ROLE_DETAIL_BACK = "divineascension:ui.religion.role_detail.back";
    public const string UI_RELIGION_ROLE_DETAIL_TITLE = "divineascension:ui.religion.role_detail.title";
    public const string UI_RELIGION_ROLE_DETAIL_NO_MEMBERS = "divineascension:ui.religion.role_detail.no_members";
    public const string UI_RELIGION_ROLE_DETAIL_UNKNOWN_ROLE = "divineascension:ui.religion.role_detail.unknown_role";
    public const string UI_RELIGION_ROLE_DETAIL_NO_ROLE = "divineascension:ui.religion.role_detail.no_role";
    public const string UI_RELIGION_ROLE_DETAIL_YOUR_ROLE = "divineascension:ui.religion.role_detail.your_role";
    public const string UI_RELIGION_ROLE_DETAIL_FOUNDER_ROLE = "divineascension:ui.religion.role_detail.founder_role";

    public const string UI_RELIGION_ROLE_DETAIL_ASSIGN_CONFIRM_TITLE =
        "divineascension:ui.religion.role_detail.assign_confirm.title";

    public const string UI_RELIGION_ROLE_DETAIL_ASSIGN_CONFIRM_MESSAGE =
        "divineascension:ui.religion.role_detail.assign_confirm.message";

    public const string UI_RELIGION_ROLE_DETAIL_ASSIGN_CONFIRM_BUTTON =
        "divineascension:ui.religion.role_detail.assign_confirm.button";

    #endregion

    #region Religion - Invites

    public const string UI_RELIGION_INVITES_TITLE = "divineascension:ui.religion.invites.title";
    public const string UI_RELIGION_INVITES_SUBTITLE = "divineascension:ui.religion.invites.subtitle";
    public const string UI_RELIGION_INVITES_CARD_TITLE = "divineascension:ui.religion.invites.card.title";
    public const string UI_RELIGION_INVITES_RELIGION_LABEL = "divineascension:ui.religion.invites.religion.label";
    public const string UI_RELIGION_INVITES_ACCEPT = "divineascension:ui.religion.invites.accept";
    public const string UI_RELIGION_INVITES_DECLINE = "divineascension:ui.religion.invites.decline";
    public const string UI_RELIGION_INVITES_LOADING = "divineascension:ui.religion.invites.loading";

    #endregion

    #region Religion - Activity

    public const string UI_RELIGION_ACTIVITY_TITLE = "divineascension:ui.religion.activity.title";
    public const string UI_RELIGION_ACTIVITY_DESCRIPTION = "divineascension:ui.religion.activity.description";
    public const string UI_RELIGION_ACTIVITY_LOADING = "divineascension:ui.religion.activity.loading";
    public const string UI_RELIGION_ACTIVITY_EMPTY = "divineascension:ui.religion.activity.empty";
    public const string UI_RELIGION_ACTIVITY_ERROR = "divineascension:ui.religion.activity.error";
    public const string UI_RELIGION_ACTIVITY_REFRESH = "divineascension:ui.religion.activity.refresh";

    #endregion

    #region Religion - Info

    public const string UI_RELIGION_INFO_LOADING = "divineascension:ui.religion.info.loading";
    public const string UI_RELIGION_INFO_NO_RELIGION = "divineascension:ui.religion.info.no_religion";
    public const string UI_RELIGION_INFO_MEMBERS_LABEL = "divineascension:ui.religion.info.members.label";
    public const string UI_RELIGION_INFO_MEMBERS_NO_MEMBERS = "divineascension:ui.religion.info.members.no_members";
    public const string UI_RELIGION_INFO_BANNED_LABEL = "divineascension:ui.religion.info.banned.label";
    public const string UI_RELIGION_INFO_BANNED_NO_PLAYERS = "divineascension:ui.religion.info.banned.no_players";
    public const string UI_RELIGION_INFO_BANNED_NEVER = "divineascension:ui.religion.info.banned.never";
    public const string UI_RELIGION_INFO_BANNED_AT_LABEL = "divineascension:ui.religion.info.banned.at_label";
    public const string UI_RELIGION_INFO_BANNED_EXPIRES_LABEL = "divineascension:ui.religion.info.banned.expires_label";
    public const string UI_RELIGION_INFO_BAN_BUTTON = "divineascension:ui.religion.info.ban.button";
    public const string UI_RELIGION_INFO_UNBAN_BUTTON = "divineascension:ui.religion.info.unban.button";
    public const string UI_RELIGION_INFO_DISBAND_TITLE = "divineascension:ui.religion.info.disband.title";
    public const string UI_RELIGION_INFO_DISBAND_MESSAGE = "divineascension:ui.religion.info.disband.message";
    public const string UI_RELIGION_INFO_DISBAND_CONFIRM = "divineascension:ui.religion.info.disband.confirm";
    public const string UI_RELIGION_INFO_KICK_TITLE = "divineascension:ui.religion.info.kick.title";
    public const string UI_RELIGION_INFO_KICK_MESSAGE = "divineascension:ui.religion.info.kick.message";
    public const string UI_RELIGION_INFO_KICK_CONFIRM = "divineascension:ui.religion.info.kick.confirm";
    public const string UI_RELIGION_INFO_BAN_TITLE = "divineascension:ui.religion.info.ban.title";
    public const string UI_RELIGION_INFO_BAN_MESSAGE = "divineascension:ui.religion.info.ban.message";
    public const string UI_RELIGION_INFO_BAN_CONFIRM = "divineascension:ui.religion.info.ban.confirm";
    public const string UI_RELIGION_INFO_DESCRIPTION_EDITABLE = "divineascension:ui.religion.info.description.editable";
    public const string UI_RELIGION_INFO_DESCRIPTION_LABEL = "divineascension:ui.religion.info.description.label";
    public const string UI_RELIGION_INFO_DESCRIPTION_EMPTY = "divineascension:ui.religion.info.description.empty";
    public const string UI_RELIGION_INFO_SAVE_DESCRIPTION = "divineascension:ui.religion.info.save_description";
    public const string UI_RELIGION_INFO_DEITY_LABEL = "divineascension:ui.religion.info.deity";
    public const string UI_RELIGION_DEITY_NAME_SAVE = "divineascension:ui.religion.deity_name.save";
    public const string UI_RELIGION_DEITY_NAME_SAVING = "divineascension:ui.religion.deity_name.saving";
    public const string UI_RELIGION_DEITY_NAME_CANCEL = "divineascension:ui.religion.deity_name.cancel";
    public const string UI_RELIGION_INFO_MEMBERS_COUNT = "divineascension:ui.religion.info.members_count";
    public const string UI_RELIGION_INFO_FOUNDER_LABEL = "divineascension:ui.religion.info.founder";
    public const string UI_RELIGION_INFO_PRESTIGE_LABEL = "divineascension:ui.religion.info.prestige";
    public const string UI_RELIGION_INFO_PRESTIGE_VALUE = "divineascension:ui.religion.info.prestige_value";
    public const string UI_RELIGION_INFO_INVITE_LABEL = "divineascension:ui.religion.info.invite.label";
    public const string UI_RELIGION_INFO_INVITE_PLACEHOLDER = "divineascension:ui.religion.info.invite.placeholder";
    public const string UI_RELIGION_INFO_INVITE_BUTTON = "divineascension:ui.religion.info.invite.button";

    #endregion

    #region Religion - Actions

    public const string UI_RELIGION_ACTION_JOIN = "divineascension:ui.religion.action.join";
    public const string UI_RELIGION_ACTION_LEAVE = "divineascension:ui.religion.action.leave";
    public const string UI_RELIGION_ACTION_DISBAND = "divineascension:ui.religion.action.disband";

    #endregion

    #region Blessing Tab

    public const string UI_BLESSING_NO_RELIGION = "divineascension:ui.blessing.no_religion";
    public const string UI_BLESSING_PLAYER_PROGRESS = "divineascension:ui.blessing.player_progress";
    public const string UI_BLESSING_RELIGION_PROGRESS = "divineascension:ui.blessing.religion_progress";
    public const string UI_BLESSING_CIVILIZATION = "divineascension:ui.blessing.civilization";
    public const string UI_BLESSING_MEMBER_COUNT = "divineascension:ui.blessing.member_count";
    public const string UI_BLESSING_NO_MEMBERS = "divineascension:ui.blessing.no_members";
    public const string UI_BLESSING_SELECT_TO_VIEW = "divineascension:ui.blessing.select_to_view";
    public const string UI_BLESSING_REQUIREMENTS = "divineascension:ui.blessing.requirements";
    public const string UI_BLESSING_EFFECTS = "divineascension:ui.blessing.effects";
    public const string UI_BLESSING_UNLOCK_BUTTON = "divineascension:ui.blessing.unlock";
    public const string UI_BLESSING_FAVOR_RANK_REQUIREMENT = "divineascension:ui.blessing.favor_rank_requirement";
    public const string UI_BLESSING_PRESTIGE_RANK_REQUIREMENT = "divineascension:ui.blessing.prestige_rank_requirement";
    public const string UI_BLESSING_UNLOCK_REQUIREMENT = "divineascension:ui.blessing.unlock_requirement";
    public const string UI_BLESSING_TREE_PLAYER_PANEL = "divineascension:ui.blessing.tree.player_panel";
    public const string UI_BLESSING_TREE_RELIGION_PANEL = "divineascension:ui.blessing.tree.religion_panel";
    public const string UI_BLESSING_TREE_NO_BLESSINGS = "divineascension:ui.blessing.tree.no_blessings";
    public const string UI_BLESSING_UNKNOWN_RELIGION = "divineascension:ui.blessing.unknown_religion";
    public const string UI_BLESSING_UNKNOWN_CIVILIZATION = "divineascension:ui.blessing.unknown_civilization";
    public const string UI_BLESSING_RELIGIONS_COUNT = "divineascension:ui.blessing.religions_count";

    #endregion

    #region Blessing - Tooltip

    public const string UI_BLESSING_CATEGORY_TIER = "divineascension:ui.blessing.category_tier";
    public const string UI_BLESSING_RELIGION_LABEL = "divineascension:ui.blessing.religion_label";
    public const string UI_BLESSING_REQUIRES_FAVOR_RANK = "divineascension:ui.blessing.requires_favor_rank";
    public const string UI_BLESSING_REQUIRES_PRESTIGE_RANK = "divineascension:ui.blessing.requires_prestige_rank";
    public const string UI_BLESSING_REQUIRES_BLESSING = "divineascension:ui.blessing.requires_blessing";
    public const string UI_BLESSING_UNLOCKED = "divineascension:ui.blessing.unlocked";
    public const string UI_BLESSING_CLICK_TO_UNLOCK = "divineascension:ui.blessing.click_to_unlock";
    public const string UI_BLESSING_LOCKED = "divineascension:ui.blessing.locked";
    public const string UI_BLESSING_TOOLTIP_REQUIRES_FAVOR = "divineascension:ui.blessing.tooltip.requires_favor";
    public const string UI_BLESSING_TOOLTIP_REQUIRES_PRESTIGE = "divineascension:ui.blessing.tooltip.requires_prestige";
    public const string UI_BLESSING_TOOLTIP_REQUIRES_UNLOCK = "divineascension:ui.blessing.tooltip.requires_unlock";
    public const string UI_BLESSING_TOOLTIP_CLICK_TO_UNLOCK = "divineascension:ui.blessing.tooltip.click_to_unlock";
    public const string UI_BLESSING_TOOLTIP_LOCKED = "divineascension:ui.blessing.tooltip.locked";

    #endregion

    #region Civilization Tab

    public const string UI_CIVILIZATION_TAB_BROWSE = "divineascension:ui.civilization.tab.browse";
    public const string UI_CIVILIZATION_TAB_INFO = "divineascension:ui.civilization.tab.info";
    public const string UI_CIVILIZATION_TAB_INVITES = "divineascension:ui.civilization.tab.invites";
    public const string UI_CIVILIZATION_TAB_CREATE = "divineascension:ui.civilization.tab.create";
    public const string UI_CIVILIZATION_TAB_DIPLOMACY = "divineascension:ui.civilization.tab.diplomacy";
    public const string UI_CIVILIZATION_TAB_HOLYSITES = "divineascension:ui.civilization.tab.holysites";

    #endregion

    #region Civilization - Holy Sites

    public const string UI_CIVILIZATION_HOLYSITES_TITLE = "divineascension:ui.civilization.holysites.title";
    public const string UI_CIVILIZATION_HOLYSITES_REFRESH = "divineascension:ui.civilization.holysites.refresh";
    public const string UI_CIVILIZATION_HOLYSITES_LOADING = "divineascension:ui.civilization.holysites.loading";
    public const string UI_CIVILIZATION_HOLYSITES_EMPTY = "divineascension:ui.civilization.holysites.empty";
    public const string UI_CIVILIZATION_HOLYSITES_ERROR = "divineascension:ui.civilization.holysites.error";
    public const string UI_CIVILIZATION_HOLYSITES_NOT_IN_CIV = "divineascension:ui.civilization.holysites.not_in_civ";
    public const string UI_CIVILIZATION_HOLYSITES_SITE_COUNT = "divineascension:ui.civilization.holysites.site_count";
    public const string UI_CIVILIZATION_HOLYSITES_TIER = "divineascension:ui.civilization.holysites.tier";
    public const string UI_CIVILIZATION_HOLYSITES_VOLUME = "divineascension:ui.civilization.holysites.volume";
    public const string UI_CIVILIZATION_HOLYSITES_MULTIPLIERS = "divineascension:ui.civilization.holysites.multipliers";

    #endregion

    #region Civilization - Browse

    public const string UI_CIVILIZATION_BROWSE_FILTER = "divineascension:ui.civilization.browse.filter";
    public const string UI_CIVILIZATION_BROWSE_REFRESH = "divineascension:ui.civilization.browse.refresh";
    public const string UI_CIVILIZATION_BROWSE_NO_CIVS = "divineascension:ui.civilization.browse.no_civs";
    public const string UI_CIVILIZATION_BROWSE_LOADING = "divineascension:ui.civilization.browse.loading";
    public const string UI_CIVILIZATION_BROWSE_MEMBERS_LABEL = "divineascension:ui.civilization.browse.members";
    public const string UI_CIVILIZATION_BROWSE_VIEW_DETAILS = "divineascension:ui.civilization.browse.view_details";
    public const string UI_CIVILIZATION_BROWSE_HEADER_NAME = "divineascension:ui.civilization.browse.header.name";

    public const string UI_CIVILIZATION_BROWSE_HEADER_RELIGIONS =
        "divineascension:ui.civilization.browse.header.religions";

    public const string UI_CIVILIZATION_BROWSE_HEADER_DESCRIPTION =
        "divineascension:ui.civilization.browse.header.description";

    #endregion

    #region Civilization - Detail

    public const string UI_CIVILIZATION_DETAIL_LOADING = "divineascension:ui.civilization.detail.loading";
    public const string UI_CIVILIZATION_DETAIL_BACK = "divineascension:ui.civilization.detail.back";
    public const string UI_CIVILIZATION_DETAIL_FOUNDED = "divineascension:ui.civilization.detail.founded";
    public const string UI_CIVILIZATION_DETAIL_MEMBERS = "divineascension:ui.civilization.detail.members";
    public const string UI_CIVILIZATION_DETAIL_FOUNDER = "divineascension:ui.civilization.detail.founder";

    public const string UI_CIVILIZATION_DETAIL_FOUNDING_RELIGION =
        "divineascension:ui.civilization.detail.founding_religion";

    public const string UI_CIVILIZATION_DETAIL_MEMBER_RELIGIONS =
        "divineascension:ui.civilization.detail.member_religions";

    public const string UI_CIVILIZATION_DETAIL_NO_MEMBERS = "divineascension:ui.civilization.detail.no_members";

    public const string UI_CIVILIZATION_DETAIL_CAN_RECEIVE_INVITE =
        "divineascension:ui.civilization.detail.can_receive_invite";

    public const string UI_CIVILIZATION_DETAIL_FULL = "divineascension:ui.civilization.detail.full";
    public const string UI_CIVILIZATION_DETAIL_ALREADY_MEMBER = "divineascension:ui.civilization.detail.already_member";

    public const string UI_CIVILIZATION_DETAIL_MEMBER_CARD_INFO =
        "divineascension:ui.civilization.detail.member_card.info";

    public const string UI_CIVILIZATION_DETAIL_DESCRIPTION = "divineascension:ui.civilization.detail.description";

    #endregion

    #region Civilization - Create

    public const string UI_CIVILIZATION_CREATE_TITLE = "divineascension:ui.civilization.create.title";
    public const string UI_CIVILIZATION_CREATE_REQUIREMENTS = "divineascension:ui.civilization.create.requirements";
    public const string UI_CIVILIZATION_CREATE_REQ_FOUNDER = "divineascension:ui.civilization.create.req.founder";
    public const string UI_CIVILIZATION_CREATE_REQ_NOT_IN_CIV = "divineascension:ui.civilization.create.req.not_in_civ";

    public const string UI_CIVILIZATION_CREATE_REQ_NAME_LENGTH =
        "divineascension:ui.civilization.create.req.name_length";

    public const string UI_CIVILIZATION_CREATE_NAME_LABEL = "divineascension:ui.civilization.create.name.label";

    public const string UI_CIVILIZATION_CREATE_NAME_PLACEHOLDER =
        "divineascension:ui.civilization.create.name.placeholder";

    public const string UI_CIVILIZATION_NAME_ERROR_TOO_SHORT =
        "divineascension:ui.civilization.name.error.too_short";

    public const string UI_CIVILIZATION_NAME_ERROR_TOO_LONG =
        "divineascension:ui.civilization.name.error.too_long";

    public const string UI_CIVILIZATION_NAME_ERROR_PROFANITY =
        "divineascension:ui.civilization.name.error.profanity";

    public const string UI_CIVILIZATION_CREATE_ICON_LABEL = "divineascension:ui.civilization.create.icon.label";

    public const string UI_CIVILIZATION_CREATE_DESCRIPTION_LABEL =
        "divineascension:ui.civilization.create.description.label";

    public const string UI_CIVILIZATION_CREATE_DESCRIPTION_PLACEHOLDER =
        "divineascension:ui.civilization.create.description.placeholder";

    public const string UI_CIVILIZATION_DESCRIPTION_ERROR_TOO_LONG =
        "divineascension:ui.civilization.description.error.too_long";

    public const string UI_CIVILIZATION_DESCRIPTION_ERROR_PROFANITY =
        "divineascension:ui.civilization.description.error.profanity";

    public const string UI_CIVILIZATION_CREATE_BUTTON = "divineascension:ui.civilization.create.button";
    public const string UI_CIVILIZATION_CREATE_CLEAR_BUTTON = "divineascension:ui.civilization.create.clear";
    public const string UI_CIVILIZATION_CREATE_INFO_TEXT = "divineascension:ui.civilization.create.info_text";

    #endregion

    #region Civilization - Info

    public const string UI_CIVILIZATION_INFO_LOADING = "divineascension:ui.civilization.info.loading";
    public const string UI_CIVILIZATION_INFO_NOT_IN_CIV = "divineascension:ui.civilization.info.not_in_civ";
    public const string UI_CIVILIZATION_INFO_FOUNDED = "divineascension:ui.civilization.info.founded";
    public const string UI_CIVILIZATION_INFO_MEMBERS = "divineascension:ui.civilization.info.members";
    public const string UI_CIVILIZATION_INFO_FOUNDER = "divineascension:ui.civilization.info.founder";

    public const string UI_CIVILIZATION_INFO_FOUNDING_RELIGION =
        "divineascension:ui.civilization.info.founding_religion";

    public const string UI_CIVILIZATION_INFO_DESCRIPTION_LABEL =
        "divineascension:ui.civilization.info.description.label";

    public const string UI_CIVILIZATION_INFO_DESCRIPTION_PLACEHOLDER =
        "divineascension:ui.civilization.info.description.placeholder";

    public const string UI_CIVILIZATION_INFO_SAVE_DESCRIPTION_BUTTON =
        "divineascension:ui.civilization.info.save_description";

    public const string UI_CIVILIZATION_INFO_MEMBER_RELIGIONS = "divineascension:ui.civilization.info.member_religions";
    public const string UI_CIVILIZATION_INFO_INVITE_LABEL = "divineascension:ui.civilization.info.invite.label";

    public const string UI_CIVILIZATION_INFO_INVITE_PLACEHOLDER =
        "divineascension:ui.civilization.info.invite.placeholder";

    public const string UI_CIVILIZATION_INFO_INVITE_BUTTON = "divineascension:ui.civilization.info.invite.button";

    public const string UI_CIVILIZATION_INFO_INVITATIONS_LOADING =
        "divineascension:ui.civilization.info.invitations.loading";

    public const string UI_CIVILIZATION_INFO_PENDING_INVITATIONS =
        "divineascension:ui.civilization.info.pending_invitations";

    public const string UI_CIVILIZATION_INFO_LEAVE_BUTTON = "divineascension:ui.civilization.info.leave";
    public const string UI_CIVILIZATION_INFO_EDIT_ICON_BUTTON = "divineascension:ui.civilization.info.edit_icon";
    public const string UI_CIVILIZATION_INFO_DISBAND_BUTTON = "divineascension:ui.civilization.info.disband";

    public const string UI_CIVILIZATION_INFO_DISBAND_CONFIRM_TITLE =
        "divineascension:ui.civilization.info.disband.confirm.title";

    public const string UI_CIVILIZATION_INFO_DISBAND_CONFIRM_MESSAGE =
        "divineascension:ui.civilization.info.disband.confirm.message";

    public const string UI_CIVILIZATION_INFO_DISBAND_CONFIRM_BUTTON =
        "divineascension:ui.civilization.info.disband.confirm.button";

    public const string UI_CIVILIZATION_INFO_KICK_CONFIRM_TITLE =
        "divineascension:ui.civilization.info.kick.confirm.title";

    public const string UI_CIVILIZATION_INFO_KICK_CONFIRM_MESSAGE =
        "divineascension:ui.civilization.info.kick.confirm.message";

    public const string UI_CIVILIZATION_INFO_KICK_CONFIRM_BUTTON =
        "divineascension:ui.civilization.info.kick.confirm.button";

    public const string UI_CIVILIZATION_INFO_KICK_BUTTON = "divineascension:ui.civilization.info.kick.button";
    public const string UI_CIVILIZATION_INFO_UNKNOWN_RELIGION = "divineascension:ui.civilization.info.unknown_religion";

    public const string UI_CIVILIZATION_INFO_RELIGION_CARD_DEITY =
        "divineascension:ui.civilization.info.religion_card.deity";

    public const string UI_CIVILIZATION_INFO_RELIGION_CARD_MEMBERS =
        "divineascension:ui.civilization.info.religion_card.members";

    public const string UI_CIVILIZATION_INFO_INVITE_EXPIRES = "divineascension:ui.civilization.info.invite.expires";

    // Civilization edit dialog
    public const string UI_CIVILIZATION_EDIT_TITLE = "divineascension:ui.civilization.edit.title";
    public const string UI_CIVILIZATION_EDIT_CIV_LABEL = "divineascension:ui.civilization.edit.civ_label";
    public const string UI_CIVILIZATION_EDIT_CURRENT_ICON = "divineascension:ui.civilization.edit.current_icon";
    public const string UI_CIVILIZATION_EDIT_ICON_LABEL = "divineascension:ui.civilization.edit.icon_label";
    public const string UI_CIVILIZATION_EDIT_SELECT_ICON = "divineascension:ui.civilization.edit.select_icon";
    public const string UI_CIVILIZATION_EDIT_UPDATE_BUTTON = "divineascension:ui.civilization.edit.update";

    // Civilization invites
    public const string UI_CIVILIZATION_INVITES_TITLE = "divineascension:ui.civilization.invites.title";
    public const string UI_CIVILIZATION_INVITES_DESCRIPTION = "divineascension:ui.civilization.invites.description";

    public const string UI_CIVILIZATION_INVITES_NO_INVITATIONS =
        "divineascension:ui.civilization.invites.no_invitations";

    public const string UI_CIVILIZATION_INVITES_LOADING = "divineascension:ui.civilization.invites.loading";
    public const string UI_CIVILIZATION_INVITES_CARD_TITLE = "divineascension:ui.civilization.invites.card.title";
    public const string UI_CIVILIZATION_INVITES_CARD_FROM = "divineascension:ui.civilization.invites.card.from";
    public const string UI_CIVILIZATION_INVITES_CARD_EXPIRES = "divineascension:ui.civilization.invites.card.expires";
    public const string UI_CIVILIZATION_INVITES_ACCEPT_BUTTON = "divineascension:ui.civilization.invites.accept";
    public const string UI_CIVILIZATION_INVITES_DECLINE_BUTTON = "divineascension:ui.civilization.invites.decline";

    #endregion

    #region Diplomacy

    public const string UI_DIPLOMACY_LOADING = "divineascension:ui.diplomacy.loading";
    public const string UI_DIPLOMACY_NO_CIVILIZATION = "divineascension:ui.diplomacy.no_civilization";
    public const string UI_DIPLOMACY_CURRENT_RELATIONSHIPS = "divineascension:ui.diplomacy.current_relationships";
    public const string UI_DIPLOMACY_NO_RELATIONSHIPS = "divineascension:ui.diplomacy.no_relationships";
    public const string UI_DIPLOMACY_COL_CIVILIZATION = "divineascension:ui.diplomacy.col.civilization";
    public const string UI_DIPLOMACY_COL_STATUS = "divineascension:ui.diplomacy.col.status";
    public const string UI_DIPLOMACY_COL_ESTABLISHED = "divineascension:ui.diplomacy.col.established";
    public const string UI_DIPLOMACY_COL_EXPIRES = "divineascension:ui.diplomacy.col.expires";
    public const string UI_DIPLOMACY_COL_VIOLATIONS = "divineascension:ui.diplomacy.col.violations";
    public const string UI_DIPLOMACY_COL_ACTIONS = "divineascension:ui.diplomacy.col.actions";
    public const string UI_DIPLOMACY_PERMANENT = "divineascension:ui.diplomacy.permanent";
    public const string UI_DIPLOMACY_BREAKS_IN = "divineascension:ui.diplomacy.breaks_in";
    public const string UI_DIPLOMACY_CANCEL_BUTTON = "divineascension:ui.diplomacy.cancel_button";
    public const string UI_DIPLOMACY_SCHEDULE_BREAK_BUTTON = "divineascension:ui.diplomacy.schedule_break";
    public const string UI_DIPLOMACY_DECLARE_PEACE_BUTTON = "divineascension:ui.diplomacy.declare_peace";
    public const string UI_DIPLOMACY_PENDING_PROPOSALS = "divineascension:ui.diplomacy.pending_proposals";
    public const string UI_DIPLOMACY_INCOMING_LABEL = "divineascension:ui.diplomacy.incoming";
    public const string UI_DIPLOMACY_OUTGOING_LABEL = "divineascension:ui.diplomacy.outgoing";
    public const string UI_DIPLOMACY_ACCEPT_BUTTON = "divineascension:ui.diplomacy.accept";
    public const string UI_DIPLOMACY_DECLINE_BUTTON = "divineascension:ui.diplomacy.decline";
    public const string UI_DIPLOMACY_NO_PROPOSALS = "divineascension:ui.diplomacy.no_proposals";
    public const string UI_DIPLOMACY_PROPOSE_NEW = "divineascension:ui.diplomacy.propose_new";
    public const string UI_DIPLOMACY_NO_CIVS_AVAILABLE = "divineascension:ui.diplomacy.no_civs_available";
    public const string UI_DIPLOMACY_TARGET_CIV_LABEL = "divineascension:ui.diplomacy.target_civ";
    public const string UI_DIPLOMACY_SELECT_CIV_PLACEHOLDER = "divineascension:ui.diplomacy.select_civ";
    public const string UI_DIPLOMACY_RELATIONSHIP_TYPE_LABEL = "divineascension:ui.diplomacy.relationship_type";
    public const string UI_DIPLOMACY_TYPE_NAP = "divineascension:ui.diplomacy.type.nap";
    public const string UI_DIPLOMACY_TYPE_ALLIANCE = "divineascension:ui.diplomacy.type.alliance";
    public const string UI_DIPLOMACY_DURATION_LABEL = "divineascension:ui.diplomacy.duration";
    public const string UI_DIPLOMACY_DURATION_3DAYS = "divineascension:ui.diplomacy.duration.3days";
    public const string UI_DIPLOMACY_DURATION_PERMANENT = "divineascension:ui.diplomacy.duration.permanent";
    public const string UI_DIPLOMACY_INSUFFICIENT_RANK = "divineascension:ui.diplomacy.insufficient_rank";
    public const string UI_DIPLOMACY_SEND_PROPOSAL_BUTTON = "divineascension:ui.diplomacy.send_proposal";
    public const string UI_DIPLOMACY_DECLARE_WAR_BUTTON = "divineascension:ui.diplomacy.declare_war";
    public const string UI_DIPLOMACY_CONFIRM_WAR_MESSAGE = "divineascension:ui.diplomacy.confirm_war";
    public const string UI_DIPLOMACY_YES_DECLARE_WAR = "divineascension:ui.diplomacy.yes_declare_war";
    public const string UI_DIPLOMACY_STATUS_ALLIANCE = "divineascension:ui.diplomacy.status.alliance";
    public const string UI_DIPLOMACY_STATUS_NAP = "divineascension:ui.diplomacy.status.nap";
    public const string UI_DIPLOMACY_STATUS_WAR = "divineascension:ui.diplomacy.status.war";
    public const string UI_DIPLOMACY_STATUS_NEUTRAL = "divineascension:ui.diplomacy.status.neutral";
    public const string UI_DIPLOMACY_UNKNOWN_CIV = "divineascension:ui.diplomacy.unknown_civ";
    public const string UI_DIPLOMACY_PROPOSAL_TO = "divineascension:ui.diplomacy.proposal_to";

    #endregion

    #region Common UI Components

    public const string UI_COMMON_DISMISS = "divineascension:ui.common.dismiss";
    public const string UI_COMMON_RETRY = "divineascension:ui.common.retry";
    public const string UI_COMMON_CONFIRM = "divineascension:ui.common.confirm";
    public const string UI_COMMON_CANCEL = "divineascension:ui.common.cancel";
    public const string UI_COMMON_EDIT = "divineascension:ui.common.edit";
    public const string UI_COMMON_SAVE = "divineascension:ui.common.save";
    public const string UI_COMMON_LOADING = "divineascension:ui.common.loading";
    public const string UI_COMMON_NO_DATA = "divineascension:ui.common.no_data";
    public const string UI_COMMON_PUBLIC = "divineascension:ui.common.public";
    public const string UI_COMMON_PRIVATE = "divineascension:ui.common.private";
    public const string UI_COMMON_UNKNOWN = "divineascension:ui.common.unknown";
    public const string UI_COMMON_NEVER = "divineascension:ui.common.never";

    #endregion

    #region Rank Up Notification

    public const string UI_RANKUP_TITLE = "divineascension:ui.rankup.title";
    public const string UI_RANKUP_VIEW_BLESSINGS = "divineascension:ui.rankup.view_blessings";

    #endregion

    #region Table Headers

    public const string UI_TABLE_NAME = "divineascension:ui.table.name";
    public const string UI_TABLE_DOMAIN = "divineascension:ui.table.deity_name";
    public const string UI_TABLE_PRESTIGE = "divineascension:ui.table.prestige";
    public const string UI_TABLE_MEMBERS = "divineascension:ui.table.members";
    public const string UI_TABLE_PUBLIC = "divineascension:ui.table.public";

    #endregion

    #region Deity Domain Names

    public const string DOMAIN_CRAFT_NAME = "divineascension:deity.domain.craft.name";
    public const string DOMAIN_CRAFT_TITLE = "divineascension:deity.domain.craft.title";
    public const string DOMAIN_CRAFT_DESCRIPTION = "divineascension:deity.domain.craft.description";

    public const string DOMAIN_WILD_NAME = "divineascension:deity.domain.wild.name";
    public const string DOMAIN_WILD_TITLE = "divineascension:deity.domain.wild.title";
    public const string DOMAIN_WILD_DESCRIPTION = "divineascension:deity.domain.wild.description";

    public const string DOMAIN_HARVEST_NAME = "divineascension:deity.domain.harvest.name";
    public const string DOMAIN_HARVEST_TITLE = "divineascension:deity.domain.harvest.title";
    public const string DOMAIN_HARVEST_DESCRIPTION = "divineascension:deity.domain.harvest.description";

    public const string DOMAIN_STONE_NAME = "divineascension:deity.domain.stone.name";
    public const string DOMAIN_STONE_TITLE = "divineascension:deity.domain.stone.title";
    public const string DOMAIN_STONE_DESCRIPTION = "divineascension:deity.domain.stone.description";

    public const string DOMAIN_UNKNOWN_NAME = "divineascension:deity.domain.unknown.name";
    public const string DOMAIN_LABEL = "divineascension:deity.domain_label";

    #endregion

    #region Favor Rank Names

    public const string RANK_FAVOR_INITIATE = "divineascension:rank.favor.initiate";
    public const string RANK_FAVOR_DISCIPLE = "divineascension:rank.favor.disciple";
    public const string RANK_FAVOR_ZEALOT = "divineascension:rank.favor.zealot";
    public const string RANK_FAVOR_CHAMPION = "divineascension:rank.favor.champion";
    public const string RANK_FAVOR_AVATAR = "divineascension:rank.favor.avatar";

    #endregion

    #region Prestige Rank Names

    public const string RANK_PRESTIGE_FLEDGLING = "divineascension:rank.prestige.fledgling";
    public const string RANK_PRESTIGE_ESTABLISHED = "divineascension:rank.prestige.established";
    public const string RANK_PRESTIGE_RENOWNED = "divineascension:rank.prestige.renowned";
    public const string RANK_PRESTIGE_LEGENDARY = "divineascension:rank.prestige.legendary";
    public const string RANK_PRESTIGE_MYTHIC = "divineascension:rank.prestige.mythic";

    #endregion

    #region Stat Display Names

    public const string STAT_MELEE_DAMAGE = "divineascension:stat.melee_damage";
    public const string STAT_RANGED_DAMAGE = "divineascension:stat.ranged_damage";
    public const string STAT_MOVEMENT_SPEED = "divineascension:stat.movement_speed";
    public const string STAT_MAX_HEALTH = "divineascension:stat.max_health";
    public const string STAT_DAMAGE = "divineascension:stat.damage";
    public const string STAT_HEALTH = "divineascension:stat.health";
    public const string STAT_ARMOR = "divineascension:stat.armor";
    public const string STAT_ARMOR_EFFECTIVENESS = "divineascension:stat.armor_effectiveness";
    public const string STAT_SPEED = "divineascension:stat.speed";
    public const string STAT_MINING_SPEED = "divineascension:stat.mining_speed";
    public const string STAT_ATTACK_SPEED = "divineascension:stat.attack_speed";
    public const string STAT_HEAL_EFFECTIVENESS = "divineascension:stat.heal_effectiveness";
    public const string STAT_HEALTH_REGEN = "divineascension:stat.health_regen";
    public const string STAT_HUNGER_RATE = "divineascension:stat.hunger_rate";
    public const string STAT_WALK_SPEED = "divineascension:stat.walk_speed";
    public const string STAT_TOOL_DURABILITY = "divineascension:stat.tool_durability";
    public const string STAT_ORE_YIELD = "divineascension:stat.ore_yield";
    public const string STAT_COLD_RESISTANCE = "divineascension:stat.cold_resistance";
    public const string STAT_CHOPPING_SPEED = "divineascension:stat.chopping_speed";
    public const string STAT_REPAIR_COST_REDUCTION = "divineascension:stat.repair_cost_reduction";
    public const string STAT_REPAIR_EFFICIENCY = "divineascension:stat.repair_efficiency";
    public const string STAT_SMITHING_COST_REDUCTION = "divineascension:stat.smithing_cost_reduction";
    public const string STAT_METAL_ARMOR_BONUS = "divineascension:stat.metal_armor_bonus";
    public const string STAT_ARMOR_DURABILITY_LOSS = "divineascension:stat.armor_durability_loss";
    public const string STAT_ARMOR_WALK_SPEED = "divineascension:stat.armor_walk_speed";
    public const string STAT_POTTERY_BATCH_COMPLETION = "divineascension:stat.pottery_batch_completion";
    public const string STAT_ANIMAL_LOOT_DROPS = "divineascension:stat.animal_loot_drops";
    public const string STAT_FORAGE_DROPS = "divineascension:stat.forage_drops";
    public const string STAT_ORE_DROPS = "divineascension:stat.ore_drops";
    public const string STAT_MECHANICAL_POWER = "divineascension:stat.mechanical_power";
    public const string STAT_CROP_GROWTH_SPEED = "divineascension:stat.crop_growth_speed";
    public const string STAT_SMITHING_SPEED = "divineascension:stat.smithing_speed";
    public const string STAT_WHOLE_VESSEL_CAPACITY = "divineascension:stat.whole_vessel_capacity";
    public const string STAT_RANGED_WEAPONS_SPEED = "divineascension:stat.ranged_weapons_speed";

    #endregion

    #region Command Errors

    public const string CMD_ERROR_NO_RELIGION = "divineascension:cmd.error.no_religion";
    public const string CMD_ERROR_MUST_JOIN_RELIGION = "divineascension:cmd.error.must_join_religion";
    public const string CMD_ERROR_BLESSING_NOT_FOUND = "divineascension:cmd.error.blessing_not_found";
    public const string CMD_ERROR_PLAYER_NOT_FOUND = "divineascension:cmd.error.player_not_found";
    public const string CMD_ERROR_RELIGION_NOT_FOUND = "divineascension:cmd.error.religion_not_found";
    public const string CMD_ERROR_CIVILIZATION_NOT_FOUND = "divineascension:cmd.error.civilization_not_found";
    public const string CMD_ERROR_ALREADY_IN_RELIGION = "divineascension:cmd.error.already_in_religion";
    public const string CMD_ERROR_NOT_FOUNDER = "divineascension:cmd.error.not_founder";
    public const string CMD_ERROR_INSUFFICIENT_PERMISSIONS = "divineascension:cmd.error.insufficient_permissions";

    #endregion

    #region Command Success Messages

    public const string CMD_SUCCESS_RELIGION_CREATED = "divineascension:cmd.success.religion_created";
    public const string CMD_SUCCESS_RELIGION_JOINED = "divineascension:cmd.success.religion_joined";
    public const string CMD_SUCCESS_RELIGION_LEFT = "divineascension:cmd.success.religion_left";
    public const string CMD_SUCCESS_RELIGION_DISBANDED = "divineascension:cmd.success.religion_disbanded";
    public const string CMD_SUCCESS_BLESSING_UNLOCKED = "divineascension:cmd.success.blessing_unlocked";
    public const string CMD_SUCCESS_CIVILIZATION_CREATED = "divineascension:cmd.success.civilization_created";
    public const string CMD_SUCCESS_INVITATION_SENT = "divineascension:cmd.success.invitation_sent";

    #endregion

    #region Blessing Commands

    // Command descriptions
    public const string CMD_BLESSINGS_DESC = "divineascension:cmd.blessings.desc";
    public const string CMD_BLESSINGS_LIST_DESC = "divineascension:cmd.blessings.list.desc";
    public const string CMD_BLESSINGS_PLAYER_DESC = "divineascension:cmd.blessings.player.desc";
    public const string CMD_BLESSINGS_RELIGION_DESC = "divineascension:cmd.blessings.religion.desc";
    public const string CMD_BLESSINGS_INFO_DESC = "divineascension:cmd.blessings.info.desc";
    public const string CMD_BLESSINGS_TREE_DESC = "divineascension:cmd.blessings.tree.desc";
    public const string CMD_BLESSINGS_UNLOCK_DESC = "divineascension:cmd.blessings.unlock.desc";
    public const string CMD_BLESSINGS_ACTIVE_DESC = "divineascension:cmd.blessings.active.desc";

    // Blessing errors
    public const string CMD_BLESSING_ERROR_MUST_JOIN_FOR_TREE = "divineascension:cmd.blessing.error.must_join_for_tree";

    public const string CMD_BLESSING_ERROR_MUST_BE_IN_RELIGION_TO_UNLOCK =
        "divineascension:cmd.blessing.error.must_be_in_religion";

    public const string CMD_BLESSING_ERROR_ONLY_FOUNDER_CAN_UNLOCK = "divineascension:cmd.blessing.error.only_founder";
    public const string CMD_BLESSING_ERROR_CANNOT_UNLOCK = "divineascension:cmd.blessing.error.cannot_unlock";
    public const string CMD_BLESSING_ERROR_FAILED_TO_UNLOCK = "divineascension:cmd.blessing.error.failed_to_unlock";

    // Usage messages
    public const string CMD_BLESSING_USAGE_INFO = "divineascension:cmd.blessing.usage.info";
    public const string CMD_BLESSING_USAGE_UNLOCK = "divineascension:cmd.blessing.usage.unlock";

    // Success messages
    public const string CMD_BLESSING_SUCCESS_UNLOCKED_PLAYER = "divineascension:cmd.blessing.success.unlocked_player";

    public const string CMD_BLESSING_SUCCESS_UNLOCKED_RELIGION =
        "divineascension:cmd.blessing.success.unlocked_religion";

    public const string CMD_BLESSING_NOTIFICATION_UNLOCKED = "divineascension:cmd.blessing.notification.unlocked";

    // Info messages
    public const string CMD_BLESSING_INFO_NO_PLAYER_BLESSINGS = "divineascension:cmd.blessing.info.no_player";
    public const string CMD_BLESSING_INFO_NO_RELIGION_BLESSINGS = "divineascension:cmd.blessing.info.no_religion";

    // Format strings - headers
    public const string CMD_BLESSING_HEADER_FOR_DEITY = "divineascension:cmd.blessing.header.for_deity";
    public const string CMD_BLESSING_HEADER_PLAYER = "divineascension:cmd.blessing.header.player";
    public const string CMD_BLESSING_HEADER_RELIGION = "divineascension:cmd.blessing.header.religion";
    public const string CMD_BLESSING_HEADER_UNLOCKED_PLAYER = "divineascension:cmd.blessing.header.unlocked_player";

    public const string CMD_BLESSING_HEADER_RELIGION_WITH_NAME =
        "divineascension:cmd.blessing.header.religion_with_name";

    public const string CMD_BLESSING_HEADER_INFO = "divineascension:cmd.blessing.header.info";
    public const string CMD_BLESSING_HEADER_TREE = "divineascension:cmd.blessing.header.tree";
    public const string CMD_BLESSING_HEADER_ACTIVE = "divineascension:cmd.blessing.header.active";
    public const string CMD_BLESSING_HEADER_RANK_SECTION = "divineascension:cmd.blessing.header.rank_section";

    // Format strings - labels
    public const string CMD_BLESSING_LABEL_UNLOCKED = "divineascension:cmd.blessing.label.unlocked";
    public const string CMD_BLESSING_LABEL_CHECKED = "divineascension:cmd.blessing.label.checked";
    public const string CMD_BLESSING_LABEL_UNCHECKED = "divineascension:cmd.blessing.label.unchecked";
    public const string CMD_BLESSING_LABEL_ID = "divineascension:cmd.blessing.label.id";
    public const string CMD_BLESSING_LABEL_DEITY = "divineascension:cmd.blessing.label.deity";
    public const string CMD_BLESSING_LABEL_TYPE = "divineascension:cmd.blessing.label.type";
    public const string CMD_BLESSING_LABEL_CATEGORY = "divineascension:cmd.blessing.label.category";
    public const string CMD_BLESSING_LABEL_DESCRIPTION = "divineascension:cmd.blessing.label.description";

    public const string CMD_BLESSING_LABEL_REQUIRED_FAVOR_RANK =
        "divineascension:cmd.blessing.label.required_favor_rank";

    public const string CMD_BLESSING_LABEL_REQUIRED_PRESTIGE_RANK =
        "divineascension:cmd.blessing.label.required_prestige_rank";

    public const string CMD_BLESSING_LABEL_PREREQUISITES = "divineascension:cmd.blessing.label.prerequisites";
    public const string CMD_BLESSING_LABEL_STAT_MODIFIERS = "divineascension:cmd.blessing.label.stat_modifiers";
    public const string CMD_BLESSING_LABEL_SPECIAL_EFFECTS = "divineascension:cmd.blessing.label.special_effects";
    public const string CMD_BLESSING_LABEL_EFFECTS = "divineascension:cmd.blessing.label.effects";
    public const string CMD_BLESSING_LABEL_EFFECTS_FOR_ALL = "divineascension:cmd.blessing.label.effects_for_all";
    public const string CMD_BLESSING_LABEL_REQUIRES = "divineascension:cmd.blessing.label.requires";
    public const string CMD_BLESSING_LABEL_PLAYER_SECTION = "divineascension:cmd.blessing.label.player_section";
    public const string CMD_BLESSING_LABEL_RELIGION_SECTION = "divineascension:cmd.blessing.label.religion_section";
    public const string CMD_BLESSING_LABEL_COMBINED_MODIFIERS = "divineascension:cmd.blessing.label.combined_modifiers";

    public const string CMD_BLESSING_LABEL_NO_ACTIVE_MODIFIERS =
        "divineascension:cmd.blessing.label.no_active_modifiers";

    public const string CMD_BLESSING_LABEL_NONE = "divineascension:cmd.blessing.label.none";

    // Format strings - item formats
    public const string CMD_BLESSING_FORMAT_ID = "divineascension:cmd.blessing.format.id";
    public const string CMD_BLESSING_FORMAT_NAME = "divineascension:cmd.blessing.format.name";
    public const string CMD_BLESSING_FORMAT_UNLOCKED = "divineascension:cmd.blessing.format.unlocked";
    public const string CMD_BLESSING_FORMAT_LOCKED = "divineascension:cmd.blessing.format.locked";
    public const string CMD_BLESSING_FORMAT_STAT_MODIFIER = "divineascension:cmd.blessing.format.stat_modifier";
    public const string CMD_BLESSING_FORMAT_PREREQUISITE = "divineascension:cmd.blessing.format.prerequisite";
    public const string CMD_BLESSING_FORMAT_NONE = "divineascension:cmd.blessing.format.none";
    public const string CMD_BLESSING_FORMAT_YES = "divineascension:cmd.blessing.format.yes";
    public const string CMD_BLESSING_FORMAT_NO = "divineascension:cmd.blessing.format.no";

    // Additional keys used in code
    public const string CMD_BLESSINGS_ADMIN_DESC = "divineascension:cmd.blessings.admin.desc";
    public const string CMD_BLESSING_ERROR_PLAYER_NOT_FOUND = "divineascension:cmd.blessing.error.player_not_found";
    public const string CMD_BLESSING_ERROR_NOT_IN_RELIGION = "divineascension:cmd.blessing.error.not_in_religion";
    public const string CMD_BLESSING_INFO_NO_PLAYER_UNLOCKED = "divineascension:cmd.blessing.info.no_player_unlocked";

    public const string CMD_BLESSING_INFO_NO_RELIGION_UNLOCKED =
        "divineascension:cmd.blessing.info.no_religion_unlocked";

    public const string CMD_BLESSING_HEADER_PLAYER_BLESSINGS = "divineascension:cmd.blessing.header.player_blessings";

    public const string CMD_BLESSING_HEADER_RELIGION_BLESSINGS =
        "divineascension:cmd.blessing.header.religion_blessings";

    public const string CMD_BLESSING_HEADER_COMBINED_STATS = "divineascension:cmd.blessing.header.combined_stats";
    public const string CMD_BLESSING_LABEL_FAVOR_RANK = "divineascension:cmd.blessing.label.favor_rank";
    public const string CMD_BLESSING_LABEL_PRESTIGE_RANK = "divineascension:cmd.blessing.label.prestige_rank";
    public const string CMD_BLESSING_ERROR_NOT_FOUND = "divineascension:cmd.blessing.error.not_found";
    public const string CMD_BLESSING_HEADER_BLESSING_INFO = "divineascension:cmd.blessing.header.blessing_info";
    public const string CMD_BLESSING_HEADER_BLESSING_TREE = "divineascension:cmd.blessing.header.blessing_tree";
    public const string CMD_BLESSING_SUCCESS_UNLOCKED = "divineascension:cmd.blessing.success.unlocked";
    public const string CMD_BLESSING_HEADER_ACTIVE_BLESSINGS = "divineascension:cmd.blessing.header.active_blessings";
    public const string CMD_BLESSING_LABEL_TOTAL_PLAYER = "divineascension:cmd.blessing.label.total_player";
    public const string CMD_BLESSING_LABEL_TOTAL_RELIGION = "divineascension:cmd.blessing.label.total_religion";
    public const string CMD_BLESSING_FORMAT_NONE_ALIAS = "divineascension:cmd.blessing.format.none";
    public const string CMD_BLESSING_LABEL_NO_ACTIVE = "divineascension:cmd.blessing.label.no_active";
    public const string CMD_BLESSING_SUCCESS_ADMIN_UNLOCKED = "divineascension:cmd.blessing.success.admin_unlocked";
    public const string CMD_BLESSING_SUCCESS_ADMIN_LOCKED = "divineascension:cmd.blessing.success.admin_locked";
    public const string CMD_BLESSING_SUCCESS_ADMIN_RESET = "divineascension:cmd.blessing.success.admin_reset";
    public const string CMD_BLESSING_SUCCESS_ADMIN_UNLOCKALL = "divineascension:cmd.blessing.success.admin_unlockall";
    public const string CMD_BLESSING_ERROR_ALREADY_UNLOCKED = "divineascension:cmd.blessing.error.already_unlocked";

    #endregion

    #region Favor Commands

    // Command descriptions
    public const string CMD_FAVOR_DESC = "divineascension:cmd.favor.desc";
    public const string CMD_FAVOR_GET_DESC = "divineascension:cmd.favor.get.desc";
    public const string CMD_FAVOR_INFO_DESC = "divineascension:cmd.favor.info.desc";
    public const string CMD_FAVOR_STATS_DESC = "divineascension:cmd.favor.stats.desc";
    public const string CMD_FAVOR_RANKS_DESC = "divineascension:cmd.favor.ranks.desc";
    public const string CMD_FAVOR_SET_DESC = "divineascension:cmd.favor.set.desc";
    public const string CMD_FAVOR_ADD_DESC = "divineascension:cmd.favor.add.desc";
    public const string CMD_FAVOR_REMOVE_DESC = "divineascension:cmd.favor.remove.desc";
    public const string CMD_FAVOR_RESET_DESC = "divineascension:cmd.favor.reset.desc";
    public const string CMD_FAVOR_MAX_DESC = "divineascension:cmd.favor.max.desc";
    public const string CMD_FAVOR_SETTOTAL_DESC = "divineascension:cmd.favor.settotal.desc";

    // Error messages
    public const string CMD_FAVOR_ERROR_MUST_BE_PLAYER = "divineascension:cmd.favor.error.must_be_player";
    public const string CMD_FAVOR_ERROR_MUST_HAVE_RELIGION = "divineascension:cmd.favor.error.must_have_religion";
    public const string CMD_FAVOR_ERROR_NEGATIVE_AMOUNT = "divineascension:cmd.favor.error.negative_amount";
    public const string CMD_FAVOR_ERROR_EXCEEDS_MAX = "divineascension:cmd.favor.error.exceeds_max";
    public const string CMD_FAVOR_ERROR_AMOUNT_TOO_SMALL = "divineascension:cmd.favor.error.amount_too_small";
    public const string CMD_FAVOR_ERROR_PLAYER_NOT_FOUND = "divineascension:cmd.favor.error.player_not_found";
    public const string CMD_FAVOR_ERROR_NOT_SERVER_PLAYER = "divineascension:cmd.favor.error.not_server_player";
    public const string CMD_FAVOR_ERROR_TARGET_NO_RELIGION = "divineascension:cmd.favor.error.target_no_religion";
    public const string CMD_FAVOR_ERROR_TOTAL_NEGATIVE = "divineascension:cmd.favor.error.total_negative";
    public const string CMD_FAVOR_ERROR_TOTAL_EXCEEDS_MAX = "divineascension:cmd.favor.error.total_exceeds_max";

    // Success messages
    public const string CMD_FAVOR_SUCCESS_CHECK = "divineascension:cmd.favor.success.check";
    public const string CMD_FAVOR_SUCCESS_SET = "divineascension:cmd.favor.success.set";
    public const string CMD_FAVOR_SUCCESS_ADD = "divineascension:cmd.favor.success.add";
    public const string CMD_FAVOR_SUCCESS_REMOVE = "divineascension:cmd.favor.success.remove";
    public const string CMD_FAVOR_SUCCESS_RESET = "divineascension:cmd.favor.success.reset";
    public const string CMD_FAVOR_SUCCESS_MAX = "divineascension:cmd.favor.success.max";
    public const string CMD_FAVOR_SUCCESS_TOTAL_SET = "divineascension:cmd.favor.success.total_set";
    public const string CMD_FAVOR_SUCCESS_RANK_UPDATE = "divineascension:cmd.favor.success.rank_update";
    public const string CMD_FAVOR_SUCCESS_RANK_UNCHANGED = "divineascension:cmd.favor.success.rank_unchanged";

    // Headers and labels
    public const string CMD_FAVOR_HEADER_INFO = "divineascension:cmd.favor.header.info";
    public const string CMD_FAVOR_HEADER_STATS = "divineascension:cmd.favor.header.stats";
    public const string CMD_FAVOR_HEADER_RANKS = "divineascension:cmd.favor.header.ranks";
    public const string CMD_FAVOR_LABEL_DOMAIN = "divineascension:cmd.favor.label.domain";
    public const string CMD_FAVOR_LABEL_CURRENT = "divineascension:cmd.favor.label.current";
    public const string CMD_FAVOR_LABEL_TOTAL_EARNED = "divineascension:cmd.favor.label.total_earned";
    public const string CMD_FAVOR_LABEL_CURRENT_RANK = "divineascension:cmd.favor.label.current_rank";
    public const string CMD_FAVOR_LABEL_DEVOTION_RANK = "divineascension:cmd.favor.label.devotion_rank";
    public const string CMD_FAVOR_LABEL_NEXT_RANK = "divineascension:cmd.favor.label.next_rank";
    public const string CMD_FAVOR_LABEL_FAVOR_NEEDED = "divineascension:cmd.favor.label.favor_needed";
    public const string CMD_FAVOR_LABEL_PROGRESS = "divineascension:cmd.favor.label.progress";
    public const string CMD_FAVOR_LABEL_MAX_RANK = "divineascension:cmd.favor.label.max_rank";
    public const string CMD_FAVOR_LABEL_UNLOCK_MESSAGE = "divineascension:cmd.favor.label.unlock_message";
    public const string CMD_FAVOR_FORMAT_RANK_REQUIREMENT = "divineascension:cmd.favor.format.rank_requirement";

    #endregion

    #region System Messages

    public const string SYSTEM_PLAYER_ENTITY_NULL = "divineascension:system.player_entity_null";
    public const string SYSTEM_PLAYER_NO_STATS = "divineascension:system.player_no_stats";
    public const string SYSTEM_STAT_NOT_FOUND = "divineascension:system.stat_not_found";
    public const string SYSTEM_APPLIED_MODIFIERS = "divineascension:system.applied_modifiers";
    public const string SYSTEM_REMOVED_MODIFIERS = "divineascension:system.removed_modifiers";
    public const string SYSTEM_REFRESHED_BLESSINGS = "divineascension:system.refreshed_blessings";

    #endregion

    #region Network Errors

    public const string ERROR_RELIGION_NOT_FOUND = "divineascension:error.religion_not_found";
    public const string ERROR_NOT_MEMBER = "divineascension:error.not_member";
    public const string ERROR_CIVILIZATION_NOT_FOUND = "divineascension:error.civilization_not_found";
    public const string ERROR_GENERIC = "divineascension:error.generic";

    #endregion

    #region Role Commands

    // Command descriptions
    public const string CMD_ROLES_DESC = "divineascension:cmd.roles.desc";
    public const string CMD_TRANSFER_DESC = "divineascension:cmd.transfer.desc";
    public const string CMD_ROLE_DESC = "divineascension:cmd.role.desc";
    public const string CMD_ROLE_MEMBERS_DESC = "divineascension:cmd.role.members.desc";
    public const string CMD_ROLE_CREATE_DESC = "divineascension:cmd.role.create.desc";
    public const string CMD_ROLE_DELETE_DESC = "divineascension:cmd.role.delete.desc";
    public const string CMD_ROLE_RENAME_DESC = "divineascension:cmd.role.rename.desc";
    public const string CMD_ROLE_ASSIGN_DESC = "divineascension:cmd.role.assign.desc";
    public const string CMD_ROLE_GRANT_DESC = "divineascension:cmd.role.grant.desc";
    public const string CMD_ROLE_REVOKE_DESC = "divineascension:cmd.role.revoke.desc";
    public const string CMD_ROLE_PERMISSIONS_DESC = "divineascension:cmd.role.permissions.desc";

    // Common errors (reusable across commands)
    public const string CMD_ERROR_PLAYERS_ONLY = "divineascension:cmd.error.players_only";
    public const string CMD_ERROR_NOT_IN_RELIGION = "divineascension:cmd.error.not_in_religion";
    public const string CMD_ERROR_RELIGION_DATA_NOT_FOUND = "divineascension:cmd.error.religion_data_not_found";

    // Role-specific errors
    public const string CMD_ROLE_ERROR_NO_VIEW_PERMISSION = "divineascension:cmd.role.error.no_view_permission";
    public const string CMD_ROLE_ERROR_ROLE_NOT_FOUND = "divineascension:cmd.role.error.role_not_found";
    public const string CMD_ROLE_ERROR_PLAYER_NOT_FOUND = "divineascension:cmd.role.error.player_not_found";
    public const string CMD_ROLE_ERROR_INVALID_PERMISSION = "divineascension:cmd.role.error.invalid_permission";

    // Success messages
    public const string CMD_ROLE_SUCCESS_CREATED = "divineascension:cmd.role.success.created";
    public const string CMD_ROLE_SUCCESS_DELETED = "divineascension:cmd.role.success.deleted";
    public const string CMD_ROLE_SUCCESS_RENAMED = "divineascension:cmd.role.success.renamed";
    public const string CMD_ROLE_SUCCESS_ASSIGNED = "divineascension:cmd.role.success.assigned";

    public const string CMD_ROLE_SUCCESS_ASSIGNED_NOTIFICATION =
        "divineascension:cmd.role.success.assigned_notification";

    public const string CMD_ROLE_SUCCESS_PERMISSION_GRANTED = "divineascension:cmd.role.success.permission_granted";
    public const string CMD_ROLE_SUCCESS_PERMISSION_REVOKED = "divineascension:cmd.role.success.permission_revoked";
    public const string CMD_ROLE_SUCCESS_FOUNDER_TRANSFERRED = "divineascension:cmd.role.success.founder_transferred";

    public const string CMD_ROLE_SUCCESS_FOUNDER_TRANSFERRED_NOTIFICATION =
        "divineascension:cmd.role.success.founder_transferred_notification";

    // Formatted output labels
    public const string CMD_ROLE_HEADER_ROLES = "divineascension:cmd.role.header.roles";
    public const string CMD_ROLE_LABEL_DEFAULT = "divineascension:cmd.role.label.default";
    public const string CMD_ROLE_LABEL_CUSTOM = "divineascension:cmd.role.label.custom";
    public const string CMD_ROLE_LABEL_PROTECTED = "divineascension:cmd.role.label.protected";
    public const string CMD_ROLE_LABEL_MEMBERS = "divineascension:cmd.role.label.members";
    public const string CMD_ROLE_LABEL_PERMISSIONS = "divineascension:cmd.role.label.permissions";
    public const string CMD_ROLE_FOOTER_VIEW_PERMISSIONS = "divineascension:cmd.role.footer.view_permissions";
    public const string CMD_ROLE_HEADER_MEMBERS_WITH_ROLE = "divineascension:cmd.role.header.members_with_role";
    public const string CMD_ROLE_NO_MEMBERS = "divineascension:cmd.role.no_members";
    public const string CMD_ROLE_FORMAT_MEMBER_INFO = "divineascension:cmd.role.format.member_info";
    public const string CMD_ROLE_HEADER_PERMISSIONS = "divineascension:cmd.role.header.permissions";
    public const string CMD_ROLE_NO_PERMISSIONS = "divineascension:cmd.role.no_permissions";
    public const string CMD_ROLE_LABEL_AVAILABLE_PERMISSIONS = "divineascension:cmd.role.label.available_permissions";

    #endregion

    #region Civilization Commands

    // Command descriptions
    public const string CMD_CIV_DESC = "divineascension:cmd.civ.desc";
    public const string CMD_CIV_CREATE_DESC = "divineascension:cmd.civ.create.desc";
    public const string CMD_CIV_INVITE_DESC = "divineascension:cmd.civ.invite.desc";
    public const string CMD_CIV_ACCEPT_DESC = "divineascension:cmd.civ.accept.desc";
    public const string CMD_CIV_DECLINE_DESC = "divineascension:cmd.civ.decline.desc";
    public const string CMD_CIV_LEAVE_DESC = "divineascension:cmd.civ.leave.desc";
    public const string CMD_CIV_KICK_DESC = "divineascension:cmd.civ.kick.desc";
    public const string CMD_CIV_DISBAND_DESC = "divineascension:cmd.civ.disband.desc";
    public const string CMD_CIV_DESCRIPTION_DESC = "divineascension:cmd.civ.description.desc";
    public const string CMD_CIV_LIST_DESC = "divineascension:cmd.civ.list.desc";
    public const string CMD_CIV_INFO_DESC = "divineascension:cmd.civ.info.desc";
    public const string CMD_CIV_INVITES_DESC = "divineascension:cmd.civ.invites.desc";
    public const string CMD_CIV_ADMIN_DESC = "divineascension:cmd.civ.admin.desc";
    public const string CMD_CIV_ADMIN_CREATE_DESC = "divineascension:cmd.civ.admin.create.desc";
    public const string CMD_CIV_ADMIN_DISSOLVE_DESC = "divineascension:cmd.civ.admin.dissolve.desc";
    public const string CMD_CIV_ADMIN_CLEANUP_DESC = "divineascension:cmd.civ.admin.cleanup.desc";

    // Common errors (additional ones specific to civilization)
    public const string CMD_ERROR_MUST_BE_IN_RELIGION = "divineascension:cmd.error.must_be_in_religion";

    public const string CMD_ERROR_MUST_BE_IN_RELIGION_TO_CREATE =
        "divineascension:cmd.error.must_be_in_religion_to_create";

    public const string CMD_ERROR_MUST_BE_IN_RELIGION_FOR_INVITES =
        "divineascension:cmd.error.must_be_in_religion_for_invites";

    public const string CMD_ERROR_RELIGION_NOT_FOUND_GENERIC = "divineascension:cmd.error.religion_not_found_generic";

    // Civilization-specific errors
    public const string CMD_CIV_ERROR_ONLY_FOUNDERS_CREATE = "divineascension:cmd.civ.error.only_founders_create";
    public const string CMD_CIV_ERROR_CREATE_FAILED = "divineascension:cmd.civ.error.create_failed";
    public const string CMD_CIV_ERROR_NAME_PROFANITY = "divineascension:cmd.civ.error.name_profanity";
    public const string CMD_CIV_ERROR_NOT_IN_CIV_USE_CREATE = "divineascension:cmd.civ.error.not_in_civ_use_create";
    public const string CMD_CIV_ERROR_ONLY_FOUNDER_INVITE = "divineascension:cmd.civ.error.only_founder_invite";
    public const string CMD_CIV_ERROR_RELIGION_NOT_FOUND = "divineascension:cmd.civ.error.religion_not_found";
    public const string CMD_CIV_ERROR_INVITE_FAILED = "divineascension:cmd.civ.error.invite_failed";
    public const string CMD_CIV_ERROR_ONLY_FOUNDERS_ACCEPT = "divineascension:cmd.civ.error.only_founders_accept";
    public const string CMD_CIV_ERROR_ACCEPT_FAILED = "divineascension:cmd.civ.error.accept_failed";
    public const string CMD_CIV_ERROR_ONLY_FOUNDERS_DECLINE = "divineascension:cmd.civ.error.only_founders_decline";
    public const string CMD_CIV_ERROR_DECLINE_FAILED = "divineascension:cmd.civ.error.decline_failed";
    public const string CMD_CIV_ERROR_ONLY_FOUNDERS_LEAVE = "divineascension:cmd.civ.error.only_founders_leave";
    public const string CMD_CIV_ERROR_NOT_IN_CIV = "divineascension:cmd.civ.error.not_in_civ";
    public const string CMD_CIV_ERROR_FOUNDER_CANNOT_LEAVE = "divineascension:cmd.civ.error.founder_cannot_leave";
    public const string CMD_CIV_ERROR_LEAVE_FAILED = "divineascension:cmd.civ.error.leave_failed";
    public const string CMD_CIV_ERROR_ONLY_FOUNDER_KICK = "divineascension:cmd.civ.error.only_founder_kick";
    public const string CMD_CIV_ERROR_KICK_FAILED = "divineascension:cmd.civ.error.kick_failed";
    public const string CMD_CIV_ERROR_ONLY_FOUNDER_DISBAND = "divineascension:cmd.civ.error.only_founder_disband";
    public const string CMD_CIV_ERROR_DISBAND_FAILED = "divineascension:cmd.civ.error.disband_failed";

    public const string CMD_CIV_ERROR_ONLY_FOUNDER_DESCRIPTION =
        "divineascension:cmd.civ.error.only_founder_description";

    public const string CMD_CIV_ERROR_CIV_NOT_FOUND = "divineascension:cmd.civ.error.civ_not_found";
    public const string CMD_CIV_ERROR_NOT_IN_CIV_SPECIFY_NAME = "divineascension:cmd.civ.error.not_in_civ_specify_name";

    public const string CMD_CIV_ERROR_MUST_BE_IN_RELIGION_SPECIFY_NAME =
        "divineascension:cmd.civ.error.must_be_in_religion_specify_name";

    public const string CMD_CIV_ERROR_ONLY_FOUNDERS_VIEW_INVITES =
        "divineascension:cmd.civ.error.only_founders_view_invites";

    public const string CMD_CIV_ERROR_NO_ORPHANED = "divineascension:cmd.civ.error.no_orphaned";
    public const string CMD_CIV_ERROR_ADMIN_CREATE_FAILED = "divineascension:cmd.civ.error.admin_create_failed";
    public const string CMD_CIV_ERROR_RELIGION_ALREADY_IN_CIV = "divineascension:cmd.civ.error.religion_already_in_civ";
    public const string CMD_CIV_ERROR_DUPLICATE_DEITIES = "divineascension:cmd.civ.error.duplicate_deities";

    // Success messages
    public const string CMD_CIV_SUCCESS_CREATED = "divineascension:cmd.civ.success.created";
    public const string CMD_CIV_SUCCESS_INVITE_SENT = "divineascension:cmd.civ.success.invite_sent";
    public const string CMD_CIV_SUCCESS_JOINED = "divineascension:cmd.civ.success.joined";
    public const string CMD_CIV_SUCCESS_DECLINED = "divineascension:cmd.civ.success.declined";
    public const string CMD_CIV_SUCCESS_LEFT = "divineascension:cmd.civ.success.left";
    public const string CMD_CIV_SUCCESS_KICKED = "divineascension:cmd.civ.success.kicked";
    public const string CMD_CIV_SUCCESS_DISBANDED = "divineascension:cmd.civ.success.disbanded";
    public const string CMD_CIV_SUCCESS_NO_CIVS = "divineascension:cmd.civ.success.no_civs";
    public const string CMD_CIV_SUCCESS_NO_INVITES = "divineascension:cmd.civ.success.no_invites";
    public const string CMD_CIV_SUCCESS_CLEANUP = "divineascension:cmd.civ.success.cleanup";

    public const string CMD_CIV_SUCCESS_RELIGION_JOINED_NOTIFICATION =
        "divineascension:cmd.civ.success.religion_joined_notification";

    public const string CMD_CIV_SUCCESS_ADMIN_CREATED = "divineascension:cmd.civ.success.admin_created";

    public const string CMD_CIV_SUCCESS_ADMIN_DISBANDED_NOTIFICATION =
        "divineascension:cmd.civ.success.admin_disbanded_notification";

    public const string CMD_CIV_SUCCESS_ADMIN_DISBANDED = "divineascension:cmd.civ.success.admin_disbanded";

    // Formatted output labels
    public const string CMD_CIV_HEADER_LIST = "divineascension:cmd.civ.header.list";
    public const string CMD_CIV_FORMAT_LIST_ITEM = "divineascension:cmd.civ.format.list_item";
    public const string CMD_CIV_LABEL_DEITIES = "divineascension:cmd.civ.label.deities";
    public const string CMD_CIV_LABEL_RELIGIONS = "divineascension:cmd.civ.label.religions";
    public const string CMD_CIV_HEADER_INFO = "divineascension:cmd.civ.header.info";
    public const string CMD_CIV_LABEL_FOUNDED = "divineascension:cmd.civ.label.founded";
    public const string CMD_CIV_LABEL_MEMBERS = "divineascension:cmd.civ.label.members";
    public const string CMD_CIV_LABEL_MEMBER_RELIGIONS = "divineascension:cmd.civ.label.member_religions";
    public const string CMD_CIV_FORMAT_MEMBER_RELIGION = "divineascension:cmd.civ.format.member_religion";
    public const string CMD_CIV_LABEL_FOUNDER = "divineascension:cmd.civ.label.founder";
    public const string CMD_CIV_LABEL_PENDING_INVITES = "divineascension:cmd.civ.label.pending_invites";
    public const string CMD_CIV_FORMAT_PENDING_INVITE = "divineascension:cmd.civ.format.pending_invite";
    public const string CMD_CIV_HEADER_INVITES = "divineascension:cmd.civ.header.invites";
    public const string CMD_CIV_FORMAT_INVITE_ITEM = "divineascension:cmd.civ.format.invite_item";
    public const string CMD_CIV_LABEL_INVITE_ID = "divineascension:cmd.civ.label.invite_id";
    public const string CMD_CIV_LABEL_USE_ACCEPT = "divineascension:cmd.civ.label.use_accept";

    #endregion

    #region Religion Commands

    // Command descriptions
    public const string CMD_RELIGION_DESC = "divineascension:cmd.religion.desc";
    public const string CMD_RELIGION_CREATE_DESC = "divineascension:cmd.religion.create.desc";
    public const string CMD_RELIGION_JOIN_DESC = "divineascension:cmd.religion.join.desc";
    public const string CMD_RELIGION_LEAVE_DESC = "divineascension:cmd.religion.leave.desc";
    public const string CMD_RELIGION_LIST_DESC = "divineascension:cmd.religion.list.desc";
    public const string CMD_RELIGION_INFO_DESC = "divineascension:cmd.religion.info.desc";
    public const string CMD_RELIGION_MEMBERS_DESC = "divineascension:cmd.religion.members.desc";
    public const string CMD_RELIGION_INVITE_DESC = "divineascension:cmd.religion.invite.desc";
    public const string CMD_RELIGION_KICK_DESC = "divineascension:cmd.religion.kick.desc";
    public const string CMD_RELIGION_BAN_DESC = "divineascension:cmd.religion.ban.desc";
    public const string CMD_RELIGION_UNBAN_DESC = "divineascension:cmd.religion.unban.desc";
    public const string CMD_RELIGION_BANLIST_DESC = "divineascension:cmd.religion.banlist.desc";
    public const string CMD_RELIGION_DISBAND_DESC = "divineascension:cmd.religion.disband.desc";
    public const string CMD_RELIGION_SETDESC_DESC = "divineascension:cmd.religion.setdesc.desc";
    public const string CMD_RELIGION_PRESTIGE_DESC = "divineascension:cmd.religion.prestige.desc";
    public const string CMD_RELIGION_PRESTIGE_INFO_DESC = "divineascension:cmd.religion.prestige.info.desc";
    public const string CMD_RELIGION_ADMIN_DESC = "divineascension:cmd.religion.admin.desc";
    public const string CMD_RELIGION_ADMIN_REPAIR_DESC = "divineascension:cmd.religion.admin.repair.desc";
    public const string CMD_RELIGION_ADMIN_FORCEJOIN_DESC = "divineascension:cmd.religion.admin.forcejoin.desc";
    public const string CMD_RELIGION_ADMIN_FORCEREMOVE_DESC = "divineascension:cmd.religion.admin.forceremove.desc";
    public const string CMD_RELIGION_ADMIN_ADDPRESTIGE_DESC = "divineascension:cmd.religion.admin.addprestige.desc";
    public const string CMD_RELIGION_ADMIN_SETPRESTIGE_DESC = "divineascension:cmd.religion.admin.setprestige.desc";

    // Error messages
    public const string CMD_RELIGION_ERROR_ALREADY_IN_RELIGION =
        "divineascension:cmd.religion.error.already_in_religion";

    public const string CMD_RELIGION_ERROR_INVALID_DEITY = "divineascension:cmd.religion.error.invalid_deity";
    public const string CMD_RELIGION_ERROR_NAME_EXISTS = "divineascension:cmd.religion.error.name_exists";
    public const string CMD_RELIGION_ERROR_NAME_PROFANITY = "divineascension:cmd.religion.error.name_profanity";

    public const string CMD_RELIGION_ERROR_DEITY_NAME_PROFANITY =
        "divineascension:cmd.religion.error.deity_name_profanity";

    public const string CMD_RELIGION_ERROR_DESC_PROFANITY = "divineascension:cmd.religion.error.desc_profanity";
    public const string CMD_RELIGION_ERROR_NOT_FOUND = "divineascension:cmd.religion.error.not_found";
    public const string CMD_RELIGION_ERROR_PRIVATE_NO_INVITE = "divineascension:cmd.religion.error.private_no_invite";

    public const string CMD_RELIGION_ERROR_CANNOT_LEAVE_FOUNDER =
        "divineascension:cmd.religion.error.cannot_leave_founder";

    public const string CMD_RELIGION_ERROR_NO_RELIGIONS = "divineascension:cmd.religion.error.no_religions";
    public const string CMD_RELIGION_ERROR_SPECIFY_NAME = "divineascension:cmd.religion.error.specify_name";
    public const string CMD_RELIGION_ERROR_NO_PERMISSION = "divineascension:cmd.religion.error.no_permission";
    public const string CMD_RELIGION_ERROR_PLAYER_NOT_FOUND = "divineascension:cmd.religion.error.player_not_found";

    public const string CMD_RELIGION_ERROR_PLAYER_NOT_IN_RELIGION =
        "divineascension:cmd.religion.error.player_not_in_religion";

    public const string CMD_RELIGION_ERROR_CANNOT_KICK_SELF = "divineascension:cmd.religion.error.cannot_kick_self";
    public const string CMD_RELIGION_ERROR_CANNOT_BAN_SELF = "divineascension:cmd.religion.error.cannot_ban_self";
    public const string CMD_RELIGION_ERROR_ALREADY_BANNED = "divineascension:cmd.religion.error.already_banned";
    public const string CMD_RELIGION_ERROR_NOT_BANNED = "divineascension:cmd.religion.error.not_banned";
    public const string CMD_RELIGION_ERROR_NO_BANNED_PLAYERS = "divineascension:cmd.religion.error.no_banned_players";

    public const string CMD_RELIGION_ERROR_CANNOT_DISBAND_NOT_FOUNDER =
        "divineascension:cmd.religion.error.cannot_disband_not_founder";

    public const string CMD_RELIGION_ERROR_DESC_TOO_LONG = "divineascension:cmd.religion.error.desc_too_long";
    public const string CMD_RELIGION_ERROR_INVALID_AMOUNT = "divineascension:cmd.religion.error.invalid_amount";
    public const string CMD_RELIGION_ERROR_NO_INCONSISTENCIES = "divineascension:cmd.religion.error.no_inconsistencies";

    // Success messages
    public const string CMD_RELIGION_SUCCESS_CREATED = "divineascension:cmd.religion.success.created";
    public const string CMD_RELIGION_SUCCESS_JOINED = "divineascension:cmd.religion.success.joined";
    public const string CMD_RELIGION_SUCCESS_LEFT = "divineascension:cmd.religion.success.left";
    public const string CMD_RELIGION_SUCCESS_INVITED = "divineascension:cmd.religion.success.invited";

    public const string CMD_RELIGION_SUCCESS_INVITE_NOTIFICATION =
        "divineascension:cmd.religion.success.invite_notification";

    public const string CMD_RELIGION_SUCCESS_KICKED = "divineascension:cmd.religion.success.kicked";

    public const string CMD_RELIGION_SUCCESS_KICK_NOTIFICATION =
        "divineascension:cmd.religion.success.kick_notification";

    public const string CMD_RELIGION_SUCCESS_BANNED = "divineascension:cmd.religion.success.banned";
    public const string CMD_RELIGION_SUCCESS_BAN_NOTIFICATION = "divineascension:cmd.religion.success.ban_notification";
    public const string CMD_RELIGION_SUCCESS_UNBANNED = "divineascension:cmd.religion.success.unbanned";
    public const string CMD_RELIGION_SUCCESS_DISBANDED = "divineascension:cmd.religion.success.disbanded";

    public const string CMD_RELIGION_SUCCESS_DISBANDED_NOTIFICATION =
        "divineascension:cmd.religion.success.disbanded_notification";

    public const string CMD_RELIGION_SUCCESS_DESC_SET = "divineascension:cmd.religion.success.desc_set";
    public const string CMD_RELIGION_SUCCESS_PRESTIGE_ADDED = "divineascension:cmd.religion.success.prestige_added";
    public const string CMD_RELIGION_SUCCESS_PRESTIGE_SET = "divineascension:cmd.religion.success.prestige_set";
    public const string CMD_RELIGION_SUCCESS_REPAIR_COMPLETE = "divineascension:cmd.religion.success.repair_complete";
    public const string CMD_RELIGION_SUCCESS_FORCE_JOINED = "divineascension:cmd.religion.success.force_joined";
    public const string CMD_RELIGION_SUCCESS_FORCE_REMOVED = "divineascension:cmd.religion.success.force_removed";

    // Formatted output
    public const string CMD_RELIGION_HEADER_LIST = "divineascension:cmd.religion.header.list";
    public const string CMD_RELIGION_FORMAT_LIST_ITEM = "divineascension:cmd.religion.format.list_item";
    public const string CMD_RELIGION_LABEL_VISIBILITY = "divineascension:cmd.religion.label.visibility";
    public const string CMD_RELIGION_HEADER_INFO = "divineascension:cmd.religion.header.info";
    public const string CMD_RELIGION_LABEL_DEITY = "divineascension:cmd.religion.label.deity";
    public const string CMD_RELIGION_LABEL_FOUNDER = "divineascension:cmd.religion.label.founder";
    public const string CMD_RELIGION_LABEL_MEMBERS_COUNT = "divineascension:cmd.religion.label.members_count";
    public const string CMD_RELIGION_LABEL_PRESTIGE = "divineascension:cmd.religion.label.prestige";
    public const string CMD_RELIGION_LABEL_PRESTIGE_RANK = "divineascension:cmd.religion.label.prestige_rank";
    public const string CMD_RELIGION_LABEL_DESCRIPTION = "divineascension:cmd.religion.label.description";
    public const string CMD_RELIGION_HEADER_MEMBERS = "divineascension:cmd.religion.header.members";
    public const string CMD_RELIGION_FORMAT_MEMBER = "divineascension:cmd.religion.format.member";
    public const string CMD_RELIGION_HEADER_BANNED = "divineascension:cmd.religion.header.banned";
    public const string CMD_RELIGION_HEADER_PRESTIGE_INFO = "divineascension:cmd.religion.header.prestige_info";
    public const string CMD_RELIGION_LABEL_CURRENT_PRESTIGE = "divineascension:cmd.religion.label.current_prestige";
    public const string CMD_RELIGION_LABEL_NEXT_RANK = "divineascension:cmd.religion.label.next_rank";
    public const string CMD_RELIGION_LABEL_PRESTIGE_NEEDED = "divineascension:cmd.religion.label.prestige_needed";
    public const string CMD_RELIGION_LABEL_MAX_RANK_ACHIEVED = "divineascension:cmd.religion.label.max_rank_achieved";

    // Additional missing keys
    public const string CMD_RELIGION_DESCRIPTION_DESC = "divineascension:cmd.religion.description.desc";
    public const string CMD_RELIGION_SETDEITYNAME_DESC = "divineascension:cmd.religion.setdeityname.desc";
    public const string CMD_RELIGION_SETDEITYNAME_SUCCESS = "divineascension:cmd.religion.setdeityname.success";

    public const string CMD_RELIGION_SETDEITYNAME_ERROR_TOO_SHORT =
        "divineascension:cmd.religion.setdeityname.error.too_short";

    public const string CMD_RELIGION_SETDEITYNAME_ERROR_TOO_LONG =
        "divineascension:cmd.religion.setdeityname.error.too_long";

    public const string CMD_RELIGION_SETDEITYNAME_ERROR_INVALID_CHARS =
        "divineascension:cmd.religion.setdeityname.error.invalid_chars";

    public const string CMD_RELIGION_PRESTIGE_ADD_DESC = "divineascension:cmd.religion.prestige.add.desc";
    public const string CMD_RELIGION_PRESTIGE_SET_DESC = "divineascension:cmd.religion.prestige.set.desc";
    public const string CMD_RELIGION_ADMIN_JOIN_DESC = "divineascension:cmd.religion.admin.join.desc";
    public const string CMD_RELIGION_ADMIN_LEAVE_DESC = "divineascension:cmd.religion.admin.leave.desc";
    public const string CMD_RELIGION_ADMIN_SETDEITYNAME_DESC = "divineascension:cmd.religion.admin.setdeityname.desc";

    public const string CMD_RELIGION_ADMIN_SETDEITYNAME_SUCCESS =
        "divineascension:cmd.religion.admin.setdeityname.success";

    /// <summary>
    ///     Migration notification sent to founders when deity name is auto-generated
    /// </summary>
    public const string MIGRATION_DEITY_NAME_NOTICE = "divineascension:migration.deity_name_notice";

    public const string CMD_RELIGION_ERROR_NO_RELIGION = "divineascension:cmd.religion.error.no_religion";

    public const string CMD_RELIGION_ERROR_FOUNDER_CANNOT_LEAVE =
        "divineascension:cmd.religion.error.founder_cannot_leave";

    public const string CMD_RELIGION_ERROR_INVALID_DEITY_FILTER =
        "divineascension:cmd.religion.error.invalid_deity_filter";

    public const string CMD_RELIGION_INFO_NO_RELIGIONS = "divineascension:cmd.religion.info.no_religions";
    public const string CMD_RELIGION_FORMAT_VISIBILITY_PUBLIC = "divineascension:cmd.religion.format.visibility_public";

    public const string CMD_RELIGION_FORMAT_VISIBILITY_PRIVATE =
        "divineascension:cmd.religion.format.visibility_private";

    public const string CMD_RELIGION_FORMAT_LIST_ENTRY = "divineascension:cmd.religion.format.list_entry";

    public const string CMD_RELIGION_ERROR_NO_RELIGION_SPECIFY =
        "divineascension:cmd.religion.error.no_religion_specify";

    public const string CMD_RELIGION_ERROR_DATA_NOT_FOUND = "divineascension:cmd.religion.error.data_not_found";
    public const string CMD_RELIGION_FORMAT_DEITY = "divineascension:cmd.religion.format.deity";
    public const string CMD_RELIGION_FORMAT_VISIBILITY = "divineascension:cmd.religion.format.visibility";
    public const string CMD_RELIGION_FORMAT_MEMBERS = "divineascension:cmd.religion.format.members";
    public const string CMD_RELIGION_FORMAT_PRESTIGE_RANK = "divineascension:cmd.religion.format.prestige_rank";
    public const string CMD_RELIGION_FORMAT_PRESTIGE = "divineascension:cmd.religion.format.prestige";
    public const string CMD_RELIGION_FORMAT_CREATED = "divineascension:cmd.religion.format.created";
    public const string CMD_RELIGION_FORMAT_FOUNDER = "divineascension:cmd.religion.format.founder";
    public const string CMD_RELIGION_FORMAT_DESCRIPTION = "divineascension:cmd.religion.format.description";
    public const string CMD_RELIGION_FORMAT_ROLE_FOUNDER = "divineascension:cmd.religion.format.role_founder";
    public const string CMD_RELIGION_FORMAT_ROLE_MEMBER = "divineascension:cmd.religion.format.role_member";

    public const string CMD_RELIGION_ERROR_NO_PERMISSION_INVITE =
        "divineascension:cmd.religion.error.no_permission_invite";

    public const string CMD_RELIGION_ERROR_PLAYER_NOT_FOUND_ONLINE =
        "divineascension:cmd.religion.error.player_not_found_online";

    public const string CMD_RELIGION_ERROR_ALREADY_MEMBER = "divineascension:cmd.religion.error.already_member";
    public const string CMD_RELIGION_ERROR_INVITE_FAILED = "divineascension:cmd.religion.error.invite_failed";
    public const string CMD_RELIGION_NOTIFICATION_INVITED = "divineascension:cmd.religion.notification.invited";
    public const string CMD_RELIGION_SUCCESS_INVITE_SENT = "divineascension:cmd.religion.success.invite_sent";
    public const string CMD_RELIGION_ERROR_NO_PERMISSION_KICK = "divineascension:cmd.religion.error.no_permission_kick";
    public const string CMD_RELIGION_ERROR_NOT_MEMBER = "divineascension:cmd.religion.error.not_member";
    public const string CMD_RELIGION_NOTIFICATION_KICKED = "divineascension:cmd.religion.notification.kicked";
    public const string CMD_RELIGION_ERROR_NO_PERMISSION_BAN = "divineascension:cmd.religion.error.no_permission_ban";
    public const string CMD_RELIGION_FORMAT_NO_REASON = "divineascension:cmd.religion.format.no_reason";
    public const string CMD_RELIGION_NOTIFICATION_BANNED = "divineascension:cmd.religion.notification.banned";
    public const string CMD_RELIGION_FORMAT_BAN_TEMPORARY = "divineascension:cmd.religion.format.ban_temporary";
    public const string CMD_RELIGION_FORMAT_BAN_PERMANENT = "divineascension:cmd.religion.format.ban_permanent";

    public const string CMD_RELIGION_ERROR_NO_PERMISSION_UNBAN =
        "divineascension:cmd.religion.error.no_permission_unban";

    public const string CMD_RELIGION_ERROR_NO_PERMISSION_VIEW_BANLIST =
        "divineascension:cmd.religion.error.no_permission_view_banlist";

    public const string CMD_RELIGION_INFO_NO_BANNED_PLAYERS = "divineascension:cmd.religion.info.no_banned_players";
    public const string CMD_RELIGION_HEADER_BANLIST = "divineascension:cmd.religion.header.banlist";
    public const string CMD_RELIGION_FORMAT_BAN_EXPIRES = "divineascension:cmd.religion.format.ban_expires";
    public const string CMD_RELIGION_FORMAT_BAN_ENTRY_HEADER = "divineascension:cmd.religion.format.ban_entry_header";
    public const string CMD_RELIGION_FORMAT_BAN_ENTRY_REASON = "divineascension:cmd.religion.format.ban_entry_reason";

    public const string CMD_RELIGION_FORMAT_BAN_ENTRY_BANNEDBY =
        "divineascension:cmd.religion.format.ban_entry_bannedby";

    public const string CMD_RELIGION_FORMAT_BAN_ENTRY_STATUS = "divineascension:cmd.religion.format.ban_entry_status";

    public const string CMD_RELIGION_ERROR_NO_PERMISSION_DISBAND =
        "divineascension:cmd.religion.error.no_permission_disband";

    public const string CMD_RELIGION_NOTIFICATION_DISBANDED_BY_FOUNDER =
        "divineascension:cmd.religion.notification.disbanded_by_founder";

    public const string CMD_RELIGION_NOTIFICATION_DISBANDED = "divineascension:cmd.religion.notification.disbanded";

    public const string CMD_RELIGION_ERROR_NO_PERMISSION_EDIT_DESC =
        "divineascension:cmd.religion.error.no_permission_edit_desc";

    public const string CMD_RELIGION_SUCCESS_DESCRIPTION_SET = "divineascension:cmd.religion.success.description_set";
    public const string CMD_RELIGION_HEADER_PRESTIGE = "divineascension:cmd.religion.header.prestige";

    public const string CMD_RELIGION_FORMAT_PRESTIGE_CURRENT_RANK =
        "divineascension:cmd.religion.format.prestige_current_rank";

    public const string CMD_RELIGION_FORMAT_PRESTIGE_CURRENT = "divineascension:cmd.religion.format.prestige_current";
    public const string CMD_RELIGION_FORMAT_PRESTIGE_TOTAL = "divineascension:cmd.religion.format.prestige_total";

    public const string CMD_RELIGION_FORMAT_PRESTIGE_NEXT_RANK =
        "divineascension:cmd.religion.format.prestige_next_rank";

    public const string CMD_RELIGION_FORMAT_PRESTIGE_PROGRESS = "divineascension:cmd.religion.format.prestige_progress";

    public const string CMD_RELIGION_FORMAT_PRESTIGE_REMAINING =
        "divineascension:cmd.religion.format.prestige_remaining";

    public const string CMD_RELIGION_INFO_MAX_RANK = "divineascension:cmd.religion.info.max_rank";
    public const string CMD_RELIGION_ERROR_AMOUNT_POSITIVE = "divineascension:cmd.religion.error.amount_positive";
    public const string CMD_RELIGION_FORMAT_ADMIN_COMMAND = "divineascension:cmd.religion.format.admin_command";

    public const string CMD_RELIGION_FORMAT_PRESTIGE_RANK_CHANGE =
        "divineascension:cmd.religion.format.prestige_rank_change";

    public const string CMD_RELIGION_ERROR_AMOUNT_NON_NEGATIVE =
        "divineascension:cmd.religion.error.amount_non_negative";

    public const string CMD_RELIGION_ERROR_INTERNAL = "divineascension:cmd.religion.error.internal";
    public const string CMD_RELIGION_INFO_REPAIR_NO_ISSUES = "divineascension:cmd.religion.info.repair_no_issues";

    public const string CMD_RELIGION_NOTIFICATION_MEMBERSHIP_REPAIRED =
        "divineascension:cmd.religion.notification.membership_repaired";

    public const string CMD_RELIGION_SUCCESS_REPAIR_PLAYER = "divineascension:cmd.religion.success.repair_player";
    public const string CMD_RELIGION_ERROR_REPAIR_FAILED = "divineascension:cmd.religion.error.repair_failed";
    public const string CMD_RELIGION_HEADER_REPAIR_SCAN = "divineascension:cmd.religion.header.repair_scan";
    public const string CMD_RELIGION_FORMAT_REPAIR_SUCCESS = "divineascension:cmd.religion.format.repair_success";
    public const string CMD_RELIGION_FORMAT_REPAIR_ISSUE = "divineascension:cmd.religion.format.repair_issue";

    public const string CMD_RELIGION_FORMAT_REPAIR_FAILED_ENTRY =
        "divineascension:cmd.religion.format.repair_failed_entry";

    public const string CMD_RELIGION_HEADER_REPAIR_SUMMARY = "divineascension:cmd.religion.header.repair_summary";
    public const string CMD_RELIGION_FORMAT_REPAIR_SCANNED = "divineascension:cmd.religion.format.repair_scanned";
    public const string CMD_RELIGION_FORMAT_REPAIR_CONSISTENT = "divineascension:cmd.religion.format.repair_consistent";
    public const string CMD_RELIGION_FORMAT_REPAIR_REPAIRED = "divineascension:cmd.religion.format.repair_repaired";

    public const string CMD_RELIGION_FORMAT_REPAIR_FAILED_COUNT =
        "divineascension:cmd.religion.format.repair_failed_count";

    public const string CMD_RELIGION_INFO_ALL_CONSISTENT = "divineascension:cmd.religion.info.all_consistent";

    public const string CMD_RELIGION_INFO_ALREADY_MEMBER_ADMIN =
        "divineascension:cmd.religion.info.already_member_admin";

    public const string CMD_RELIGION_NOTIFICATION_ADMIN_ADDED = "divineascension:cmd.religion.notification.admin_added";
    public const string CMD_RELIGION_SUCCESS_ADMIN_ADDED = "divineascension:cmd.religion.success.admin_added";
    public const string CMD_RELIGION_ERROR_PLAYER_NO_RELIGION = "divineascension:cmd.religion.error.player_no_religion";

    public const string CMD_RELIGION_ERROR_TRANSFER_FOUNDER_FAILED =
        "divineascension:cmd.religion.error.transfer_founder_failed";

    public const string CMD_RELIGION_NOTIFICATION_ADMIN_REMOVED =
        "divineascension:cmd.religion.notification.admin_removed";

    public const string CMD_RELIGION_SUCCESS_ADMIN_LEFT_FOUNDER_TRANSFER =
        "divineascension:cmd.religion.success.admin_left_founder_transfer";

    public const string CMD_RELIGION_NOTIFICATION_ADMIN_DISBANDED =
        "divineascension:cmd.religion.notification.admin_disbanded";

    public const string CMD_RELIGION_SUCCESS_ADMIN_LEFT_DISBANDED =
        "divineascension:cmd.religion.success.admin_left_disbanded";

    public const string CMD_RELIGION_SUCCESS_ADMIN_LEFT = "divineascension:cmd.religion.success.admin_left";

    #endregion

    #region Network Messages - Blessings

    public const string NET_BLESSING_NOT_FOUND = "divineascension:net.blessing.not_found";

    public const string NET_BLESSING_MUST_BE_IN_RELIGION_PLAYER =
        "divineascension:net.blessing.must_be_in_religion_player";

    public const string NET_BLESSING_SUCCESS_UNLOCKED = "divineascension:net.blessing.success_unlocked";
    public const string NET_BLESSING_FAILED_TO_UNLOCK = "divineascension:net.blessing.failed_to_unlock";

    public const string NET_BLESSING_MUST_BE_IN_RELIGION_RELIGION =
        "divineascension:net.blessing.must_be_in_religion_religion";

    public const string NET_BLESSING_ONLY_FOUNDER_CAN_UNLOCK = "divineascension:net.blessing.only_founder_can_unlock";

    public const string NET_BLESSING_SUCCESS_UNLOCKED_FOR_RELIGION =
        "divineascension:net.blessing.success_unlocked_for_religion";

    public const string NET_BLESSING_UNLOCKED_NOTIFICATION = "divineascension:net.blessing.unlocked_notification";
    public const string NET_BLESSING_ERROR_UNLOCKING = "divineascension:net.blessing.error_unlocking";

    #endregion

    #region Network Messages - Diplomacy

    public const string NET_DIPLOMACY_MUST_BE_IN_RELIGION = "divineascension:net.diplomacy.must_be_in_religion";

    public const string NET_DIPLOMACY_RELIGION_NO_LONGER_EXISTS =
        "divineascension:net.diplomacy.religion_no_longer_exists";

    public const string NET_DIPLOMACY_NOT_PART_OF_CIVILIZATION =
        "divineascension:net.diplomacy.not_part_of_civilization";

    public const string NET_DIPLOMACY_UNKNOWN_ACTION = "divineascension:net.diplomacy.unknown_action";
    public const string NET_DIPLOMACY_TARGET_CIV_REQUIRED = "divineascension:net.diplomacy.target_civ_required";

    public const string NET_DIPLOMACY_PROPOSED_STATUS_REQUIRED =
        "divineascension:net.diplomacy.proposed_status_required";

    public const string NET_DIPLOMACY_INVALID_STATUS = "divineascension:net.diplomacy.invalid_status";
    public const string NET_DIPLOMACY_PROPOSAL_RECEIVED = "divineascension:net.diplomacy.proposal_received";
    public const string NET_DIPLOMACY_PROPOSAL_ID_REQUIRED = "divineascension:net.diplomacy.proposal_id_required";
    public const string NET_DIPLOMACY_PROPOSAL_ACCEPTED = "divineascension:net.diplomacy.proposal_accepted";
    public const string NET_DIPLOMACY_TREATY_BREAK_SCHEDULED = "divineascension:net.diplomacy.treaty_break_scheduled";
    public const string NET_DIPLOMACY_TREATY_BREAK_CANCELED = "divineascension:net.diplomacy.treaty_break_canceled";
    public const string NET_DIPLOMACY_PEACE_DECLARED = "divineascension:net.diplomacy.peace_declared";
    public const string NET_DIPLOMACY_PREFIX = "divineascension:net.diplomacy.prefix";

    // Religion Network Handler
    public const string NET_RELIGION_NAME_EMPTY = "divineascension:net.religion.name_empty";
    public const string NET_RELIGION_NAME_TOO_SHORT = "divineascension:net.religion.name_too_short";
    public const string NET_RELIGION_NAME_TOO_LONG = "divineascension:net.religion.name_too_long";
    public const string NET_RELIGION_NAME_EXISTS = "divineascension:net.religion.name_exists";
    public const string NET_RELIGION_NAME_PROFANITY = "divineascension:net.religion.name_profanity";
    public const string NET_RELIGION_DEITY_NAME_PROFANITY = "divineascension:net.religion.deity_name_profanity";
    public const string NET_RELIGION_DESC_PROFANITY = "divineascension:net.religion.desc_profanity";
    public const string NET_RELIGION_ALREADY_IN_RELIGION = "divineascension:net.religion.already_in_religion";
    public const string NET_RELIGION_INVALID_DEITY = "divineascension:net.religion.invalid_deity";
    public const string NET_RELIGION_CREATED = "divineascension:net.religion.created";
    public const string NET_RELIGION_CREATE_ERROR = "divineascension:net.religion.create_error";
    public const string NET_RELIGION_NOT_FOUND = "divineascension:net.religion.not_found";
    public const string NET_RELIGION_ONLY_FOUNDER_EDIT = "divineascension:net.religion.only_founder_edit";
    public const string NET_RELIGION_DESC_TOO_LONG = "divineascension:net.religion.desc_too_long";
    public const string NET_RELIGION_DESC_UPDATED = "divineascension:net.religion.desc_updated";
    public const string NET_RELIGION_DESC_ERROR = "divineascension:net.religion.desc_error";
    public const string NET_RELIGION_NOT_MEMBER = "divineascension:net.religion.not_member";
    public const string NET_RELIGION_UNKNOWN_ACTION = "divineascension:net.religion.unknown_action";
    public const string NET_RELIGION_ERROR = "divineascension:net.religion.error";
    public const string NET_RELIGION_INVALID_REQUEST = "divineascension:net.religion.invalid_request";
    public const string NET_RELIGION_ROLE_CHANGED = "divineascension:net.religion.role_changed";
    public const string NET_RELIGION_FOUNDER_TRANSFERRED = "divineascension:net.religion.founder_transferred";
    public const string NET_RELIGION_FOUNDER_TRANSFER_SUCCESS = "divineascension:net.religion.founder_transfer_success";
    public const string NET_RELIGION_BANNED_PERMANENT = "divineascension:net.religion.banned_permanent";
    public const string NET_RELIGION_BANNED_EXPIRES = "divineascension:net.religion.banned_expires";
    public const string NET_RELIGION_BANNED_WITH_REASON = "divineascension:net.religion.banned_with_reason";
    public const string NET_RELIGION_BANNED_GENERIC = "divineascension:net.religion.banned_generic";
    public const string NET_RELIGION_JOINED = "divineascension:net.religion.joined";
    public const string NET_RELIGION_CANNOT_JOIN = "divineascension:net.religion.cannot_join";
    public const string NET_RELIGION_JOIN_ERROR = "divineascension:net.religion.join_error";
    public const string NET_RELIGION_YOU_JOINED = "divineascension:net.religion.you_joined";
    public const string NET_RELIGION_INVITE_ACCEPTED = "divineascension:net.religion.invite_accepted";
    public const string NET_RELIGION_INVITE_FAILED = "divineascension:net.religion.invite_failed";
    public const string NET_RELIGION_INVITE_DECLINED = "divineascension:net.religion.invite_declined";
    public const string NET_RELIGION_INVITE_DECLINE_FAILED = "divineascension:net.religion.invite_decline_failed";
    public const string NET_RELIGION_NOT_IN_RELIGION = "divineascension:net.religion.not_in_religion";
    public const string NET_RELIGION_FOUNDER_CANNOT_LEAVE = "divineascension:net.religion.founder_cannot_leave";
    public const string NET_RELIGION_YOU_LEFT = "divineascension:net.religion.you_left";
    public const string NET_RELIGION_LEFT = "divineascension:net.religion.left";
    public const string NET_RELIGION_NO_KICK_PERMISSION = "divineascension:net.religion.no_kick_permission";
    public const string NET_RELIGION_CANNOT_KICK_SELF = "divineascension:net.religion.cannot_kick_self";
    public const string NET_RELIGION_KICKED_NOTIFICATION = "divineascension:net.religion.kicked_notification";
    public const string NET_RELIGION_KICKED = "divineascension:net.religion.kicked";
    public const string NET_RELIGION_NO_BAN_PERMISSION = "divineascension:net.religion.no_ban_permission";
    public const string NET_RELIGION_CANNOT_BAN_SELF = "divineascension:net.religion.cannot_ban_self";
    public const string NET_RELIGION_NO_REASON = "divineascension:net.religion.no_reason";
    public const string NET_RELIGION_BANNED_NOTIFICATION = "divineascension:net.religion.banned_notification";
    public const string NET_RELIGION_PLAYER_BANNED = "divineascension:net.religion.player_banned";
    public const string NET_RELIGION_ONLY_FOUNDER_UNBAN = "divineascension:net.religion.only_founder_unban";
    public const string NET_RELIGION_UNBANNED = "divineascension:net.religion.unbanned";
    public const string NET_RELIGION_UNBAN_FAILED = "divineascension:net.religion.unban_failed";
    public const string NET_RELIGION_PLAYER_NOT_FOUND = "divineascension:net.religion.player_not_found";
    public const string NET_RELIGION_INVITED_NOTIFICATION = "divineascension:net.religion.invited_notification";
    public const string NET_RELIGION_INVITE_SENT = "divineascension:net.religion.invite_sent";
    public const string NET_RELIGION_INVITE_SEND_FAILED = "divineascension:net.religion.invite_send_failed";
    public const string NET_RELIGION_ONLY_FOUNDER_KICK = "divineascension:net.religion.only_founder_kick";
    public const string NET_RELIGION_DISBANDED = "divineascension:net.religion.disbanded";
    public const string NET_RELIGION_DEFAULT_ROLE = "divineascension:net.religion.default_role";

    // Civilization Network Handler
    public const string NET_CIV_UNKNOWN_RELIGION = "divineascension:net.civ.unknown_religion";
    public const string NET_CIV_MUST_BE_IN_RELIGION = "divineascension:net.civ.must_be_in_religion";
    public const string NET_CIV_CREATED = "divineascension:net.civ.created";
    public const string NET_CIV_CREATE_FAILED = "divineascension:net.civ.create_failed";
    public const string NET_CIV_NAME_PROFANITY = "divineascension:net.civ.name_profanity";
    public const string NET_CIV_RELIGION_NOT_FOUND = "divineascension:net.civ.religion_not_found";
    public const string NET_CIV_INVITE_SENT = "divineascension:net.civ.invite_sent";
    public const string NET_CIV_INVITE_FAILED = "divineascension:net.civ.invite_failed";
    public const string NET_CIV_INVITED_NOTIFICATION = "divineascension:net.civ.invited_notification";
    public const string NET_CIV_JOINED = "divineascension:net.civ.joined";
    public const string NET_CIV_JOIN_FAILED = "divineascension:net.civ.join_failed";
    public const string NET_CIV_INVITE_DECLINED = "divineascension:net.civ.invite_declined";
    public const string NET_CIV_INVITE_DECLINE_FAILED = "divineascension:net.civ.invite_decline_failed";
    public const string NET_CIV_NOT_IN_RELIGION = "divineascension:net.civ.not_in_religion";
    public const string NET_CIV_RELIGION_NOT_FOUND_PLAYER = "divineascension:net.civ.religion_not_found_player";
    public const string NET_CIV_ONLY_FOUNDER_LEAVE = "divineascension:net.civ.only_founder_leave";
    public const string NET_CIV_FOUNDER_MUST_DISBAND = "divineascension:net.civ.founder_must_disband";
    public const string NET_CIV_LEFT = "divineascension:net.civ.left";
    public const string NET_CIV_LEAVE_FAILED = "divineascension:net.civ.leave_failed";
    public const string NET_CIV_KICKED = "divineascension:net.civ.kicked";
    public const string NET_CIV_KICK_FAILED = "divineascension:net.civ.kick_failed";
    public const string NET_CIV_DISBANDED = "divineascension:net.civ.disbanded";
    public const string NET_CIV_DISBAND_FAILED = "divineascension:net.civ.disband_failed";
    public const string NET_CIV_ICON_UPDATED = "divineascension:net.civ.icon_updated";
    public const string NET_CIV_ICON_UPDATE_FAILED = "divineascension:net.civ.icon_update_failed";
    public const string NET_CIV_DESCRIPTION_UPDATED = "divineascension:net.civ.description_updated";
    public const string NET_CIV_DESCRIPTION_UPDATE_FAILED = "divineascension:net.civ.description_update_failed";
    public const string NET_CIV_DESCRIPTION_TOO_LONG = "divineascension:net.civ.description_too_long";
    public const string NET_CIV_DESCRIPTION_PROFANITY = "divineascension:net.civ.description_profanity";
    public const string NET_CIV_UNKNOWN_ACTION = "divineascension:net.civ.unknown_action";
    public const string NET_CIV_ERROR = "divineascension:net.civ.error";

    // Holy Site Commands
    public const string CMD_HOLYSITE_DESC = "divineascension:cmd.holysite.desc";
    public const string CMD_HOLYSITE_CONSECRATE_DESC = "divineascension:cmd.holysite.consecrate.desc";
    public const string CMD_HOLYSITE_DECONSECRATE_DESC = "divineascension:cmd.holysite.deconsecrate.desc";
    public const string CMD_HOLYSITE_INFO_DESC = "divineascension:cmd.holysite.info.desc";
    public const string CMD_HOLYSITE_LIST_DESC = "divineascension:cmd.holysite.list.desc";
    public const string CMD_HOLYSITE_NEARBY_DESC = "divineascension:cmd.holysite.nearby.desc";

    // Holy Site Messages
    public const string HOLYSITE_CONSECRATED = "divineascension:holysite.consecrated";
    public const string HOLYSITE_DECONSECRATED = "divineascension:holysite.deconsecrated";
    public const string HOLYSITE_NOT_MEMBER = "divineascension:holysite.not_member";
    public const string HOLYSITE_NO_PERMISSION = "divineascension:holysite.no_permission";
    public const string HOLYSITE_NOT_CLAIMED = "divineascension:holysite.not_claimed";
    public const string HOLYSITE_LIMIT_REACHED = "divineascension:holysite.limit_reached";
    public const string HOLYSITE_NOT_FOUND = "divineascension:holysite.not_found";
    public const string HOLYSITE_INFO_HEADER = "divineascension:holysite.info.header";
    public const string HOLYSITE_INFO_TIER = "divineascension:holysite.info.tier";
    public const string HOLYSITE_INFO_SIZE = "divineascension:holysite.info.size";
    public const string HOLYSITE_INFO_BONUSES = "divineascension:holysite.info.bonuses";
    public const string HOLYSITE_INFO_FOUNDER = "divineascension:holysite.info.founder";
    public const string HOLYSITE_INFO_CREATED = "divineascension:holysite.info.created";
    public const string HOLYSITE_LIST_HEADER = "divineascension:holysite.list.header";
    public const string HOLYSITE_LIST_ENTRY = "divineascension:holysite.list.entry";
    public const string HOLYSITE_LIST_EMPTY = "divineascension:holysite.list.empty";
    public const string HOLYSITE_NEARBY_HEADER = "divineascension:holysite.nearby.header";
    public const string HOLYSITE_NEARBY_ENTRY = "divineascension:holysite.nearby.entry";
    public const string HOLYSITE_NEARBY_EMPTY = "divineascension:holysite.nearby.empty";
    public const string HOLYSITE_NOT_IN_SITE = "divineascension:holysite.not_in_site";

    // UI - Browse
    public const string UI_HOLYSITES_BROWSE_TITLE = "divineascension:ui.holysites.browse.title";
    public const string UI_HOLYSITES_BROWSE_REFRESH = "divineascension:ui.holysites.browse.refresh";
    public const string UI_HOLYSITES_BROWSE_LOADING = "divineascension:ui.holysites.browse.loading";
    public const string UI_HOLYSITES_BROWSE_NO_SITES = "divineascension:ui.holysites.browse.nosites";
    public const string UI_HOLYSITES_TABLE_NAME = "divineascension:ui.holysites.table.name";
    public const string UI_HOLYSITES_TABLE_TIER = "divineascension:ui.holysites.table.tier";
    public const string UI_HOLYSITES_TABLE_VOLUME = "divineascension:ui.holysites.table.volume";
    public const string UI_HOLYSITES_TABLE_PRAYER = "divineascension:ui.holysites.table.prayer";

    // UI - Detail
    public const string UI_HOLYSITES_DETAIL_LOADING = "divineascension:ui.holysites.detail.loading";
    public const string UI_HOLYSITES_DETAIL_BACK = "divineascension:ui.holysites.detail.back";
    public const string UI_HOLYSITES_DETAIL_MARK = "divineascension:ui.holysites.detail.mark";
    public const string UI_HOLYSITES_DETAIL_NAME = "divineascension:ui.holysites.detail.name";
    public const string UI_HOLYSITES_DETAIL_COORDINATES = "divineascension:ui.holysites.detail.coordinates";
    public const string UI_HOLYSITES_DETAIL_DESCRIPTION = "divineascension:ui.holysites.detail.description";
    public const string UI_HOLYSITES_DETAIL_NO_DESCRIPTION = "divineascension:ui.holysites.detail.nodescription";

    // Messages
    public const string HOLYSITE_NAME_EMPTY = "divineascension:holysite.name.empty";
    public const string HOLYSITE_NAME_TOO_LONG = "divineascension:holysite.name.toolong";
    public const string HOLYSITE_NAME_PROFANITY = "divineascension:holysite.name.profanity";
    public const string HOLYSITE_NAME_EXISTS = "divineascension:holysite.name.exists";
    public const string HOLYSITE_RENAMED = "divineascension:holysite.renamed";
    public const string HOLYSITE_DESC_TOO_LONG = "divineascension:holysite.desc.toolong";
    public const string HOLYSITE_DESC_PROFANITY = "divineascension:holysite.desc.profanity";
    public const string HOLYSITE_DESC_UPDATED = "divineascension:holysite.desc.updated";
    public const string HOLYSITE_WAYPOINT_ADDED = "divineascension:holysite.waypoint.added";

    // Ritual Keys
    public const string RITUAL_STARTED = "divineascension:ritual.started";
    public const string RITUAL_CONTRIBUTION = "divineascension:ritual.contribution";
    public const string RITUAL_COMPLETED = "divineascension:ritual.completed";
    public const string RITUAL_CANCELLED = "divineascension:ritual.cancelled";
    public const string RITUAL_PROGRESS = "divineascension:ritual.progress";
    public const string RITUAL_REQUIREMENT_MET = "divineascension:ritual.requirement_met";
    public const string RITUAL_NOT_CONSECRATOR = "divineascension:ritual.not_consecrator";
    public const string RITUAL_ALREADY_ACTIVE = "divineascension:ritual.already_active";
    public const string RITUAL_INVALID_TIER = "divineascension:ritual.invalid_tier";
    public const string RITUAL_MAX_TIER = "divineascension:ritual.max_tier";

    // Errors
    public const string ERROR_HOLYSITE_NOT_FOUND = "divineascension:error.holysite.not_found";
    public const string ERROR_PERMISSION_DENIED = "divineascension:error.permission_denied";
    public const string ERROR_UPDATE_FAILED = "divineascension:error.update_failed";
    public const string ERROR_INTERNAL = "divineascension:error.internal";

    #endregion

    #region Prayer

    // Error messages
    public const string PRAYER_ALTAR_NOT_CONSECRATED = "divineascension:prayer.altar.not_consecrated";
    public const string PRAYER_NO_RELIGION = "divineascension:prayer.no_religion";
    public const string PRAYER_WRONG_RELIGION = "divineascension:prayer.wrong_religion";
    public const string PRAYER_COOLDOWN = "divineascension:prayer.cooldown";
    public const string PRAYER_OFFERING_TIER_REJECTED = "divineascension:prayer.offering.tier_rejected";

    // Success messages
    public const string PRAYER_SUCCESS_WITH_OFFERING = "divineascension:prayer.success.with_offering";
    public const string PRAYER_SUCCESS_OFFERING_REJECTED = "divineascension:prayer.success.offering_rejected";
    public const string PRAYER_SUCCESS_NO_OFFERING = "divineascension:prayer.success.no_offering";

    #endregion
}