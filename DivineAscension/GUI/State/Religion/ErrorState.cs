namespace DivineAscension.GUI.State.Religion;

public class ErrorState
{
    public string? LastActionError { get; set; }
    public string? BrowseError { get; set; }
    public string? InfoError { get; set; }
    public string? CreateError { get; set; }
    public string? ActivityError { get; set; }
    public string? RolesError { get; set; }

    public void Reset()
    {
        LastActionError = null;
        BrowseError = null;
        InfoError = null;
        CreateError = null;
        ActivityError = null;
        RolesError = null;
    }
}