using DivineAscension.GUI.Interfaces;
using DivineAscension.Models.Enum;

namespace DivineAscension.GUI.State.Civilization;

public class CreateState : IState
{
    public string CreateCivName { get; set; } = string.Empty;
    public string CreateDescription { get; set; } = string.Empty;
    public string SelectedIcon { get; set; } = "default";

    /// <summary>
    ///     Founder-picked ethos. Null means "no explicit pick yet" — the server will
    ///     derive from the founder religion's patron domain at create time. The
    ///     create form seeds this with the derived value as soon as the founder's
    ///     religion is known, so the picker shows the derived option highlighted.
    /// </summary>
    public CivilizationEthos? SelectedEthos { get; set; }

    public void Reset()
    {
        CreateCivName = string.Empty;
        CreateDescription = string.Empty;
        SelectedIcon = "default";
        SelectedEthos = null;
    }
}
