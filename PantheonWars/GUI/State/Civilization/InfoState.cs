using PantheonWars.Network.Civilization;

namespace PantheonWars.GUI.State.Civilization;

public class InfoState : IState
{
    public CivilizationInfoResponsePacket.CivilizationDetails? MyCivilization { get; set; }
    public float MemberScrollY { get; set; }
    public string InviteReligionName { get; set; } = string.Empty;
    public bool IsLoading { get; set; }
    public string? ErrorMsg { get; set; }

    public void Reset()
    {
        MyCivilization = null;
        MemberScrollY = 0;
        InviteReligionName = string.Empty;
        IsLoading = false;
        ErrorMsg = null;
    }
}