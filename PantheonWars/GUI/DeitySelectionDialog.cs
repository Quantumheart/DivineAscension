using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Cairo;
using PantheonWars.Models;
using PantheonWars.Models.Enum;
using PantheonWars.Systems.Interfaces;
using Vintagestory.API.Client;

namespace PantheonWars.GUI;

/// <summary>
///     Dialog for selecting a deity to pledge to
/// </summary>
[ExcludeFromCodeCoverage]
[Obsolete("This dialog has been migrated to ReligionCreateRenderer with inline deity selection in the BlessingDialog tab system. Use BlessingDialog's Religion tab instead.")]
public class DeitySelectionDialog : GuiDialog
{
    private readonly IDeityRegistry _deityRegistry;
    private readonly Action<DeityType> _onDeitySelected;
    private DeityType _selectedDeity = DeityType.None;

    public DeitySelectionDialog(ICoreClientAPI capi, IDeityRegistry deityRegistry, Action<DeityType> onDeitySelected)
        : base(capi)
    {
        _deityRegistry = deityRegistry;
        _onDeitySelected = onDeitySelected;
    }

    public override string? ToggleKeyCombinationCode => null;

    public override void OnGuiOpened()
    {
        base.OnGuiOpened();
        ComposeDialog();
    }

    private void ComposeDialog()
    {
        const int titleBarHeight = 30;
        const double contentWidth = 660;
        const double verticalSpacing = 95;

        var deities = _deityRegistry.GetAllDeities().ToList();

        // Pre-calculate all element bounds with relative positioning
        var textBounds = ElementBounds.Fixed(0, 0, contentWidth, 60);

        // Create bounds for each deity option
        var deityBoundsList = new List<ElementBounds>();
        double yPos = 75; // Start below intro text

        foreach (var deity in deities)
        {
            var deityBounds = ElementBounds.Fixed(10, yPos, contentWidth - 20, 80);
            deityBoundsList.Add(deityBounds);
            yPos += verticalSpacing;
        }

        // Confirm button (centered)
        var buttonBounds = ElementBounds.Fixed(contentWidth / 2 - 100, yPos + 15, 200, 30);

        // Create bgBounds with Fill pattern and register all children
        var bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
        bgBounds.BothSizing = ElementSizing.FitToChildren;
        bgBounds.WithChildren(new[] { textBounds, buttonBounds }.Concat(deityBoundsList).ToArray());
        bgBounds.fixedOffsetY = titleBarHeight; // CRITICAL: Shifts content below title bar

        // Auto-sized dialog at center of screen
        var dialogBounds = ElementStdBounds.AutosizedMainDialog
            .WithAlignment(EnumDialogArea.CenterMiddle);

        SingleComposer?.Dispose();

        SingleComposer = capi.Gui
            .CreateCompo("deityselection", dialogBounds)
            .AddShadedDialogBG(ElementBounds.Fill) // Use Fill, not custom bounds
            .AddDialogTitleBar("Choose Your Deity", OnTitleBarCloseClicked)
            .BeginChildElements(bgBounds); // bgBounds has fixedOffsetY set

        var composer = SingleComposer;

        // Introduction text
        composer.AddStaticText(
            "Pledge yourself to a deity and gain access to their divine powers. Choose wisely, for this decision shapes your path.",
            CairoFont.WhiteSmallText(),
            textBounds
        );

        // Add deity options using pre-calculated bounds
        for (var i = 0; i < deities.Count; i++) AddDeityOption(composer, deities[i], deityBoundsList[i]);

        // Confirm button
        composer.AddSmallButton("Confirm Selection", OnConfirmClicked, buttonBounds, EnumButtonStyle.Normal,
            "confirmButton");

        composer.EndChildElements().Compose();
    }

    private void AddDeityOption(GuiComposer composer, Deity deity, ElementBounds containerBounds)
    {
        // Add inset container for visual separation
        composer.AddInset(containerBounds, 2);

        // Inner padding for content
        double innerPadding = 8;
        var baseX = containerBounds.fixedX + innerPadding;
        var baseY = containerBounds.fixedY + innerPadding;
        var contentWidth = containerBounds.fixedWidth - innerPadding * 2;

        // Radio button with lambda to capture deity type
        var radioBounds = ElementBounds.Fixed(baseX, baseY + 5, 20, 20);
        var deityType = deity.Type; // Capture for lambda
        composer.AddToggleButton("", CairoFont.WhiteSmallText(), on => OnDeityToggle(on, deityType), radioBounds,
            deity.Type.ToString().ToLower());

        // Deity name and domain (header)
        var nameBounds = ElementBounds.Fixed(baseX + 30, baseY, contentWidth - 30, 25);
        composer.AddStaticText($"{deity.Name} - {deity.Domain}",
            CairoFont.WhiteDetailText().WithWeight(FontWeight.Bold).WithFontSize(20), nameBounds);

        // Deity description (below name)
        var descBounds = ElementBounds.Fixed(baseX + 30, baseY + 28, contentWidth - 30, 40);
        composer.AddStaticText(deity.Description, CairoFont.WhiteSmallText(), descBounds);
    }

    private void OnDeityToggle(bool on, DeityType deityType)
    {
        if (on)
        {
            _selectedDeity = deityType;

            // Uncheck all other toggles
            var deities = _deityRegistry.GetAllDeities();
            foreach (var deity in deities)
                if (deity.Type != deityType)
                {
                    var toggle = SingleComposer.GetToggleButton(deity.Type.ToString().ToLower());
                    if (toggle != null) toggle.SetValue(false);
                }
        }
        else
        {
            _selectedDeity = DeityType.None;
        }
    }

    private bool OnConfirmClicked()
    {
        if (_selectedDeity == DeityType.None)
        {
            capi.ShowChatMessage("Please select a deity before confirming.");
            return true;
        }

        _onDeitySelected?.Invoke(_selectedDeity);
        TryClose();
        return true;
    }

    private void OnTitleBarCloseClicked()
    {
        TryClose();
    }
}