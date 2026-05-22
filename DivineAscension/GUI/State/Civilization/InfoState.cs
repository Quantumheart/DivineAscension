using DivineAscension.GUI.Interfaces;
using DivineAscension.Network.Civilization;

namespace DivineAscension.GUI.State.Civilization;

public class InfoState : IState
{
    public CivilizationInfoResponsePacket.CivilizationDetails? Info { get; set; }
    public float ScrollY { get; set; }
    public float MemberScrollY { get; set; }
    public string InviteReligionName { get; set; } = string.Empty;
    public string DescriptionText { get; set; } = string.Empty;
    public bool IsEditingDescription { get; set; }
    public bool IsEditingCapital { get; set; }
    public string CapitalNameText { get; set; } = string.Empty;
    public string CapitalBindingText { get; set; } = string.Empty;
    public bool IsLoading { get; set; }
    public string? ErrorMsg { get; set; }

    public void Reset()
    {
        Info = null;
        ScrollY = 0f;
        MemberScrollY = 0;
        InviteReligionName = string.Empty;
        DescriptionText = string.Empty;
        IsEditingDescription = false;
        IsEditingCapital = false;
        CapitalNameText = string.Empty;
        CapitalBindingText = string.Empty;
        IsLoading = false;
        ErrorMsg = null;
    }
}