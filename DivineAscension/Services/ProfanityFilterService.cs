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

    /// <summary>
    ///     L33t speak single-character substitution map.
    ///     Maps common l33t characters to their alphabetic equivalents.
    /// </summary>
    private static readonly Dictionary<char, char> LeetCharMap = new()
    {
        ['0'] = 'o',
        ['1'] = 'i',
        ['2'] = 'z',
        ['3'] = 'e',
        ['4'] = 'a',
        ['5'] = 's',
        ['6'] = 'g',
        ['7'] = 't',
        ['8'] = 'b',
        ['9'] = 'g',
        ['@'] = 'a',
        ['$'] = 's',
        ['!'] = 'i',
        ['|'] = 'i',
        ['+'] = 't',
        ['('] = 'c',
        ['<'] = 'c',
        ['{'] = 'c',
        ['['] = 'c'
    };

    /// <summary>
    ///     L33t speak multi-character substitution map.
    ///     These patterns are checked before single-character substitutions.
    ///     Ordered by length (longest first) for proper matching.
    /// </summary>
    private static readonly (string Pattern, string Replacement)[] LeetMultiCharPatterns =
    [
        ("|-|", "h"),
        ("/\\", "a"),
        ("\\/", "v"),
        ("|\\|", "n"),
        ("|3", "b"),
        ("|<", "k"),
        ("()", "o"),
        ("ph", "f"),
        ("vv", "w")
    ];

    private readonly HashSet<string> _profanityWords = new(StringComparer.OrdinalIgnoreCase);
    private ICoreAPI? _api;
    private bool _isEnabled = true;
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
    ///     Gets whether the profanity filter is currently enabled.
    /// </summary>
    public bool IsEnabled => _isEnabled;

    /// <summary>
    ///     Enables or disables the profanity filter at runtime.
    ///     When disabled, ContainsProfanity always returns false.
    /// </summary>
    /// <param name="enabled">True to enable, false to disable</param>
    public void SetEnabled(bool enabled)
    {
        _isEnabled = enabled;
        _api?.Logger.Notification($"[DivineAscension ProfanityFilter] Filter {(enabled ? "enabled" : "disabled")}");
    }

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

        // If filter is disabled, allow all content
        if (!_isEnabled)
        {
            return false;
        }

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

        // Check each word against all normalized variants (original, l33t, collapsed, combined)
        foreach (var word in words)
        {
            foreach (var variant in GenerateNormalizedVariants(word))
            {
                if (_profanityWords.Contains(variant))
                {
                    matchedWord = word; // Return the original word, not the variant
                    return true;
                }
            }
        }

        // Also check if the entire normalized text (no spaces) contains profanity
        // This catches cases like "bad word" vs "badword"
        var noSpaceText = normalizedText.Replace(" ", "").Replace("-", "").Replace("_", "");
        foreach (var variant in GenerateNormalizedVariants(noSpaceText))
        {
            if (_profanityWords.Contains(variant))
            {
                matchedWord = noSpaceText; // Return the concatenated form
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Normalizes l33t speak substitutions to standard characters.
    ///     Handles both single-character (4→a) and multi-character (|3→b) substitutions.
    /// </summary>
    /// <param name="input">The text to normalize (should already be lowercase)</param>
    /// <returns>Normalized text with l33t characters replaced</returns>
    private static string NormalizeLeetSpeak(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        // First pass: Replace multi-character patterns (longest first)
        // This handles patterns like "ph" -> "f", "vv" -> "w", etc.
        var result = input;
        foreach (var (pattern, replacement) in LeetMultiCharPatterns)
        {
            result = result.Replace(pattern, replacement);
        }

        // Fast path: if no single-char l33t characters remain, we're done
        var hasLeetChars = false;
        foreach (var c in result)
        {
            if (LeetCharMap.ContainsKey(c))
            {
                hasLeetChars = true;
                break;
            }
        }

        if (!hasLeetChars)
        {
            return result;
        }

        // Second pass: Replace single-character mappings
        var sb = new StringBuilder(result.Length);
        foreach (var c in result)
        {
            sb.Append(LeetCharMap.TryGetValue(c, out var replacement) ? replacement : c);
        }

        return sb.ToString();
    }

    /// <summary>
    ///     Collapses repeated characters to detect stretched profanity.
    ///     Example: "shiiiit" → "shit", "assss" → "as" (with maxRepeats=1)
    ///     Or: "shiiiit" → "shiit", "assss" → "ass" (with maxRepeats=2)
    /// </summary>
    /// <param name="input">The text to collapse</param>
    /// <param name="maxRepeats">Maximum consecutive repeats to keep (1 or 2)</param>
    /// <returns>Text with repeated characters collapsed</returns>
    private static string CollapseRepeatedCharacters(string input, int maxRepeats = 1)
    {
        if (string.IsNullOrEmpty(input) || input.Length < 2)
        {
            return input;
        }

        var sb = new StringBuilder(input.Length);
        var prevChar = input[0];
        var repeatCount = 1;
        sb.Append(prevChar);

        for (var i = 1; i < input.Length; i++)
        {
            var currentChar = input[i];
            if (currentChar == prevChar)
            {
                repeatCount++;
                if (repeatCount <= maxRepeats)
                {
                    sb.Append(currentChar);
                }
            }
            else
            {
                sb.Append(currentChar);
                prevChar = currentChar;
                repeatCount = 1;
            }
        }

        return sb.ToString();
    }

    /// <summary>
    ///     Generates normalized variants of a word for checking against the profanity list.
    ///     Returns unique variants only to avoid redundant checks.
    /// </summary>
    /// <param name="word">The word to generate variants for</param>
    /// <returns>Enumerable of unique normalized variants to check</returns>
    private static IEnumerable<string> GenerateNormalizedVariants(string word)
    {
        // Use a HashSet to track returned variants and avoid duplicates
        var returned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Original word (already lowercase)
        if (returned.Add(word))
        {
            yield return word;
        }

        // L33t normalized version
        var leetNormalized = NormalizeLeetSpeak(word);
        if (returned.Add(leetNormalized))
        {
            yield return leetNormalized;
        }

        // Repetition-collapsed versions (try both max 2 and max 1 repeats)
        // Max 2 handles cases like "assss" → "ass" (double letters preserved)
        var collapsed2 = CollapseRepeatedCharacters(word, maxRepeats: 2);
        if (returned.Add(collapsed2))
        {
            yield return collapsed2;
        }

        // Max 1 handles cases like "shiiiit" → "shit" (all doubled removed)
        var collapsed1 = CollapseRepeatedCharacters(word, maxRepeats: 1);
        if (returned.Add(collapsed1))
        {
            yield return collapsed1;
        }

        // L33t + collapsed combos (try both max values)
        var leetCollapsed2 = CollapseRepeatedCharacters(leetNormalized, maxRepeats: 2);
        if (returned.Add(leetCollapsed2))
        {
            yield return leetCollapsed2;
        }

        var leetCollapsed1 = CollapseRepeatedCharacters(leetNormalized, maxRepeats: 1);
        if (returned.Add(leetCollapsed1))
        {
            yield return leetCollapsed1;
        }
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
        _isEnabled = true;
        _api = null;
    }

    /// <summary>
    ///     Set the enabled state for testing purposes. Only use in unit tests.
    /// </summary>
    internal void SetEnabledForTesting(bool enabled)
    {
        _isEnabled = enabled;
    }
}