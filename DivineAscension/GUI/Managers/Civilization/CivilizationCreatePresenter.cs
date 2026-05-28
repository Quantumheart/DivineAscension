using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DivineAscension.GUI.Events.Civilization;
using DivineAscension.GUI.Models.Civilization.Create;
using DivineAscension.GUI.UI.Renderers.Civilization;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Models;
using DivineAscension.Models.Enum;
using DivineAscension.Services;
using ImGuiNET;

namespace DivineAscension.GUI.Managers.Civilization;

/// <summary>
///     Draws the civilization create form and reduces its events.
///     Owned by <see cref="CivilizationStateManager" />.
/// </summary>
internal sealed class CivilizationCreatePresenter(CivilizationStateManager owner)
{
    [ExcludeFromCodeCoverage]
    public void Draw(float x, float y, float width, float height)
    {
        // Check for profanity in civilization name
        string? profanityWord = null;
        if (!string.IsNullOrWhiteSpace(owner.State.CreateState.CreateCivName))
        {
            ProfanityFilterService.Instance.ContainsProfanity(owner.State.CreateState.CreateCivName, out profanityWord);
        }

        // Check for profanity in description
        string? profanityWordInDescription = null;
        if (!string.IsNullOrWhiteSpace(owner.State.CreateState.CreateDescription))
        {
            ProfanityFilterService.Instance.ContainsProfanity(owner.State.CreateState.CreateDescription,
                out profanityWordInDescription);
        }

        // Default ethos is derived from the founder's patron domain; the founder may
        // still override before submitting.
        var defaultEthos = ChromeContext.PlayerPatronDomain.HasValue
            ? CivilizationEthosDeriver.Derive(ChromeContext.PlayerPatronDomain.Value).Ethos
            : CivilizationEthos.Sovereign;
        var displayedEthos = owner.State.CreateState.SelectedEthos ?? defaultEthos;

        var vm = new CivilizationCreateViewModel(
            owner.State.CreateState.CreateCivName,
            owner.State.CreateState.SelectedIcon,
            owner.State.CreateState.CreateDescription,
            owner.State.CreateError,
            owner.UserIsReligionFounder,
            owner.HasCivilization(),
            profanityWord,
            profanityWordInDescription,
            x,
            y,
            width,
            height,
            displayedEthos);

        var drawList = ImGui.GetWindowDrawList();
        var result = CivilizationCreateRenderer.Draw(vm, drawList);
        ProcessEvents(result.Events);
    }

    public void ProcessEvents(IReadOnlyList<CreateEvent> events)
    {
        foreach (var evt in events)
            switch (evt)
            {
                case CreateEvent.NameChanged nc:
                    owner.State.CreateState.CreateCivName = nc.newName;
                    break;

                case CreateEvent.DescriptionChanged dc:
                    owner.State.CreateState.CreateDescription = dc.newDescription;
                    break;

                case CreateEvent.IconSelected iconSelected:
                    owner.State.CreateState.SelectedIcon = iconSelected.icon;
                    owner.SoundManager.PlayClick();
                    break;

                case CreateEvent.EthosSelected ethosSelected:
                    owner.State.CreateState.SelectedEthos = ethosSelected.ethos;
                    owner.SoundManager.PlayClick();
                    break;

                case CreateEvent.SubmitClicked:
                    if (!string.IsNullOrWhiteSpace(owner.State.CreateState.CreateCivName) &&
                        owner.State.CreateState.CreateCivName.Length >= 3 &&
                        owner.State.CreateState.CreateCivName.Length <= 32)
                    {
                        var pickedEthos = owner.State.CreateState.SelectedEthos.HasValue
                            ? (int)owner.State.CreateState.SelectedEthos.Value
                            : -1;
                        owner.RequestCivilizationAction("create", "", "", owner.State.CreateState.CreateCivName,
                            owner.State.CreateState.SelectedIcon, owner.State.CreateState.CreateDescription, pickedEthos);
                        owner.State.CreateState.CreateCivName = string.Empty;
                        owner.State.CreateState.SelectedIcon = "default";
                        owner.State.CreateState.CreateDescription = string.Empty;
                        owner.State.CreateState.SelectedEthos = null;
                    }
                    else
                    {
                        owner.ClientApi.ShowChatMessage("Civilization name must be 3-32 characters.");
                        owner.SoundManager.PlayError();
                    }

                    break;

                case CreateEvent.ClearClicked:
                    owner.State.CreateState.CreateCivName = string.Empty;
                    owner.State.CreateState.SelectedIcon = "default";
                    owner.State.CreateState.CreateDescription = string.Empty;
                    owner.State.CreateState.SelectedEthos = null;
                    break;
            }
    }
}
