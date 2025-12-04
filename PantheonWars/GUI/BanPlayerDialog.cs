using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Cairo;
using PantheonWars.Network;
using Vintagestory.API.Client;

namespace PantheonWars.GUI;

/// <summary>
///     Dialog for banning a player from the religion
/// </summary>
[ExcludeFromCodeCoverage]
[Obsolete("This dialog has been migrated to ReligionMyReligionRenderer with inline ban functionality in the BlessingDialog tab system. Use BlessingDialog's Religion tab instead.")]
public class BanPlayerDialog : GuiDialog
{
    private readonly ICoreClientAPI _capi;
    private readonly IClientNetworkChannel _channel;
    private readonly string _religionUID;
    private readonly string _targetPlayerUID;
    private readonly string _targetPlayerName;
    private string _reason = "";
    private string _expiryDays = "";

    public BanPlayerDialog(ICoreClientAPI capi, IClientNetworkChannel channel, string religionUID,
        string targetPlayerUID, string targetPlayerName) : base(capi)
    {
        _capi = capi;
        _channel = channel;
        _religionUID = religionUID;
        _targetPlayerUID = targetPlayerUID;
        _targetPlayerName = targetPlayerName;
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
        const double contentWidth = 500;
        const double contentHeight = 280;

        var bgBounds = ElementBounds.Fixed(0, titleBarHeight, contentWidth, contentHeight);

        var dialogBounds = ElementStdBounds.AutosizedMainDialog
            .WithAlignment(EnumDialogArea.CenterMiddle);

        SingleComposer?.Dispose();

        SingleComposer = capi.Gui
            .CreateCompo("banplayer", dialogBounds)
            .AddShadedDialogBG(ElementBounds.Fill)
            .AddDialogTitleBar("Ban Player from Religion", OnTitleBarCloseClicked)
            .BeginChildElements(bgBounds);

        var composer = SingleComposer;

        double yPos = 10;

        // Player info
        var playerInfoBounds = ElementBounds.Fixed(10, yPos, contentWidth - 20, 25);
        composer.AddStaticText($"Banning: {_targetPlayerName}", CairoFont.WhiteDetailText().WithWeight(FontWeight.Bold),
            playerInfoBounds);
        yPos += 35;

        // Reason label
        var reasonLabelBounds = ElementBounds.Fixed(10, yPos, contentWidth - 20, 25);
        composer.AddStaticText("Reason for ban:", CairoFont.WhiteSmallText(), reasonLabelBounds);
        yPos += 30;

        // Reason input (multiline)
        var reasonInputBounds = ElementBounds.Fixed(10, yPos, contentWidth - 20, 80);
        composer.AddTextInput(reasonInputBounds, OnReasonChanged, CairoFont.WhiteDetailText(), "reasonInput");
        yPos += 90;

        // Expiry days label
        var expiryLabelBounds = ElementBounds.Fixed(10, yPos, contentWidth - 20, 25);
        composer.AddStaticText("Ban duration (days, leave empty for permanent):", CairoFont.WhiteSmallText(),
            expiryLabelBounds);
        yPos += 30;

        // Expiry input
        var expiryInputBounds = ElementBounds.Fixed(10, yPos, 150, 30);
        composer.AddTextInput(expiryInputBounds, OnExpiryChanged, CairoFont.WhiteDetailText(), "expiryInput");
        yPos += 40;

        // Description text
        var descBounds = ElementBounds.Fixed(10, yPos, contentWidth - 20, 40);
        composer.AddStaticText(
            "This will kick the player if they are still a member and prevent them from rejoining.",
            CairoFont.WhiteSmallText().WithFontSize(10),
            descBounds
        );
        yPos += 45;

        // Buttons
        var cancelBounds = ElementBounds.Fixed(10, yPos, 120, 30);
        composer.AddSmallButton("Cancel", OnCancelClicked, cancelBounds);

        var banBounds = ElementBounds.Fixed(contentWidth - 130, yPos, 120, 30);
        composer.AddSmallButton("Ban Player", OnBanClicked, banBounds, EnumButtonStyle.Normal, "banButton");

        composer.EndChildElements().Compose();
    }

    private void OnReasonChanged(string reason)
    {
        _reason = reason;
    }

    private void OnExpiryChanged(string expiryDays)
    {
        _expiryDays = expiryDays;
    }

    private bool OnBanClicked()
    {
        // Validate reason
        if (string.IsNullOrWhiteSpace(_reason))
        {
            _capi.ShowChatMessage("Please enter a reason for the ban.");
            return true;
        }

        // Parse expiry days
        int? expiryDays = null;
        if (!string.IsNullOrWhiteSpace(_expiryDays))
        {
            if (int.TryParse(_expiryDays, out var days) && days > 0)
            {
                expiryDays = days;
            }
            else
            {
                _capi.ShowChatMessage("Invalid expiry days. Please enter a positive number or leave empty.");
                return true;
            }
        }

        // Send ban request with Data dictionary
        var packet = new ReligionActionRequestPacket("ban", _religionUID, _targetPlayerUID)
        {
            Data = new Dictionary<string, object>
            {
                { "Reason", _reason },
                { "ExpiryDays", expiryDays?.ToString() ?? "" }
            }
        };

        _channel.SendPacket(packet);

        var expiryText = expiryDays.HasValue ? $" for {expiryDays} days" : " permanently";
        _capi.ShowChatMessage($"{_targetPlayerName} has been banned{expiryText}.");

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
