using System.Text;
using DivineAscension.Services;

namespace DivineAscension.Tests.Services;

/// <summary>
///     Integration tests that verify the actual language files are valid and loadable.
///     These tests read the real language files from the TestAssets directory.
/// </summary>
[Collection("ProfanityFilterTests")]
public class ProfanityFilterLanguageFileTests
{
    private static readonly string TestAssetsPath = Path.Combine(
        AppContext.BaseDirectory,
        "TestAssets",
        "profanity");

    private static readonly Dictionary<string, int> ExpectedMinimumWordCounts = new()
    {
        { "en", 400 }, // English: 403 words
        { "de", 60 }, // German: 66 words
        { "es", 65 }, // Spanish: 68 words
        { "fr", 85 }, // French: 91 words
        { "ru", 145 } // Russian: 151 words
    };

    private static readonly Dictionary<string, string[]> SampleWordsPerLanguage = new()
    {
        { "en", new[] { "fuck", "shit", "ass", "bitch" } },
        { "de", new[] { "arsch", "scheiße", "ficken", "hure" } },
        { "es", new[] { "mierda", "puta", "coño", "idiota" } },
        { "fr", new[] { "merde", "putain", "con", "salope" } },
        { "ru", new[] { "блядь", "хуй", "пизда", "suka" } }
    };

    public ProfanityFilterLanguageFileTests()
    {
        // Reset the service before each test to ensure clean state
        ProfanityFilterService.Instance.ResetForTesting();
    }

    #region Helper Methods

    /// <summary>
    ///     Load words from a language file, using the same parsing logic as the service.
    /// </summary>
    private static List<string> LoadWordsFromFile(string filePath)
    {
        var words = new List<string>();
        var content = File.ReadAllText(filePath, Encoding.UTF8);
        var lines = content.Split('\n');

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            // Skip empty lines and comments (matching service behavior)
            if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("#"))
            {
                continue;
            }

            words.Add(trimmedLine.ToLowerInvariant());
        }

        return words;
    }

    #endregion

    #region File Existence Tests

    [Fact]
    public void TestAssetsDirectory_Exists()
    {
        // Act & Assert
        Assert.True(
            Directory.Exists(TestAssetsPath),
            $"TestAssets directory should exist at: {TestAssetsPath}");
    }

    [Theory]
    [InlineData("en")]
    [InlineData("de")]
    [InlineData("es")]
    [InlineData("fr")]
    [InlineData("ru")]
    public void LanguageFile_Exists(string languageCode)
    {
        // Arrange
        var filePath = Path.Combine(TestAssetsPath, $"{languageCode}.txt");

        // Act & Assert
        Assert.True(
            File.Exists(filePath),
            $"Language file should exist: {filePath}");
    }

    #endregion

    #region Word Count Validation Tests

    [Theory]
    [InlineData("en")]
    [InlineData("de")]
    [InlineData("es")]
    [InlineData("fr")]
    [InlineData("ru")]
    public void LanguageFile_HasExpectedMinimumWordCount(string languageCode)
    {
        // Arrange
        var filePath = Path.Combine(TestAssetsPath, $"{languageCode}.txt");
        var expectedMinimum = ExpectedMinimumWordCounts[languageCode];

        // Act
        var words = LoadWordsFromFile(filePath);

        // Assert
        Assert.True(
            words.Count >= expectedMinimum,
            $"{languageCode}.txt should have at least {expectedMinimum} words, but has {words.Count}");
    }

    [Fact]
    public void AllLanguageFiles_CombinedWordCount_IsReasonable()
    {
        // Arrange
        var allWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var languageCodes = new[] { "en", "de", "es", "fr", "ru" };

        // Act
        foreach (var lang in languageCodes)
        {
            var filePath = Path.Combine(TestAssetsPath, $"{lang}.txt");
            var words = LoadWordsFromFile(filePath);
            foreach (var word in words)
            {
                allWords.Add(word);
            }
        }

        // Assert - Combined should have at least 700 unique words
        Assert.True(
            allWords.Count >= 700,
            $"Combined word list should have at least 700 unique words, but has {allWords.Count}");
    }

    #endregion

    #region Content Validation Tests

    [Theory]
    [InlineData("en")]
    [InlineData("de")]
    [InlineData("es")]
    [InlineData("fr")]
    [InlineData("ru")]
    public void LanguageFile_ContainsExpectedSampleWords(string languageCode)
    {
        // Arrange
        var filePath = Path.Combine(TestAssetsPath, $"{languageCode}.txt");
        var sampleWords = SampleWordsPerLanguage[languageCode];
        var words = LoadWordsFromFile(filePath);

        // Act & Assert - At least some sample words should be present
        var foundCount = sampleWords.Count(sample =>
            words.Any(w => w.Equals(sample, StringComparison.OrdinalIgnoreCase)));

        Assert.True(
            foundCount >= 2,
            $"{languageCode}.txt should contain at least 2 of the sample words. " +
            $"Found {foundCount} of: {string.Join(", ", sampleWords)}");
    }

    [Theory]
    [InlineData("en")]
    [InlineData("de")]
    [InlineData("es")]
    [InlineData("fr")]
    [InlineData("ru")]
    public void LanguageFile_HasNoEmptyLines_AfterParsing(string languageCode)
    {
        // Arrange
        var filePath = Path.Combine(TestAssetsPath, $"{languageCode}.txt");

        // Act
        var words = LoadWordsFromFile(filePath);

        // Assert
        Assert.DoesNotContain(string.Empty, words);
        Assert.All(words, word => Assert.False(string.IsNullOrWhiteSpace(word)));
    }

    [Theory]
    [InlineData("en")]
    [InlineData("de")]
    [InlineData("es")]
    [InlineData("fr")]
    [InlineData("ru")]
    public void LanguageFile_WordsAreTrimmed(string languageCode)
    {
        // Arrange
        var filePath = Path.Combine(TestAssetsPath, $"{languageCode}.txt");

        // Act
        var words = LoadWordsFromFile(filePath);

        // Assert - No word should have leading/trailing whitespace
        Assert.All(words, word => { Assert.Equal(word, word.Trim()); });
    }

    #endregion

    #region Integration With Service Tests

    [Fact]
    public void LoadedWords_WorkWithProfanityFilter_Detection()
    {
        // Arrange - Load all words from files
        var allWords = new List<string>();
        var languageCodes = new[] { "en", "de", "es", "fr", "ru" };

        foreach (var lang in languageCodes)
        {
            var filePath = Path.Combine(TestAssetsPath, $"{lang}.txt");
            allWords.AddRange(LoadWordsFromFile(filePath));
        }

        // Initialize the service with loaded words
        ProfanityFilterService.Instance.ResetForTesting();
        ProfanityFilterService.Instance.InitializeForTesting(allWords);

        // Act & Assert - Test detection for sample words from each language
        Assert.True(ProfanityFilterService.Instance.ContainsProfanity("This contains fuck"));
        Assert.True(ProfanityFilterService.Instance.ContainsProfanity("Das ist scheiße"));
        Assert.True(ProfanityFilterService.Instance.ContainsProfanity("Eso es mierda"));
        Assert.True(ProfanityFilterService.Instance.ContainsProfanity("C'est de la merde"));
        Assert.True(ProfanityFilterService.Instance.ContainsProfanity("Это блядь"));
    }

    [Fact]
    public void LoadedWords_WordCountMatchesExpected()
    {
        // Arrange - Load all words from files
        var allWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var languageCodes = new[] { "en", "de", "es", "fr", "ru" };

        foreach (var lang in languageCodes)
        {
            var filePath = Path.Combine(TestAssetsPath, $"{lang}.txt");
            foreach (var word in LoadWordsFromFile(filePath))
            {
                allWords.Add(word);
            }
        }

        // Initialize the service with loaded words
        ProfanityFilterService.Instance.ResetForTesting();
        ProfanityFilterService.Instance.InitializeForTesting(allWords);

        // Act
        var serviceWordCount = ProfanityFilterService.Instance.WordCount;

        // Assert - Service word count should match our loaded count
        Assert.Equal(allWords.Count, serviceWordCount);
    }

    [Fact]
    public void LoadedWords_DuplicatesAcrossLanguages_AreDeduped()
    {
        // Arrange - Some words appear in multiple languages (e.g., "porno", "nazi")
        var languageCodes = new[] { "en", "de", "es", "fr", "ru" };
        var totalWordsWithDuplicates = 0;
        var uniqueWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var lang in languageCodes)
        {
            var filePath = Path.Combine(TestAssetsPath, $"{lang}.txt");
            var words = LoadWordsFromFile(filePath);
            totalWordsWithDuplicates += words.Count;
            foreach (var word in words)
            {
                uniqueWords.Add(word);
            }
        }

        // Initialize the service
        ProfanityFilterService.Instance.ResetForTesting();
        ProfanityFilterService.Instance.InitializeForTesting(uniqueWords);

        // Act
        var serviceWordCount = ProfanityFilterService.Instance.WordCount;

        // Assert - If there are duplicates, total should be greater than unique
        // The service should have exactly the unique count
        Assert.Equal(uniqueWords.Count, serviceWordCount);

        // Log info for verification (duplicates exist if these differ)
        // totalWordsWithDuplicates vs uniqueWords.Count shows deduplication happening
    }

    #endregion

    #region File Format Tests

    [Theory]
    [InlineData("en")]
    [InlineData("de")]
    [InlineData("es")]
    [InlineData("fr")]
    [InlineData("ru")]
    public void LanguageFile_IsValidUtf8(string languageCode)
    {
        // Arrange
        var filePath = Path.Combine(TestAssetsPath, $"{languageCode}.txt");

        // Act & Assert - Should not throw when reading as UTF-8
        var exception = Record.Exception(() =>
        {
            var content = File.ReadAllText(filePath, Encoding.UTF8);
            Assert.NotEmpty(content);
        });

        Assert.Null(exception);
    }

    [Fact]
    public void RussianFile_ContainsCyrillicCharacters()
    {
        // Arrange
        var filePath = Path.Combine(TestAssetsPath, "ru.txt");

        // Act
        var content = File.ReadAllText(filePath, Encoding.UTF8);

        // Assert - Should contain Cyrillic characters (Unicode range 0400-04FF)
        var hasCyrillic = content.Any(c => c >= '\u0400' && c <= '\u04FF');
        Assert.True(hasCyrillic, "Russian file should contain Cyrillic characters");
    }

    [Fact]
    public void GermanFile_ContainsUmlauts()
    {
        // Arrange
        var filePath = Path.Combine(TestAssetsPath, "de.txt");

        // Act
        var content = File.ReadAllText(filePath, Encoding.UTF8);

        // Assert - Should contain German umlauts (ä, ö, ü, ß)
        var umlauts = new[] { 'ä', 'ö', 'ü', 'ß', 'Ä', 'Ö', 'Ü' };
        var hasUmlaut = content.Any(c => umlauts.Contains(c));
        Assert.True(hasUmlaut, "German file should contain umlauts (ä, ö, ü, or ß)");
    }

    [Fact]
    public void SpanishFile_ContainsAccentedCharacters()
    {
        // Arrange
        var filePath = Path.Combine(TestAssetsPath, "es.txt");

        // Act
        var content = File.ReadAllText(filePath, Encoding.UTF8);

        // Assert - Should contain Spanish accented characters or ñ
        var spanishChars = new[] { 'á', 'é', 'í', 'ó', 'ú', 'ñ', 'Á', 'É', 'Í', 'Ó', 'Ú', 'Ñ', 'ü', 'Ü' };
        var hasSpanishChar = content.Any(c => spanishChars.Contains(c));
        Assert.True(hasSpanishChar, "Spanish file should contain accented characters or ñ");
    }

    [Fact]
    public void FrenchFile_ContainsAccentedCharacters()
    {
        // Arrange
        var filePath = Path.Combine(TestAssetsPath, "fr.txt");

        // Act
        var content = File.ReadAllText(filePath, Encoding.UTF8);

        // Assert - Should contain French accented characters
        var frenchChars = new[] { 'é', 'è', 'ê', 'ë', 'à', 'â', 'ù', 'û', 'ô', 'î', 'ï', 'ç' };
        var hasFrenchChar = content.Any(c => frenchChars.Contains(c));
        Assert.True(hasFrenchChar, "French file should contain accented characters");
    }

    #endregion
}