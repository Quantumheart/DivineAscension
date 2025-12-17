using System.Collections.Generic;
using DivineAscension.GUI.Interfaces;
using DivineAscension.Network.Civilization;

namespace DivineAscension.GUI.State.Civilization;

public class InviteState : IState
{
    public List<CivilizationInfoResponsePacket.PendingInvite> MyInvites { get; set; } = new();
    public float InvitesScrollY { get; set; }
    public bool IsLoading { get; set; }
    public string? ErrorMsg { get; set; }

    public void Reset()
    {
        MyInvites.Clear();
        InvitesScrollY = 0;
        IsLoading = false;
        ErrorMsg = string.Empty;
    }
}