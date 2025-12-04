using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using PantheonWars.Models.Enum;
using PantheonWars.Network;
using Vintagestory.API.Client;

namespace PantheonWars.GUI;

/// <summary>
///     Dialog for creating a new religion
/// </summary>
[ExcludeFromCodeCoverage]
[Obsolete("This dialog has been migrated to ReligionCreateRenderer in the BlessingDialog tab system. Use BlessingDialog's Religion tab instead.")]
public class CreateReligionDialog : GuiDialog
{
    private readonly ICoreClientAPI _capi;
    private readonly IClientNetworkChannel _channel;
    private bool _isPublic = true;
    private string _religionName = "";
    private DeityType _selectedDeity = DeityType.Khoras;

    public CreateReligionDialog(ICoreClientAPI capi, IClientNetworkChannel channel) : base(capi)
    {
        _capi = capi;
        _channel = channel;
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
        const double contentHeight = 300;

        var bgBounds = ElementBounds.Fixed(0, titleBarHeight, contentWidth, contentHeight);

        var dialogBounds = ElementStdBounds.AutosizedMainDialog
            .WithAlignment(EnumDialogArea.CenterMiddle);

        SingleComposer?.Dispose();

        SingleComposer = capi.Gui
            .CreateCompo("createreligion", dialogBounds)
            .AddShadedDialogBG(ElementBounds.Fill)
            .AddDialogTitleBar("Create New Religion", OnTitleBarCloseClicked)
            .BeginChildElements(bgBounds);

        var composer = SingleComposer;

        double yPos = 10;

        // Religion Name
        var nameLabel = ElementBounds.Fixed(10, yPos, contentWidth - 20, 25);
        composer.AddStaticText("Religion Name:", CairoFont.WhiteSmallText(), nameLabel);
        yPos += 30;

        var nameInput = ElementBounds.Fixed(10, yPos, contentWidth - 20, 30);
        composer.AddTextInput(nameInput, OnNameChanged, CairoFont.WhiteDetailText(), "nameInput");
        yPos += 45;

        // Deity Selection
        var deityLabel = ElementBounds.Fixed(10, yPos, contentWidth - 20, 25);
        composer.AddStaticText("Deity:", CairoFont.WhiteSmallText(), deityLabel);
        yPos += 30;

        var deityNames = Enum.GetValues(typeof(DeityType))
            .Cast<DeityType>()
            .Where(d => d != DeityType.None)
            .Select(d => d.ToString())
            .ToArray();

        var deityDropdown = ElementBounds.Fixed(10, yPos, contentWidth - 20, 30);
        composer.AddDropDown(deityNames, deityNames, 0, OnDeityChanged, deityDropdown, "deityDropdown");
        yPos += 45;

        // Public/Private Selection
        var visibilityLabel = ElementBounds.Fixed(10, yPos, contentWidth - 20, 25);
        composer.AddStaticText("Visibility:", CairoFont.WhiteSmallText(), visibilityLabel);
        yPos += 30;

        var visibilityOptions = new[] { "Public", "Private" };
        var visibilityDropdown = ElementBounds.Fixed(10, yPos, contentWidth - 20, 30);
        composer.AddDropDown(visibilityOptions, visibilityOptions, 0, OnVisibilityChanged, visibilityDropdown,
            "visibilityDropdown");
        yPos += 45;

        // Description text
        var descBounds = ElementBounds.Fixed(10, yPos, contentWidth - 20, 40);
        composer.AddStaticText(
            "Public religions can be joined by anyone.\nPrivate religions require an invitation.",
            CairoFont.WhiteSmallText().WithFontSize(10),
            descBounds
        );
        yPos += 50;

        // Buttons
        var cancelBounds = ElementBounds.Fixed(10, yPos, 120, 30);
        composer.AddSmallButton("Cancel", OnCancelClicked, cancelBounds);

        var createBounds = ElementBounds.Fixed(contentWidth - 130, yPos, 120, 30);
        composer.AddSmallButton("Create", OnCreateClicked, createBounds, EnumButtonStyle.Normal, "createButton");

        composer.EndChildElements().Compose();
    }

    private void OnNameChanged(string name)
    {
        _religionName = name;
    }

    private void OnDeityChanged(string code, bool selected)
    {
        if (Enum.TryParse<DeityType>(code, out var deity)) _selectedDeity = deity;
    }

    private void OnVisibilityChanged(string code, bool selected)
    {
        _isPublic = code == "Public";
    }

    private bool OnCreateClicked()
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(_religionName))
        {
            _capi.ShowChatMessage("Please enter a religion name.");
            return true;
        }

        if (_religionName.Length < 3)
        {
            _capi.ShowChatMessage("Religion name must be at least 3 characters.");
            return true;
        }

        if (_religionName.Length > 32)
        {
            _capi.ShowChatMessage("Religion name must be 32 characters or less.");
            return true;
        }

        if (_selectedDeity == DeityType.None)
        {
            _capi.ShowChatMessage("Please select a deity.");
            return true;
        }

        // Send creation request to server
        _channel.SendPacket(new CreateReligionRequestPacket(_religionName, _selectedDeity.ToString(), _isPublic));

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