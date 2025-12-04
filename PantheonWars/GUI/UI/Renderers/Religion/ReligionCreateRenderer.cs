using System;
using System.Numerics;
using ImGuiNET;
using PantheonWars.GUI.State;
using PantheonWars.GUI.UI.Components;
using PantheonWars.GUI.UI.Components.Buttons;
using PantheonWars.GUI.UI.Components.Inputs;
using PantheonWars.GUI.UI.Utilities;
using Vintagestory.API.Client;

namespace PantheonWars.GUI.UI.Renderers.Religion;

/// <summary>
///     Renderer for creating a new religion
///     Migrates functionality from CreateReligionOverlay
/// </summary>
internal static class ReligionCreateRenderer
{
    public static float Draw(
        BlessingDialogManager manager,
        ICoreClientAPI api,
        float x, float y, float width, float height)
    {
        var state = manager.ReligionState;
        var drawList = ImGui.GetWindowDrawList();
        var currentY = y;

        // Center the form
        var formWidth = 500f;
        var formX = x + (width - formWidth) / 2;
        var padding = 20f;

        // === HEADER ===
        var headerText = "Create New Religion";
        var headerSize = ImGui.CalcTextSize(headerText);
        var headerPos = new Vector2(formX, currentY);
        var headerColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold);
        drawList.AddText(ImGui.GetFont(), 20f, headerPos, headerColor, headerText);
        currentY += headerSize.Y + padding;

        // === FORM FIELDS ===
        var fieldWidth = formWidth;

        // Religion Name
        TextRenderer.DrawLabel(drawList, "Religion Name:", formX, currentY);
        currentY += 25f;

        state.CreateReligionName = TextInput.Draw(drawList, "##createReligionName", state.CreateReligionName,
            formX, currentY, fieldWidth, 32f, "Enter religion name...", 32);
        currentY += 40f;

        // Validation feedback
        if (!string.IsNullOrWhiteSpace(state.CreateReligionName))
        {
            if (state.CreateReligionName.Length < 3)
            {
                TextRenderer.DrawErrorText(drawList, "Religion name must be at least 3 characters", formX, currentY);
                currentY += 25f;
            }
            else if (state.CreateReligionName.Length > 32)
            {
                TextRenderer.DrawErrorText(drawList, "Religion name must be less than 32 characters", formX, currentY);
                currentY += 25f;
            }
        }

        // Deity Selection (simple tab-based approach)
        TextRenderer.DrawLabel(drawList, "Deity:", formX, currentY);
        currentY += 25f;

        var deities = new[] { "Khoras", "Lysa", "Aethra", "Gaia" };
        var currentDeityIndex = Array.IndexOf(deities, state.CreateDeity);
        if (currentDeityIndex == -1) currentDeityIndex = 0; // Default to Khoras

        // Draw deity selection as small tabs
        var newDeityIndex = TabControl.Draw(drawList, formX, currentY, fieldWidth, 32f, deities, currentDeityIndex, 4f);

        if (newDeityIndex != currentDeityIndex)
        {
            state.CreateDeity = deities[newDeityIndex];
            api.World.PlaySoundAt(new Vintagestory.API.Common.AssetLocation("pantheonwars:sounds/click"),
                api.World.Player.Entity, null, false, 8f, 0.5f);
        }

        currentY += 40f;

        // Public/Private Toggle
        state.CreateIsPublic = Checkbox.Draw(drawList, api, "Public (anyone can join)", formX, currentY, state.CreateIsPublic);
        currentY += 35f;

        // Info text
        var infoText = state.CreateIsPublic
            ? "Public religions appear in the browser and anyone can join."
            : "Private religions require an invitation from the founder.";
        TextRenderer.DrawInfoText(drawList, infoText, formX, currentY, fieldWidth);
        currentY += 50f;

        // Error message
        if (!string.IsNullOrEmpty(state.CreateError))
        {
            TextRenderer.DrawErrorText(drawList, state.CreateError, formX, currentY);
            currentY += 30f;
        }

        // === CREATE BUTTON (centered) ===
        const float buttonWidth = 160f;
        const float buttonHeight = 36f;
        var createButtonX = formX + (formWidth - buttonWidth) / 2;

        var canCreate = !string.IsNullOrWhiteSpace(state.CreateReligionName)
                        && state.CreateReligionName.Length >= 3
                        && state.CreateReligionName.Length <= 32;

        // Draw Create button
        if (ButtonRenderer.DrawButton(drawList, "Create Religion", createButtonX, currentY, buttonWidth, buttonHeight, isPrimary: true, enabled: canCreate))
        {
            if (canCreate)
            {
                api.World.PlaySoundAt(new Vintagestory.API.Common.AssetLocation("pantheonwars:sounds/click"),
                    api.World.Player.Entity, null, false, 8f, 0.5f);

                // Request creation via PantheonWarsSystem
                var system = api.ModLoader.GetModSystem<PantheonWarsSystem>();
                system?.RequestCreateReligion(state.CreateReligionName, state.CreateDeity, state.CreateIsPublic);

                // Clear form
                state.CreateReligionName = string.Empty;
                state.CreateDeity = "Khoras";
                state.CreateIsPublic = true;
                state.CreateError = null;

                // Switch to My Religion tab to see the new religion
                state.CurrentSubTab = ReligionSubTab.MyReligion;
            }
            else
            {
                api.World.PlaySoundAt(new Vintagestory.API.Common.AssetLocation("pantheonwars:sounds/error"),
                    api.World.Player.Entity, null, false, 8f, 0.3f);
            }
        }

        return height;
    }
}
