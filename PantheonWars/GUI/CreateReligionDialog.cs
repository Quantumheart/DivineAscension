using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using PantheonWars.Constants;
using PantheonWars.Network;
using Vintagestory.API.Client;

namespace PantheonWars.GUI;

/// <summary>
///     Dialog for creating a new religion
/// </summary>
[ExcludeFromCodeCoverage]
public class CreateReligionDialog : GuiDialog
{
    private readonly ICoreClientAPI _capi;
    private readonly IClientNetworkChannel _channel;
    private bool _isPublic = true;
    private string _religionName = "";
    private readonly HashSet<string> _selectedBlessings = new();

    // Tier 1 blessing options
    private static readonly (string Id, string Name, string Description)[] Tier1Blessings =
    {
        (BlessingIds.EfficientMiner, "Efficient Miner", "+15% mining speed"),
        (BlessingIds.SwiftTraveler, "Swift Traveler", "+10% movement speed"),
        (BlessingIds.HardyConstitution, "Hardy Constitution", "-15% hunger rate"),
        (BlessingIds.BountifulHarvest, "Bountiful Harvest", "+2 max health")
    };

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
        const double contentWidth = 450;
        const double contentHeight = 420;

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

        // Blessing Selection
        var blessingLabel = ElementBounds.Fixed(10, yPos, contentWidth - 20, 25);
        composer.AddStaticText("Choose 2 Starter Blessings:", CairoFont.WhiteSmallText(), blessingLabel);
        yPos += 30;

        // Add blessing toggles
        foreach (var blessing in Tier1Blessings)
        {
            var isSelected = _selectedBlessings.Contains(blessing.Id);
            var toggleBounds = ElementBounds.Fixed(10, yPos, 20, 20);
            var textBounds = ElementBounds.Fixed(35, yPos, contentWidth - 50, 20);
            var descBounds = ElementBounds.Fixed(35, yPos + 18, contentWidth - 50, 16);

            composer.AddSwitch(
                (on) => OnBlessingToggled(blessing.Id, on),
                toggleBounds,
                $"blessing_{blessing.Id}"
            );

            composer.AddStaticText(blessing.Name, CairoFont.WhiteSmallText(), textBounds);
            composer.AddStaticText(blessing.Description, CairoFont.WhiteSmallText().WithFontSize(10), descBounds);

            // Set initial state
            if (isSelected)
            {
                composer.GetSwitch($"blessing_{blessing.Id}").SetValue(true);
            }

            yPos += 45;
        }

        // Selection count indicator
        var countBounds = ElementBounds.Fixed(10, yPos, contentWidth - 20, 20);
        composer.AddDynamicText(
            $"Selected: {_selectedBlessings.Count}/2",
            CairoFont.WhiteSmallText(),
            countBounds,
            "selectionCount"
        );
        yPos += 30;

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
        var descTextBounds = ElementBounds.Fixed(10, yPos, contentWidth - 20, 40);
        composer.AddStaticText(
            "Public religions can be joined by anyone.\nPrivate religions require an invitation.",
            CairoFont.WhiteSmallText().WithFontSize(10),
            descTextBounds
        );
        yPos += 50;

        // Buttons
        var cancelBounds = ElementBounds.Fixed(10, yPos, 120, 30);
        composer.AddSmallButton("Cancel", OnCancelClicked, cancelBounds);

        var createBounds = ElementBounds.Fixed(contentWidth - 130, yPos, 120, 30);
        composer.AddSmallButton("Create", OnCreateClicked, createBounds, EnumButtonStyle.Normal, "createButton");

        composer.EndChildElements().Compose();
    }

    private void OnBlessingToggled(string blessingId, bool isOn)
    {
        if (isOn)
        {
            if (_selectedBlessings.Count >= 2)
            {
                // Uncheck and don't add - already at max
                SingleComposer?.GetSwitch($"blessing_{blessingId}")?.SetValue(false);
                _capi.ShowChatMessage("You can only select 2 starter blessings.");
                return;
            }
            _selectedBlessings.Add(blessingId);
        }
        else
        {
            _selectedBlessings.Remove(blessingId);
        }

        // Update count display
        SingleComposer?.GetDynamicText("selectionCount")?.SetNewText($"Selected: {_selectedBlessings.Count}/2");
    }

    private void OnNameChanged(string name)
    {
        _religionName = name;
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

        if (_selectedBlessings.Count != 2)
        {
            _capi.ShowChatMessage("Please select exactly 2 starter blessings.");
            return true;
        }

        // Send creation request to server
        _channel.SendPacket(new CreateReligionRequestPacket(_religionName, _selectedBlessings.ToList(), _isPublic));

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