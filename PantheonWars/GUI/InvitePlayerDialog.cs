using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using PantheonWars.Network;
using Vintagestory.API.Client;

namespace PantheonWars.GUI;

/// <summary>
///     Dialog for inviting a player to the religion
/// </summary>
[ExcludeFromCodeCoverage]
[Obsolete("This dialog has been migrated to ReligionMyReligionRenderer with inline invite functionality in the BlessingDialog tab system. Use BlessingDialog's Religion tab instead.")]
public class InvitePlayerDialog : GuiDialog
{
    private readonly ICoreClientAPI _capi;
    private readonly IClientNetworkChannel _channel;
    private readonly string _religionUID;
    private string _playerName = "";

    public InvitePlayerDialog(ICoreClientAPI capi, IClientNetworkChannel channel, string religionUID) : base(capi)
    {
        _capi = capi;
        _channel = channel;
        _religionUID = religionUID;
    }

    public override string ToggleKeyCombinationCode => null!;

    public override bool PrefersUngrabbedMouse => true;

    public override void OnGuiOpened()
    {
        base.OnGuiOpened();
        ComposeDialog();
    }

    private void ComposeDialog()
    {
        const int titleBarHeight = 30;
        const double contentWidth = 400;
        const double contentHeight = 150;

        var bgBounds = ElementBounds.Fixed(0, titleBarHeight, contentWidth, contentHeight);

        var dialogBounds = ElementStdBounds.AutosizedMainDialog
            .WithAlignment(EnumDialogArea.CenterMiddle);

        SingleComposer?.Dispose();

        SingleComposer = capi.Gui
            .CreateCompo("inviteplayer", dialogBounds)
            .AddShadedDialogBG(ElementBounds.Fill)
            .AddDialogTitleBar("Invite Player to Religion", OnTitleBarCloseClicked)
            .BeginChildElements(bgBounds);

        var composer = SingleComposer;

        double yPos = 10;

        // Player Name
        var nameLabel = ElementBounds.Fixed(10, yPos, contentWidth - 20, 25);
        composer.AddStaticText("Player Name:", CairoFont.WhiteSmallText(), nameLabel);
        yPos += 30;

        var nameInput = ElementBounds.Fixed(10, yPos, contentWidth - 20, 30);
        composer.AddTextInput(nameInput, OnPlayerNameChanged, CairoFont.WhiteDetailText(), "playerNameInput");
        yPos += 45;

        // Description text
        var descBounds = ElementBounds.Fixed(10, yPos, contentWidth - 20, 30);
        composer.AddStaticText(
            "Enter the exact player name to invite them to your religion.",
            CairoFont.WhiteSmallText().WithFontSize(10),
            descBounds
        );
        yPos += 40;

        // Buttons
        var cancelBounds = ElementBounds.Fixed(10, yPos, 120, 30);
        composer.AddSmallButton("Cancel", OnCancelClicked, cancelBounds);

        var inviteBounds = ElementBounds.Fixed(contentWidth - 130, yPos, 120, 30);
        composer.AddSmallButton("Invite", OnInviteClicked, inviteBounds, EnumButtonStyle.Normal, "inviteButton");

        composer.EndChildElements().Compose();
    }

    private void OnPlayerNameChanged(string name)
    {
        _playerName = name;
    }

    private bool OnInviteClicked()
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(_playerName))
        {
            _capi.ShowChatMessage("Please enter a player name.");
            return true;
        }

        // Find player by name
        var targetPlayer = _capi.World.AllOnlinePlayers.FirstOrDefault(p =>
            p.PlayerName.Equals(_playerName, StringComparison.OrdinalIgnoreCase));

        if (targetPlayer == null)
        {
            _capi.ShowChatMessage($"Player '{_playerName}' not found or not online.");
            return true;
        }

        // Send invite request
        _channel.SendPacket(new ReligionActionRequestPacket("invite", _religionUID, targetPlayer.PlayerUID));
        _capi.ShowChatMessage($"Invitation sent to {targetPlayer.PlayerName}.");

        // Close dialog
        TryClose();
        return true;
    }

    private bool OnCancelClicked()
    {
        TryClose();
        return true;
    }

    private void OnTitleBarCloseClicked()
    {
        TryClose();
    }
}