using PantheonWars.Network;

namespace PantheonWars.GUI.State.Religion;

public class InfoState
{
    public PlayerReligionInfoResponsePacket? MyReligionInfo { get; set; }
    public float MyReligionScrollY { get; set; }
    public float MemberScrollY { get; set; }
    public float BanListScrollY { get; set; }
    public string InvitePlayerName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Loading { get; set; }
    public bool ShowDisbandConfirm { get; set; }
    public string? KickConfirmPlayerUID { get; set; }
    public string? BanConfirmPlayerUID { get; set; }
    public string? KickConfirmPlayerName { get; set; }
    public string? BanConfirmPlayerName { get; set; }

    public void Reset()
    {
        MyReligionInfo = null;
        MyReligionScrollY = 0f;
        MemberScrollY = 0f;
        BanListScrollY = 0f;
        InvitePlayerName = string.Empty;
        Description = string.Empty;
        Loading = false;
        ShowDisbandConfirm = false;
        KickConfirmPlayerUID = null;
        BanConfirmPlayerUID = null;
        KickConfirmPlayerName = null;
        BanConfirmPlayerName = null;
    }
}