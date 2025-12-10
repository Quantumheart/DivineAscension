using PantheonWars.GUI.Interfaces;
using PantheonWars.Network.Civilization;

namespace PantheonWars.GUI.State.Civilization;

public class DetailState : IState
{
    public string? ViewingCivilizationId { get; set; }
    public CivilizationInfoResponsePacket.CivilizationDetails? ViewingCivilizationDetails { get; set; }
    public bool IsLoading { get; set; }
    public string? ErrorMsg { get; set; }
    public float MemberScrollY { get; set; }

    public void Reset()
    {
        ViewingCivilizationId = string.Empty;
        ViewingCivilizationDetails = null;
        IsLoading = false;
        ErrorMsg = null;
        MemberScrollY = 0f;
    }
}