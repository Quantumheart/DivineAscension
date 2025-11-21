using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Cairo;
using PantheonWars.Models.Enum;
using PantheonWars.Network;
using Vintagestory.API.Client;

namespace PantheonWars.GUI;

/// <summary>
///     Dialog for managing religions - browsing, creating, joining, and managing members (Phase 3.5)
/// </summary>
[ExcludeFromCodeCoverage]
public class ReligionManagementDialog : GuiDialog
{
    private readonly ICoreClientAPI _capi;
    private readonly IClientNetworkChannel _channel;
    private readonly CreateReligionDialog _createDialog;
    private List<ReligionListResponsePacket.ReligionInfo> _availableReligions = new();
    private string _currentTab = "browse"; // "browse" or "my_religion"
    private bool _dataLoaded;
    private EditDescriptionDialog? _editDescriptionDialog;
    private InvitePlayerDialog? _inviteDialog;
    private BanPlayerDialog? _banDialog;
    private PlayerReligionInfoResponsePacket? _playerReligionInfo;
    private DeityType _selectedDeityFilter = DeityType.None; // For browse tab filtering

    public ReligionManagementDialog(ICoreClientAPI capi, IClientNetworkChannel channel) : base(capi)
    {
        _capi = capi;
        _channel = channel;
        _createDialog = new CreateReligionDialog(capi, channel);
    }

    public override string ToggleKeyCombinationCode => "pantheonwarsreligion";

    public override void OnGuiOpened()
    {
        base.OnGuiOpened();
        RefreshData();
        ComposeDialog();
    }

    /// <summary>
    ///     Refresh religion data from server
    /// </summary>
    private void RefreshData()
    {
        _dataLoaded = false;

        // Request list of available religions
        var deityFilter = _selectedDeityFilter == DeityType.None ? "" : _selectedDeityFilter.ToString();
        _channel.SendPacket(new ReligionListRequestPacket(deityFilter));

        // Request player's current religion info
        _channel.SendPacket(new PlayerReligionInfoRequestPacket());
    }

    /// <summary>
    ///     Handle religion list response from server
    /// </summary>
    public void OnReligionListResponse(ReligionListResponsePacket packet)
    {
        _availableReligions = packet.Religions;
        CheckDataLoadedAndRefresh();
    }

    /// <summary>
    ///     Handle player religion info response from server
    /// </summary>
    public void OnPlayerReligionInfoResponse(PlayerReligionInfoResponsePacket packet)
    {
        _playerReligionInfo = packet;
        CheckDataLoadedAndRefresh();
    }

    /// <summary>
    ///     Handle action response from server
    /// </summary>
    public void OnActionResponse(ReligionActionResponsePacket packet)
    {
        if (packet.Success)
        {
            _capi.ShowChatMessage(packet.Message);
            // Refresh data after successful action
            RefreshData();
        }
        else
        {
            _capi.ShowChatMessage($"Error: {packet.Message}");
        }
    }

    private void CheckDataLoadedAndRefresh()
    {
        if (!_dataLoaded && _playerReligionInfo != null)
        {
            _dataLoaded = true;
            if (IsOpened()) ComposeDialog();
        }
    }

    private void ComposeDialog()
    {
        const int titleBarHeight = 30;
        const double contentWidth = 700;
        const double contentHeight = 650;

        // Main background bounds
        var bgBounds = ElementBounds.Fixed(0, titleBarHeight, contentWidth, contentHeight);

        // Auto-sized dialog at center of screen
        var dialogBounds = ElementStdBounds.AutosizedMainDialog
            .WithAlignment(EnumDialogArea.CenterMiddle);

        SingleComposer?.Dispose();

        SingleComposer = capi.Gui
            .CreateCompo("religionmanagement", dialogBounds)
            .AddShadedDialogBG(ElementBounds.Fill)
            .AddDialogTitleBar("Religion Management", OnTitleBarCloseClicked)
            .BeginChildElements(bgBounds);

        var composer = SingleComposer;

        // Tab buttons
        var browseTabBounds = ElementBounds.Fixed(10, 10, 150, 30);
        var myReligionTabBounds = ElementBounds.Fixed(170, 10, 150, 30);
        var createButtonBounds = ElementBounds.Fixed(contentWidth - 160, 10, 150, 30);

        composer.AddSmallButton("Browse Religions", OnBrowseTabClicked, browseTabBounds,
            _currentTab == "browse" ? EnumButtonStyle.MainMenu : EnumButtonStyle.Normal, "browseTab");
        composer.AddSmallButton("My Religion", OnMyReligionTabClicked, myReligionTabBounds,
            _currentTab == "my_religion" ? EnumButtonStyle.MainMenu : EnumButtonStyle.Normal, "myReligionTab");
        composer.AddSmallButton("Create New", OnCreateReligionClicked, createButtonBounds, EnumButtonStyle.Normal,
            "createButton");

        // Tab content area
        var tabContentBounds = ElementBounds.Fixed(10, 50, contentWidth - 20, contentHeight - 60);

        if (_currentTab == "browse")
            ComposeBrowseTab(composer, tabContentBounds);
        else if (_currentTab == "my_religion") ComposeMyReligionTab(composer, tabContentBounds);

        composer.EndChildElements().Compose();
    }

    #region Browse Religions Tab

    private void ComposeBrowseTab(GuiComposer composer, ElementBounds bounds)
    {
        // Filter dropdown
        var filterLabelBounds = ElementBounds.Fixed(bounds.fixedX, bounds.fixedY, 100, 25);
        var filterDropdownBounds = ElementBounds.Fixed(bounds.fixedX + 110, bounds.fixedY, 200, 25);

        composer.AddStaticText("Filter by Deity:", CairoFont.WhiteSmallText(), filterLabelBounds);

        var deityNames = new[] { "All Deities" }
            .Concat(Enum.GetValues(typeof(DeityType))
                .Cast<DeityType>()
                .Where(d => d != DeityType.None)
                .Select(d => d.ToString()))
            .ToArray();

        composer.AddDropDown(deityNames, deityNames, 0, OnDeityFilterChanged, filterDropdownBounds, "deityFilter");

        // Religion list area (scrollable)
        var listBounds =
            ElementBounds.Fixed(bounds.fixedX, bounds.fixedY + 40, bounds.fixedWidth, bounds.fixedHeight - 40);
        var clipBounds = listBounds.ForkBoundingParent();
        var insetBounds = listBounds.FlatCopy();

        composer.BeginClip(clipBounds);
        composer.AddInset(insetBounds, 2);

        // Filter religions by selected deity
        var filteredReligions = _selectedDeityFilter == DeityType.None
            ? _availableReligions
            : _availableReligions.Where(r => r.Deity == _selectedDeityFilter.ToString()).ToList();

        if (filteredReligions.Count == 0)
        {
            var noReligionsTextBounds = ElementBounds.Fixed(listBounds.fixedX + 10, listBounds.fixedY + 10,
                listBounds.fixedWidth - 20, 30);
            composer.AddStaticText("No religions available. Create one!", CairoFont.WhiteSmallText(),
                noReligionsTextBounds);
        }
        else
        {
            var yPos = listBounds.fixedY + 10;
            foreach (var religion in filteredReligions)
            {
                AddReligionListItem(composer, religion, listBounds.fixedX + 10, yPos, listBounds.fixedWidth - 20);
                yPos += 70; // Height of each religion item
            }
        }

        composer.EndClip();
    }

    private void AddReligionListItem(GuiComposer composer, ReligionListResponsePacket.ReligionInfo religion, double x,
        double y, double width)
    {
        var containerBounds = ElementBounds.Fixed(x, y, width, 60);
        composer.AddInset(containerBounds, 2);

        // Religion name and deity
        var nameBounds = ElementBounds.Fixed(x + 10, y + 5, width - 130, 20);
        composer.AddStaticText(
            $"{religion.ReligionName} ({religion.Deity})",
            CairoFont.WhiteDetailText().WithWeight(FontWeight.Bold),
            nameBounds
        );

        // Member count and prestige
        var infoBounds = ElementBounds.Fixed(x + 10, y + 28, width - 130, 15);
        composer.AddStaticText(
            $"Members: {religion.MemberCount} | Prestige: {religion.Prestige} ({religion.PrestigeRank})",
            CairoFont.WhiteSmallText(),
            infoBounds
        );

        // Public/Private indicator
        var visibilityBounds = ElementBounds.Fixed(x + 10, y + 45, width - 130, 12);
        composer.AddStaticText(
            religion.IsPublic ? "[Public]" : "[Private - Invite Only]",
            CairoFont.WhiteSmallText().WithFontSize(10),
            visibilityBounds
        );

        // Join button
        var joinButtonBounds = ElementBounds.Fixed(x + width - 110, y + 15, 100, 25);
        composer.AddSmallButton("Join", () => OnJoinReligionClicked(religion.ReligionUID), joinButtonBounds);
    }

    private void OnDeityFilterChanged(string code, bool selected)
    {
        if (code == "All Deities")
            _selectedDeityFilter = DeityType.None;
        else
            Enum.TryParse(code, out _selectedDeityFilter);
        ComposeDialog();
    }

    private bool OnJoinReligionClicked(string religionUID)
    {
        // Send join request to server
        _channel.SendPacket(new ReligionActionRequestPacket("join", religionUID));
        return true;
    }

    #endregion

    #region My Religion Tab

    private void ComposeMyReligionTab(GuiComposer composer, ElementBounds bounds)
    {
        if (_playerReligionInfo == null || !_playerReligionInfo.HasReligion)
        {
            var noReligionBounds =
                ElementBounds.Fixed(bounds.fixedX + 10, bounds.fixedY + 10, bounds.fixedWidth - 20, 30);
            composer.AddStaticText("You are not in a religion. Browse or create one!", CairoFont.WhiteSmallText(),
                noReligionBounds);
            return;
        }

        var religion = _playerReligionInfo;
        var yPos = bounds.fixedY + 10;

        // Religion name and deity (header)
        var headerBounds = ElementBounds.Fixed(bounds.fixedX + 10, yPos, bounds.fixedWidth - 20, 30);
        composer.AddStaticText(
            $"{religion.ReligionName} - {religion.Deity}",
            CairoFont.WhiteDetailText().WithWeight(FontWeight.Bold).WithFontSize(22),
            headerBounds
        );
        yPos += 40;

        // Religion info
        var infoBounds = ElementBounds.Fixed(bounds.fixedX + 10, yPos, bounds.fixedWidth - 20, 80);
        var prestigeRankEnum = Enum.TryParse<PrestigeRank>(religion.PrestigeRank, out var rank)
            ? rank
            : PrestigeRank.Fledgling;
        composer.AddStaticText(
            $"Founder: {religion.FounderUID}\n" +
            $"Members: {religion.Members.Count}\n" +
            $"Prestige: {religion.Prestige} / Next Rank at: {GetNextPrestigeThreshold(prestigeRankEnum)}\n" +
            $"Prestige Rank: {religion.PrestigeRank}",
            CairoFont.WhiteSmallText(),
            infoBounds
        );
        yPos += 90;

        // Description
        var descLabelBounds = ElementBounds.Fixed(bounds.fixedX + 10, yPos, bounds.fixedWidth - 20, 20);
        composer.AddStaticText("Description:", CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold), descLabelBounds);
        yPos += 25;

        var descBounds = ElementBounds.Fixed(bounds.fixedX + 10, yPos, bounds.fixedWidth - 20, 40);
        composer.AddStaticText(
            string.IsNullOrEmpty(religion.Description) ? "[No description set]" : religion.Description,
            CairoFont.WhiteSmallText(),
            descBounds
        );
        yPos += 50;

        // Member list (scrollable)
        var memberListLabelBounds = ElementBounds.Fixed(bounds.fixedX + 10, yPos, bounds.fixedWidth - 20, 20);
        composer.AddStaticText("Members:", CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold),
            memberListLabelBounds);
        yPos += 25;

        var memberListBounds = ElementBounds.Fixed(bounds.fixedX + 10, yPos, bounds.fixedWidth - 20, 150);
        var memberClipBounds = memberListBounds.ForkBoundingParent();

        composer.BeginClip(memberClipBounds);
        composer.AddInset(memberListBounds, 2);

        var memberYPos = memberListBounds.fixedY + 5;
        foreach (var member in religion.Members)
        {
            AddMemberListItem(composer, member, memberListBounds.fixedX + 10, memberYPos,
                memberListBounds.fixedWidth - 20);
            memberYPos += 30;
        }

        composer.EndClip();
        yPos += 160;

        // Banned members list (only for founder)
        if (religion.IsFounder)
        {
            var bannedListLabelBounds = ElementBounds.Fixed(bounds.fixedX + 10, yPos, bounds.fixedWidth - 20, 20);
            composer.AddStaticText("Banned Members:", CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold),
                bannedListLabelBounds);
            yPos += 25;

            var bannedListBounds = ElementBounds.Fixed(bounds.fixedX + 10, yPos, bounds.fixedWidth - 20, 100);
            var bannedClipBounds = bannedListBounds.ForkBoundingParent();

            composer.BeginClip(bannedClipBounds);
            composer.AddInset(bannedListBounds, 2);

            // Get banned players from religion manager
            var bannedPlayers = _playerReligionInfo.BannedPlayers ?? new List<PlayerReligionInfoResponsePacket.BanInfo>();

            if (bannedPlayers.Count == 0)
            {
                var noBansTextBounds = ElementBounds.Fixed(bannedListBounds.fixedX + 10, bannedListBounds.fixedY + 10,
                    bannedListBounds.fixedWidth - 20, 20);
                composer.AddStaticText("No banned players", CairoFont.WhiteSmallText(), noBansTextBounds);
            }
            else
            {
                var bannedYPos = bannedListBounds.fixedY + 5;
                foreach (var bannedPlayer in bannedPlayers)
                {
                    AddBannedPlayerListItem(composer, bannedPlayer, bannedListBounds.fixedX + 10, bannedYPos,
                        bannedListBounds.fixedWidth - 20);
                    bannedYPos += 30;
                }
            }

            composer.EndClip();
            yPos += 110;
        }

        // Action buttons (bottom)
        var buttonY = bounds.fixedY + bounds.fixedHeight - 40;
        var buttonX = bounds.fixedX + 10;

        if (religion.IsFounder)
        {
            var inviteButtonBounds = ElementBounds.Fixed(buttonX, buttonY, 120, 30);
            composer.AddSmallButton("Invite Player", OnInvitePlayerClicked, inviteButtonBounds);
            buttonX += 130;

            var editDescButtonBounds = ElementBounds.Fixed(buttonX, buttonY, 140, 30);
            composer.AddSmallButton("Edit Description", OnEditDescriptionClicked, editDescButtonBounds);
            buttonX += 150;

            var disbandButtonBounds = ElementBounds.Fixed(buttonX, buttonY, 120, 30);
            composer.AddSmallButton("Disband", OnDisbandReligionClicked, disbandButtonBounds);
        }

        // Leave button (always available)
        var leaveButtonBounds = ElementBounds.Fixed(bounds.fixedX + bounds.fixedWidth - 130, buttonY, 120, 30);
        composer.AddSmallButton("Leave Religion", OnLeaveReligionClicked, leaveButtonBounds);
    }

    private void AddMemberListItem(GuiComposer composer, PlayerReligionInfoResponsePacket.MemberInfo member, double x,
        double y, double width)
    {
        var roleText = member.IsFounder ? " [Founder]" : "";

        var nameBounds = ElementBounds.Fixed(x, y, width - 180, 20);
        composer.AddStaticText(
            $"{member.PlayerName}{roleText}",
            CairoFont.WhiteSmallText(),
            nameBounds
        );

        // Action buttons (only for founder, not for self or other founders)
        if (_playerReligionInfo != null && _playerReligionInfo.IsFounder && !member.IsFounder &&
            member.PlayerUID != _capi.World.Player.PlayerUID)
        {
            // Ban button (leftmost)
            var banButtonBounds = ElementBounds.Fixed(x + width - 170, y - 2, 75, 22);
            composer.AddSmallButton("Ban", () => OnBanMemberClicked(member.PlayerUID, member.PlayerName),
                banButtonBounds);

            // Kick button (rightmost)
            var kickButtonBounds = ElementBounds.Fixed(x + width - 90, y - 2, 80, 22);
            composer.AddSmallButton("Kick", () => OnKickMemberClicked(member.PlayerUID), kickButtonBounds);
        }
    }

    private int GetNextPrestigeThreshold(PrestigeRank currentRank)
    {
        return currentRank switch
        {
            PrestigeRank.Fledgling => 500,
            PrestigeRank.Established => 2000,
            PrestigeRank.Renowned => 5000,
            PrestigeRank.Legendary => 10000,
            PrestigeRank.Mythic => 999999,
            _ => 0
        };
    }

    #endregion

    #region Button Handlers

    private bool OnBrowseTabClicked()
    {
        _currentTab = "browse";
        ComposeDialog();
        return true;
    }

    private bool OnMyReligionTabClicked()
    {
        _currentTab = "my_religion";
        ComposeDialog();
        return true;
    }

    private bool OnCreateReligionClicked()
    {
        // Open create religion dialog
        _createDialog.TryOpen();
        return true;
    }

    private bool OnInvitePlayerClicked()
    {
        if (_playerReligionInfo != null && _playerReligionInfo.HasReligion)
        {
            // Dispose old dialog if it exists
            _inviteDialog?.Dispose();
            // Create and open invite dialog
            _inviteDialog = new InvitePlayerDialog(_capi, _channel, _playerReligionInfo.ReligionUID);
            _inviteDialog.TryOpen();
        }

        return true;
    }

    private bool OnEditDescriptionClicked()
    {
        if (_playerReligionInfo != null && _playerReligionInfo.HasReligion)
        {
            // Dispose old dialog if it exists
            _editDescriptionDialog?.Dispose();
            // Create and open edit description dialog
            _editDescriptionDialog = new EditDescriptionDialog(_capi, _channel, _playerReligionInfo.ReligionUID,
                _playerReligionInfo.Description);
            _editDescriptionDialog.TryOpen();
        }

        return true;
    }

    private bool OnKickMemberClicked(string memberUID)
    {
        // Send kick request to server
        _channel.SendPacket(new ReligionActionRequestPacket("kick", _playerReligionInfo?.ReligionUID ?? "", memberUID));
        RefreshData();
        return true;
    }

    private void AddBannedPlayerListItem(GuiComposer composer, PlayerReligionInfoResponsePacket.BanInfo bannedPlayer, double x,
        double y, double width)
    {
        // Display: "PlayerName - Reason - Banned: date - Expires: date/Never"
        var expiryText = bannedPlayer.IsPermanent ? "Never" : bannedPlayer.ExpiresAt;
        var displayText = $"{bannedPlayer.PlayerName} - {bannedPlayer.Reason}";

        var nameBounds = ElementBounds.Fixed(x, y, width - 100, 20);
        composer.AddStaticText(displayText, CairoFont.WhiteSmallText(), nameBounds);

        // Expiry info on separate line (smaller text)
        var infoBounds = ElementBounds.Fixed(x + 10, y + 12, width - 110, 15);
        composer.AddStaticText($"Banned: {bannedPlayer.BannedAt} | Expires: {expiryText}",
            CairoFont.WhiteSmallText().WithFontSize(9), infoBounds);

        // Unban button (only for founder)
        if (_playerReligionInfo != null && _playerReligionInfo.IsFounder)
        {
            var unbanButtonBounds = ElementBounds.Fixed(x + width - 90, y - 2, 80, 22);
            composer.AddSmallButton("Unban", () => OnUnbanPlayerClicked(bannedPlayer.PlayerUID), unbanButtonBounds);
        }
    }

    private bool OnUnbanPlayerClicked(string playerUID)
    {
        // Send unban request to server
        _channel.SendPacket(new ReligionActionRequestPacket("unban", _playerReligionInfo?.ReligionUID ?? "", playerUID));
        RefreshData();
        return true;
    }

    private bool OnBanMemberClicked(string memberUID, string memberName)
    {
        if (_playerReligionInfo != null && _playerReligionInfo.HasReligion)
        {
            // Dispose old dialog if it exists
            _banDialog?.Dispose();
            // Create and open ban player dialog
            _banDialog = new BanPlayerDialog(_capi, _channel, _playerReligionInfo.ReligionUID, memberUID, memberName);
            _banDialog.TryOpen();
        }

        return true;
    }

    private bool OnDisbandReligionClicked()
    {
        _channel.SendPacket(new ReligionActionRequestPacket("disband", _playerReligionInfo?.ReligionUID ?? ""));
        RefreshData();
        _currentTab = "browse";
        return true;
    }

    private bool OnLeaveReligionClicked()
    {
        // Send leave request to server
        _channel.SendPacket(new ReligionActionRequestPacket("leave", _playerReligionInfo?.ReligionUID ?? ""));
        RefreshData();
        _currentTab = "browse";
        return true;
    }

    private void OnTitleBarCloseClicked()
    {
        TryClose();
    }

    #endregion
}