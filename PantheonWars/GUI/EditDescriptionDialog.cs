using System;
using System.Diagnostics.CodeAnalysis;
using PantheonWars.Network;
using Vintagestory.API.Client;

namespace PantheonWars.GUI;

/// <summary>
///     Dialog for editing religion description
/// </summary>
[ExcludeFromCodeCoverage]
[Obsolete("This dialog has been migrated to ReligionMyReligionRenderer with inline description editing in the BlessingDialog tab system. Use BlessingDialog's Religion tab instead.")]
public class EditDescriptionDialog : GuiDialog
{
    private readonly ICoreClientAPI _capi;
    private readonly IClientNetworkChannel _channel;
    private readonly string _currentDescription;
    private readonly string _religionUID;
    private string _newDescription = "";


    public EditDescriptionDialog(ICoreClientAPI capi, IClientNetworkChannel channel, string religionUID,
        string currentDescription) : base(capi)
    {
        _capi = capi;
        _channel = channel;
        _religionUID = religionUID;
        _currentDescription = currentDescription ?? "";
        _newDescription = _currentDescription;
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
        const double contentHeight = 250;

        var bgBounds = ElementBounds.Fixed(0, titleBarHeight, contentWidth, contentHeight);

        var dialogBounds = ElementStdBounds.AutosizedMainDialog
            .WithAlignment(EnumDialogArea.CenterMiddle);

        SingleComposer?.Dispose();

        SingleComposer = capi.Gui
            .CreateCompo("editdescription", dialogBounds)
            .AddShadedDialogBG(ElementBounds.Fill)
            .AddDialogTitleBar("Edit Religion Description", OnTitleBarCloseClicked)
            .BeginChildElements(bgBounds);

        var composer = SingleComposer;

        double yPos = 10;

        // Description Label
        var descLabel = ElementBounds.Fixed(10, yPos, contentWidth - 20, 25);
        composer.AddStaticText("Description:", CairoFont.WhiteSmallText(), descLabel);
        yPos += 30;

        // Multi-line text input for description
        var descInput = ElementBounds.Fixed(10, yPos, contentWidth - 20, 120);
        composer.AddTextArea(descInput, OnDescriptionChanged, CairoFont.WhiteDetailText(), "descriptionInput");

        // Set initial value
        var textArea = composer.GetTextArea("descriptionInput");
        if (textArea != null) textArea.SetValue(_currentDescription);

        yPos += 130;

        // Character count
        var charCountBounds = ElementBounds.Fixed(10, yPos, contentWidth - 20, 20);
        composer.AddStaticText(
            $"Characters: {_currentDescription.Length} / 200",
            CairoFont.WhiteSmallText().WithFontSize(10),
            charCountBounds
        );
        yPos += 30;

        // Buttons
        var cancelBounds = ElementBounds.Fixed(10, yPos, 120, 30);
        composer.AddSmallButton("Cancel", OnCancelClicked, cancelBounds);

        var saveBounds = ElementBounds.Fixed(contentWidth - 130, yPos, 120, 30);
        composer.AddSmallButton("Save", OnSaveClicked, saveBounds, EnumButtonStyle.Normal, "saveButton");

        composer.EndChildElements().Compose();
    }

    private void OnDescriptionChanged(string description)
    {
        _newDescription = description;

        // Update character count display
        if (SingleComposer != null)
        {
            // Would need to refresh the dialog to update character count
            // For simplicity, we'll just enforce the limit in OnSaveClicked
        }
    }

    private bool OnSaveClicked()
    {
        // Validate length
        if (_newDescription.Length > 200)
        {
            _capi.ShowChatMessage("Description must be 200 characters or less.");
            return true;
        }

        // Send update request via existing ReligionActionRequestPacket
        // We'll use a new action type "edit_description"
        _channel.SendPacket(new EditDescriptionRequestPacket(_religionUID, _newDescription));

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