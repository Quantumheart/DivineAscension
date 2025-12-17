using System.Collections.Generic;
using DivineAscension.Network;

namespace DivineAscension.GUI.State.Religion;

public class InvitesState
{
    public List<PlayerReligionInfoResponsePacket.ReligionInviteInfo> MyInvites { get; set; } = new();
    public float InvitesScrollY { get; set; }
    public bool Loading { get; set; }
    public string? InvitesError { get; set; }

    public void Reset()
    {
        MyInvites.Clear();
        InvitesScrollY = 0f;
        Loading = false;
        InvitesError = null;
    }
}