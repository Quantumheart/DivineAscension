namespace PantheonWars.GUI.State.Religion;

public class CreateState
{
    public string Name { get; set; } = string.Empty;
    public string DeityName { get; set; } = "Khoras";
    public bool IsPublic { get; set; } = true;

    public void Reset()
    {
        Name = string.Empty;
        DeityName = string.Empty;
        IsPublic = false;
    }
}