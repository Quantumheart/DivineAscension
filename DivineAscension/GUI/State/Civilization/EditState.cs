using DivineAscension.GUI.Interfaces;

namespace DivineAscension.GUI.State.Civilization;

public class EditState : IState
{
    public bool IsOpen { get; set; }
    public string CivId { get; set; } = string.Empty;
    public string EditingIcon { get; set; } = "default";

    public void Reset()
    {
        IsOpen = false;
        CivId = string.Empty;
        EditingIcon = "default";
    }
}