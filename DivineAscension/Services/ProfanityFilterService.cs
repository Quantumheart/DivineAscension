using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace DivineAscension.Services;

/// <summary>
///     Profanity filtering service for Divine Ascension mod.
///     Validates user-provided names (religions, civilizations) against a word list
///     to prevent inappropriate content.
///     Thread-safe singleton pattern.
/// </summary>
public class ProfanityFilterService
{
    private static readonly Lazy<ProfanityFilterService> _instance = new(() => new ProfanityFilterService());
    private readonly HashSet<string> _profanityWords = new(StringComparer.OrdinalIgnoreCase);
    private bool _isInitialized;
    private ICoreAPI? _api;

    private ProfanityFilterService()
    {
    }

    /// <summary>
    ///     Gets the singleton instance of the ProfanityFilterService.
    /// </summary>
    public static ProfanityFilterService Instance => _instance.Value;

    /// <summary>
    ///     Initialize the profanity filter service.
    ///     Loads word lists from mod assets.
    /// </summary>
    /// <param name="api">The API instance (client or server)</param>
    public void Initialize(ICoreAPI api)
    {
        if (_isInitialized)
        {
            return;
        }

        _api = api ?? throw new ArgumentNullException(nameof(api));
        LoadWordList();
        _isInitialized = true;

        api.Logger.Notification($"[DivineAscension ProfanityFilter] Initialized with {_profanityWords.Count} filtered words");
    }

    /// <summary>
    ///     Check if the provided text contains profanity.
    /// </summary>
    /// <param name="text">The text to check</param>
    /// <returns>True if profanity is detected, false otherwise</returns>
    public bool ContainsProfanity(string text)
    {
        return ContainsProfanity(text, out _);
    }

    /// <summary>
    ///     Check if the provided text contains profanity and return the matched word.
    /// </summary>
    /// <param name="text">The text to check</param>
    /// <param name="matchedWord">The profane word that was matched (if any)</param>
    /// <returns>True if profanity is detected, false otherwise</returns>
    public bool ContainsProfanity(string text, out string matchedWord)
    {
        matchedWord = string.Empty;

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        if (!_isInitialized)
        {
            // If not initialized, allow all content (fail open for safety)
            return false;
        }

        // Normalize the text: lowercase and remove extra whitespace
        var normalizedText = text.Trim().ToLowerInvariant();

        // Split on common word boundaries and punctuation
        // This helps catch words separated by spaces, underscores, hyphens, etc.
        var words = Regex.Split(normalizedText, @"[\s\-_.,;:!?]+")
            .Where(w => !string.IsNullOrWhiteSpace(w))
            .ToList();

        // Check each word against the profanity list
        foreach (var word in words)
        {
            if (_profanityWords.Contains(word))
            {
                matchedWord = word;
                return true;
            }
        }

        // Also check if the entire normalized text (no spaces) contains profanity
        // This catches cases like "bad word" vs "badword"
        var noSpaceText = normalizedText.Replace(" ", "").Replace("-", "").Replace("_", "");
        if (_profanityWords.Contains(noSpaceText))
        {
            matchedWord = noSpaceText;
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Load the profanity word list from mod assets.
    ///     Attempts to load from mod assets first, then falls back to embedded resources.
    /// </summary>
    private void LoadWordList()
    {
        _profanityWords.Clear();

        // Try loading from mod assets first (allows server admins to customize the list)
        if (_api != null && TryLoadFromAssets())
        {
            return;
        }

        // Fall back to loading from embedded resources
        LoadFromEmbeddedResource();
    }

    /// <summary>
    ///     Try to load word list from mod assets (allows customization by server admins).
    /// </summary>
    /// <returns>True if successfully loaded from assets, false otherwise</returns>
    private bool TryLoadFromAssets()
    {
        try
        {
            var asset = _api!.Assets.TryGet(new AssetLocation("divineascension", "config/profanity-filter.txt"));
            if (asset == null)
            {
                return false;
            }

            var content = Encoding.UTF8.GetString(asset.Data);
            ParseWordList(content);

            _api.Logger.Notification("[DivineAscension ProfanityFilter] Loaded custom word list from mod assets");
            return true;
        }
        catch (Exception ex)
        {
            _api?.Logger.Warning($"[DivineAscension ProfanityFilter] Could not load from assets: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    ///     Load word list from embedded resources.
    /// </summary>
    private void LoadFromEmbeddedResource()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "DivineAscension.Resources.profanity-filter.txt";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                _api?.Logger.Warning($"[DivineAscension ProfanityFilter] Could not find embedded resource: {resourceName}");
                LoadDefaultWordList();
                return;
            }

            using var reader = new StreamReader(stream);
            var content = reader.ReadToEnd();
            ParseWordList(content);

            _api?.Logger.Notification("[DivineAscension ProfanityFilter] Loaded word list from embedded resources");
        }
        catch (Exception ex)
        {
            _api?.Logger.Error($"[DivineAscension ProfanityFilter] Failed to load embedded resource: {ex.Message}");
            LoadDefaultWordList();
        }
    }

    /// <summary>
    ///     Parse a word list from text content.
    ///     Expected format: one word per line, # for comments, blank lines ignored.
    /// </summary>
    /// <param name="content">The text content to parse</param>
    private void ParseWordList(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        var lines = content.Split('\n');
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            // Skip empty lines and comments
            if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("#"))
            {
                continue;
            }

            // Add word to the set (case-insensitive)
            if (!string.IsNullOrWhiteSpace(trimmedLine))
            {
                _profanityWords.Add(trimmedLine.ToLowerInvariant());
            }
        }
    }

    /// <summary>
    ///     Load a minimal default word list as a fallback.
    ///     Server admins should provide their own comprehensive list.
    /// </summary>
    private void LoadDefaultWordList()
    {
        // Minimal sample list for demonstration
        // Server admins should add the full LDNOOBW list to assets/divineascension/config/profanity-filter.txt
        var defaultWords = new[]
        {
            "badword1",
            "badword2",
            "offensive"
        };

        foreach (var word in defaultWords)
        {
            _profanityWords.Add(word);
        }

        _api?.Logger.Warning("[DivineAscension ProfanityFilter] Using minimal default word list. " +
                            "For comprehensive filtering, add profanity-filter.txt to mod assets.");
    }

    /// <summary>
    ///     Initialize with a custom word list for testing purposes. Only use in unit tests.
    /// </summary>
    internal void InitializeForTesting(IEnumerable<string> words)
    {
        _profanityWords.Clear();
        foreach (var word in words)
        {
            _profanityWords.Add(word.ToLowerInvariant());
        }
        _isInitialized = true;
    }

    /// <summary>
    ///     Reset the service for testing purposes. Only use in unit tests.
    /// </summary>
    internal void ResetForTesting()
    {
        _profanityWords.Clear();
        _isInitialized = false;
        _api = null;
    }

    /// <summary>
    ///     Get the count of loaded words for diagnostics.
    /// </summary>
    internal int WordCount => _profanityWords.Count;
}
