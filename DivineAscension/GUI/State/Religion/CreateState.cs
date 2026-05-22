using DivineAscension.Models.Enum;

namespace DivineAscension.GUI.State.Religion;

public class CreateState
{
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     The domain for the religion (retrieved from server)
    /// </summary>
    public string Domain { get; set; } = nameof(DeityDomain.Craft);

    /// <summary>
    ///     The custom name for the deity this religion worships
    /// </summary>
    public string DeityName { get; set; } = string.Empty;

    public bool IsPublic { get; set; } = true;

    /// <summary>
    ///     Optional motto/creed at creation (#361).
    /// </summary>
    public string Motto { get; set; } = string.Empty;

    public void Reset()
    {
        Name = string.Empty;
        Domain = nameof(DeityDomain.Craft);
        DeityName = string.Empty;
        IsPublic = false;
        Motto = string.Empty;
    }
}