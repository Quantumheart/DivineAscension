namespace PantheonWars.GUI.State;

public class ScrollState
{
    public float X { get; set; }
    public float Y { get; set; }

    public void Reset()
    {
        X = 0f;
        Y = 0f;
    }
}