namespace PantheonWars.GUI.State.Civilization;

public class CreateState : IState
{
    public string CreateCivName { get; set; } = string.Empty;
    public string CreateDescription { get; set; } = string.Empty;

    public void Reset()
    {
        CreateCivName = string.Empty;
        CreateDescription = string.Empty;
    }
}