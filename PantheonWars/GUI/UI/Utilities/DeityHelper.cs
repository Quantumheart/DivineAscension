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
    ///     All deity names in order (Utility-focused system - 4 deities)
    /// </summary>
    public static readonly string[] DeityNames =
    {
        "Khoras", "Lysa", "Aethra", "Gaia"
    };

    /// <summary>
    ///     Get the thematic color for a deity (by name)
    /// </summary>
    public static Vector4 GetDeityColor(string deity)
    {
        return deity switch
        {
            "Khoras" => new Vector4(0.8f, 0.2f, 0.2f, 1.0f), // Red - Forge & Craft
            "Lysa" => new Vector4(0.4f, 0.8f, 0.3f, 1.0f), // Green - Hunt & Wild
            "Aethra" => new Vector4(0.9f, 0.9f, 0.6f, 1.0f), // Light yellow - Agriculture & Light
            "Gaia" => new Vector4(0.5f, 0.4f, 0.2f, 1.0f), // Brown - Earth & Stone
            _ => new Vector4(0.5f, 0.5f, 0.5f, 1.0f) // Grey - Unknown
        };
    }

    /// <summary>
    ///     Get the thematic color for a deity (by enum)
    /// </summary>
    public static Vector4 GetDeityColor(DeityType deity)
    {
        return deity switch
        {
            DeityType.Khoras => new Vector4(0.8f, 0.2f, 0.2f, 1.0f), // Red - Forge & Craft
            DeityType.Lysa => new Vector4(0.4f, 0.8f, 0.3f, 1.0f), // Green - Hunt & Wild
            DeityType.Aethra => new Vector4(0.9f, 0.9f, 0.6f, 1.0f), // Light yellow - Agriculture & Light
            DeityType.Gaia => new Vector4(0.5f, 0.4f, 0.2f, 1.0f), // Brown - Earth & Stone
            _ => new Vector4(0.5f, 0.5f, 0.5f, 1.0f) // Grey - Unknown
        };
    }

    /// <summary>
    ///     Get the full title/description for a deity
    /// </summary>
    public static string GetDeityTitle(string deity)
    {
        return deity switch
        {
            "Khoras" => "God of the Forge & Craft",
            "Lysa" => "Goddess of the Hunt & Wild",
            "Aethra" => "Goddess of Agriculture & Light",
            "Gaia" => "Goddess of Earth & Stone",
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
            DeityType.Khoras => "God of the Forge & Craft",
            DeityType.Lysa => "Goddess of the Hunt & Wild",
            DeityType.Aethra => "Goddess of Agriculture & Light",
            DeityType.Gaia => "Goddess of Earth & Stone",
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
            "Khoras" => DeityType.Khoras,
            "Lysa" => DeityType.Lysa,
            "Aethra" => DeityType.Aethra,
            "Gaia" => DeityType.Gaia,
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