namespace DivineAscension.GUI.State.Religion;

public class RosterState
{
    public float ScrollY { get; set; }
    public string InvitePlayerName { get; set; } = string.Empty;
    public string? ExpandedMemberUID { get; set; }
    public string? KickConfirmPlayerUID { get; set; }
    public string? KickConfirmPlayerName { get; set; }
    public string? StrikeConfirmPlayerUID { get; set; }
    public string? StrikeConfirmPlayerName { get; set; }

    public void Reset()
    {
        ScrollY = 0f;
        InvitePlayerName = string.Empty;
        ExpandedMemberUID = null;
        KickConfirmPlayerUID = null;
        KickConfirmPlayerName = null;
        StrikeConfirmPlayerUID = null;
        StrikeConfirmPlayerName = null;
    }
}
