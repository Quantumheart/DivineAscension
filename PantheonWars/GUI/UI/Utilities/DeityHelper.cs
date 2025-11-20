using System.Numerics;
using PantheonWars.Models.Enum;

namespace PantheonWars.GUI.UI.Utilities;

/// <summary>
///     Centralized helper for deity-related UI operations
///     Provides deity colors, titles, and lists for consistent UI presentation
/// </summary>
internal static class DeityHelper
{
    /// <summary>
    ///     All deity names in order (3-deity system)
    /// </summary>
    public static readonly string[] DeityNames =
    {
        "Aethra", "Gaia", "Morthen"
    };

    /// <summary>
    ///     Get the thematic color for a deity (by name)
    /// </summary>
    public static Vector4 GetDeityColor(string deity)
    {
        return deity switch
        {
            "Aethra" => new Vector4(0.9f, 0.9f, 0.6f, 1.0f),      // Light yellow - Light (Good)
            "Gaia" => new Vector4(0.5f, 0.4f, 0.2f, 1.0f),        // Brown - Nature (Neutral)
            "Morthen" => new Vector4(0.3f, 0.1f, 0.4f, 1.0f),     // Purple - Shadow & Death (Evil)
            _ => new Vector4(0.5f, 0.5f, 0.5f, 1.0f)              // Grey - Unknown
        };
    }

    /// <summary>
    ///     Get the thematic color for a deity (by enum)
    /// </summary>
    public static Vector4 GetDeityColor(DeityType deity)
    {
        return deity switch
        {
            DeityType.Aethra => new Vector4(0.9f, 0.9f, 0.6f, 1.0f),      // Light yellow - Light (Good)
            DeityType.Gaia => new Vector4(0.5f, 0.4f, 0.2f, 1.0f),        // Brown - Nature (Neutral)
            DeityType.Morthen => new Vector4(0.3f, 0.1f, 0.4f, 1.0f),     // Purple - Shadow & Death (Evil)
            _ => new Vector4(0.5f, 0.5f, 0.5f, 1.0f)                      // Grey - Unknown
        };
    }

    /// <summary>
    ///     Get the full title/description for a deity
    /// </summary>
    public static string GetDeityTitle(string deity)
    {
        return deity switch
        {
            "Aethra" => "Goddess of Light",
            "Gaia" => "Goddess of Nature",
            "Morthen" => "God of Shadow & Death",
            _ => "Unknown Deity"
        };
    }

    /// <summary>
    ///     Get the full title/description for a deity (by enum)
    /// </summary>
    public static string GetDeityTitle(DeityType deity)
    {
        return deity switch
        {
            DeityType.Aethra => "Goddess of Light",
            DeityType.Gaia => "Goddess of Nature",
            DeityType.Morthen => "God of Shadow & Death",
            _ => "Unknown Deity"
        };
    }

    /// <summary>
    ///     Convert deity name string to DeityType enum
    /// </summary>
    public static DeityType ParseDeityType(string deityName)
    {
        return deityName switch
        {
            "Aethra" => DeityType.Aethra,
            "Gaia" => DeityType.Gaia,
            "Morthen" => DeityType.Morthen,
            _ => DeityType.None
        };
    }

    /// <summary>
    ///     Get formatted display text for a deity (e.g., "Khoras - God of War")
    /// </summary>
    public static string GetDeityDisplayText(string deity)
    {
        return $"{deity} - {GetDeityTitle(deity)}";
    }

    /// <summary>
    ///     Get formatted display text for a deity (e.g., "Khoras - God of War")
    /// </summary>
    public static string GetDeityDisplayText(DeityType deity)
    {
        return $"{deity} - {GetDeityTitle(deity)}";
    }
}
