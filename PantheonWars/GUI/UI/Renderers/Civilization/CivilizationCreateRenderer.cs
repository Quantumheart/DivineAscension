using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using PantheonWars.GUI.Events.Civilization;
using PantheonWars.GUI.Models.Civilization.Create;
using PantheonWars.GUI.UI.Components.Buttons;
using PantheonWars.GUI.UI.Components.Inputs;
using PantheonWars.GUI.UI.Utilities;

namespace PantheonWars.GUI.UI.Renderers.Civilization;

internal static class CivilizationCreateRenderer
{
    public static CivilizationCreateRenderResult Draw(
        CivilizationCreateViewModel vm,
        ImDrawListPtr drawList)
    {
        var events = new List<CreateEvent>();
        var currentY = vm.Y;

        TextRenderer.DrawLabel(drawList, "Create a New Civilization", vm.X, currentY, 18f, ColorPalette.White);
        currentY += 32f;

        // Requirements
        TextRenderer.DrawLabel(drawList, "Requirements:", vm.X, currentY, 14f, ColorPalette.Grey);
        currentY += 22f;

        var requirements = new[]
        {
            "You must be a religion founder",
            "Your religion must not be in another civilization",
            "Name must be 3-32 characters",
            "No cooldowns active"
        };

        foreach (var req in requirements)
        {
            // bullet
            drawList.AddCircleFilled(new Vector2(vm.X + 8f, currentY + 7f), 2f,
                ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold));
            drawList.AddText(ImGui.GetFont(), 14f, new Vector2(vm.X + 16f, currentY),
                ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey), req);
            currentY += 18f;
        }

        currentY += 16f;

        // Civilization name input
        TextRenderer.DrawLabel(drawList, "Civilization Name:", vm.X, currentY);
        currentY += 20f;

        var newName = TextInput.Draw(drawList, "##createCivName", vm.CivilizationName,
            vm.X, currentY,
            vm.Width * 0.7f, 30f,
            "Enter name (3-32 characters)...", 32);

        if (newName != vm.CivilizationName)
            events.Add(new CreateEvent.NameChanged(newName));

        currentY += 40f;

        // Create button
        if (ButtonRenderer.DrawButton(drawList, "Create Civilization", vm.X, currentY, 200f, 36f, true))
            events.Add(new CreateEvent.SubmitClicked());

        // Clear button
        if (ButtonRenderer.DrawButton(drawList, "Clear", vm.X + 210f, currentY, 80f, 36f))
            events.Add(new CreateEvent.ClearClicked());

        currentY += 50f;

        // Info text
        TextRenderer.DrawInfoText(drawList,
            "Once created, you can invite 2-4 religions with different deities to join your civilization. Work together to build a powerful alliance!",
            vm.X, currentY, vm.Width);
        currentY += 40f;

        return new CivilizationCreateRenderResult(events, currentY - vm.Y);
    }
}
