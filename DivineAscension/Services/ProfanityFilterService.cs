using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Vintagestory.API.Common;

namespace DivineAscension.Services;

/// <summary>
///     Profanity filtering service for Divine Ascension mod.
///     Validates user-provided names (religions, civilizations) against a word list
///     to prevent inappropriate content.
///     Thread-safe singleton pattern.
///     Supports multiple languages with merged word lists.
/// </summary>
public class ProfanityFilterService
{
    private static readonly Lazy<ProfanityFilterService> _instance = new(() => new ProfanityFilterService());
    private static readonly object _initLock = new();
    private static readonly Regex WordSplitter = new(@"[\s\-_.,;:!?]+", RegexOptions.Compiled);
    private static readonly string[] SupportedLanguages = { "en", "de", "es", "fr", "ru" };
    private readonly HashSet<string> _profanityWords = new(StringComparer.OrdinalIgnoreCase);
    private ICoreAPI? _api;
    private bool _isInitialized;

    private ProfanityFilterService()
    {
    }

    /// <summary>
    ///     Gets the singleton instance of the ProfanityFilterService.
    /// </summary>
    public static ProfanityFilterService Instance => _instance.Value;

    /// <summary>
    ///     Get the count of loaded words for diagnostics.
    /// </summary>
    internal int WordCount => _profanityWords.Count;

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

        lock (_initLock)
        {
            if (_isInitialized) // Double-check after acquiring lock
            {
                return;
            }

            _api = api ?? throw new ArgumentNullException(nameof(api));
            LoadWordList();
            _isInitialized = true;

            api.Logger.Notification(
                $"[DivineAscension ProfanityFilter] Initialized with {_profanityWords.Count} filtered words");
        }
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
        var words = WordSplitter.Split(normalizedText)
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
    ///     Priority: 1) Custom override file, 2) Multi-language files, 3) Default word list.
    /// </summary>
    private void LoadWordList()
    {
        _profanityWords.Clear();

        // Priority 1: Server admin custom override (single file, all languages combined)
        if (_api != null && TryLoadCustomOverride())
        {
            return;
        }

        // Priority 2: Load all language-specific files from assets and merge
        if (_api != null)
        {
            LoadAllLanguageFiles();
        }

        // Priority 3: Fall back to default word list if nothing was loaded
        if (_profanityWords.Count == 0)
        {
            LoadDefaultWordList();
        }
    }

    /// <summary>
    ///     Try to load custom override word list from mod assets.
    ///     This allows server admins to provide a single comprehensive file.
    /// </summary>
    /// <returns>True if successfully loaded from custom override, false otherwise</returns>
    private bool TryLoadCustomOverride()
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

            _api.Logger.Notification(
                $"[DivineAscension ProfanityFilter] Loaded custom override word list ({_profanityWords.Count} words)");
            return true;
        }
        catch (Exception ex)
        {
            _api?.Logger.Warning($"[DivineAscension ProfanityFilter] Could not load custom override: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    ///     Load and merge word lists from all supported languages.
    /// </summary>
    private void LoadAllLanguageFiles()
    {
        var loadedCount = 0;

        foreach (var lang in SupportedLanguages)
        {
            if (LoadLanguageFile(lang))
            {
                loadedCount++;
            }
        }

        if (loadedCount > 0)
        {
            _api?.Logger.Notification(
                $"[DivineAscension ProfanityFilter] Loaded {_profanityWords.Count} words from {loadedCount} language(s)");
        }
    }

    /// <summary>
    ///     Load a word list for a specific language.
    /// </summary>
    /// <param name="languageCode">The language code (e.g., "en", "de")</param>
    /// <returns>True if the language file was loaded successfully</returns>
    private bool LoadLanguageFile(string languageCode)
    {
        try
        {
            var asset = _api!.Assets.TryGet(
                new AssetLocation("divineascension", $"config/profanity/{languageCode}.txt"));

            if (asset == null)
            {
                _api.Logger.Debug($"[DivineAscension ProfanityFilter] No word list for {languageCode}");
                return false;
            }

            var content = Encoding.UTF8.GetString(asset.Data);
            var countBefore = _profanityWords.Count;
            ParseWordList(content);
            var added = _profanityWords.Count - countBefore;

            _api.Logger.Debug($"[DivineAscension ProfanityFilter] Loaded {added} words for {languageCode}");
            return true;
        }
        catch (Exception ex)
        {
            _api?.Logger.Warning($"[DivineAscension ProfanityFilter] Failed to load {languageCode}: {ex.Message}");
            return false;
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
        // Server admins should add comprehensive lists to assets/divineascension/config/profanity/{lang}.txt
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
                             "For comprehensive filtering, add word lists to config/profanity/ folder.");
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
}