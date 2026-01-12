using DivineAscension.Services;

namespace DivineAscension.Tests.Services;

public class ProfanityFilterServiceTests
{
    public ProfanityFilterServiceTests()
    {
        // Reset the service before each test
        ProfanityFilterService.Instance.ResetForTesting();
    }

    #region Singleton Tests

    [Fact]
    public void Instance_AlwaysReturnsSameInstance()
    {
        // Act
        var instance1 = ProfanityFilterService.Instance;
        var instance2 = ProfanityFilterService.Instance;

        // Assert
        Assert.Same(instance1, instance2);
    }

    #endregion

    #region Uninitialized State Tests

    [Fact]
    public void ContainsProfanity_WhenUninitialized_ReturnsFalse()
    {
        // Arrange - Reset but don't initialize
        ProfanityFilterService.Instance.ResetForTesting();

        // Act
        var result = ProfanityFilterService.Instance.ContainsProfanity("badword");

        // Assert - fail open for safety
        Assert.False(result);
    }

    #endregion

    #region Basic Profanity Detection Tests

    [Fact]
    public void ContainsProfanity_WithCleanText_ReturnsFalse()
    {
        // Arrange
        ProfanityFilterService.Instance.InitializeForTesting(new[] { "badword", "offensive" });

        // Act
        var result = ProfanityFilterService.Instance.ContainsProfanity("This is a clean religion name");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ContainsProfanity_WithProfaneWord_ReturnsTrue()
    {
        // Arrange
        ProfanityFilterService.Instance.InitializeForTesting(new[] { "badword", "offensive" });

        // Act
        var result = ProfanityFilterService.Instance.ContainsProfanity("badword");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ContainsProfanity_WithProfaneWordInSentence_ReturnsTrue()
    {
        // Arrange
        ProfanityFilterService.Instance.InitializeForTesting(new[] { "badword", "offensive" });

        // Act
        var result = ProfanityFilterService.Instance.ContainsProfanity("This contains badword in it");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ContainsProfanity_WithNullText_ReturnsFalse()
    {
        // Arrange
        ProfanityFilterService.Instance.InitializeForTesting(new[] { "badword" });

        // Act
        var result = ProfanityFilterService.Instance.ContainsProfanity(null!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ContainsProfanity_WithEmptyText_ReturnsFalse()
    {
        // Arrange
        ProfanityFilterService.Instance.InitializeForTesting(new[] { "badword" });

        // Act
        var result = ProfanityFilterService.Instance.ContainsProfanity(string.Empty);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ContainsProfanity_WithWhitespaceOnly_ReturnsFalse()
    {
        // Arrange
        ProfanityFilterService.Instance.InitializeForTesting(new[] { "badword" });

        // Act
        var result = ProfanityFilterService.Instance.ContainsProfanity("   ");

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Case Insensitivity Tests

    [Fact]
    public void ContainsProfanity_IsCaseInsensitive_Uppercase()
    {
        // Arrange
        ProfanityFilterService.Instance.InitializeForTesting(new[] { "badword" });

        // Act
        var result = ProfanityFilterService.Instance.ContainsProfanity("BADWORD");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ContainsProfanity_IsCaseInsensitive_MixedCase()
    {
        // Arrange
        ProfanityFilterService.Instance.InitializeForTesting(new[] { "badword" });

        // Act
        var result = ProfanityFilterService.Instance.ContainsProfanity("BaDwOrD");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ContainsProfanity_IsCaseInsensitive_Lowercase()
    {
        // Arrange
        ProfanityFilterService.Instance.InitializeForTesting(new[] { "BADWORD" });

        // Act
        var result = ProfanityFilterService.Instance.ContainsProfanity("badword");

        // Assert
        Assert.True(result);
    }

    #endregion

    #region Word Boundary Tests

    [Fact]
    public void ContainsProfanity_WithSpaceSeparation_DetectsProfanity()
    {
        // Arrange
        ProfanityFilterService.Instance.InitializeForTesting(new[] { "bad", "word" });

        // Act
        var result = ProfanityFilterService.Instance.ContainsProfanity("This is bad stuff");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ContainsProfanity_WithHyphenSeparation_DetectsProfanity()
    {
        // Arrange
        ProfanityFilterService.Instance.InitializeForTesting(new[] { "bad", "word" });

        // Act
        var result = ProfanityFilterService.Instance.ContainsProfanity("super-bad-name");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ContainsProfanity_WithUnderscoreSeparation_DetectsProfanity()
    {
        // Arrange
        ProfanityFilterService.Instance.InitializeForTesting(new[] { "bad", "word" });

        // Act
        var result = ProfanityFilterService.Instance.ContainsProfanity("super_bad_name");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ContainsProfanity_WithPunctuationSeparation_DetectsProfanity()
    {
        // Arrange
        ProfanityFilterService.Instance.InitializeForTesting(new[] { "bad" });

        // Act
        var result = ProfanityFilterService.Instance.ContainsProfanity("bad! wow");

        // Assert
        Assert.True(result);
    }

    #endregion

    #region Partial Match Prevention Tests

    [Fact]
    public void ContainsProfanity_DoesNotMatchPartialWords_InMiddle()
    {
        // Arrange
        ProfanityFilterService.Instance.InitializeForTesting(new[] { "ass" });

        // Act - "assassin" contains "ass" but should not match as a separate word
        var result = ProfanityFilterService.Instance.ContainsProfanity("assassin");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ContainsProfanity_DoesNotMatchPartialWords_AtStart()
    {
        // Arrange
        ProfanityFilterService.Instance.InitializeForTesting(new[] { "hell" });

        // Act - "hello" starts with "hell" but should not match
        var result = ProfanityFilterService.Instance.ContainsProfanity("hello");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ContainsProfanity_DoesNotMatchPartialWords_AtEnd()
    {
        // Arrange
        ProfanityFilterService.Instance.InitializeForTesting(new[] { "cock" });

        // Act - "peacock" ends with "cock" but should not match
        var result = ProfanityFilterService.Instance.ContainsProfanity("peacock");

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Matched Word Output Tests

    [Fact]
    public void ContainsProfanity_WithMatchedWord_OutputsMatchedWord()
    {
        // Arrange
        ProfanityFilterService.Instance.InitializeForTesting(new[] { "badword", "offensive" });

        // Act
        var result = ProfanityFilterService.Instance.ContainsProfanity("This has badword in it", out var matchedWord);

        // Assert
        Assert.True(result);
        Assert.Equal("badword", matchedWord);
    }

    [Fact]
    public void ContainsProfanity_WithNoMatch_OutputsEmptyString()
    {
        // Arrange
        ProfanityFilterService.Instance.InitializeForTesting(new[] { "badword" });

        // Act
        var result = ProfanityFilterService.Instance.ContainsProfanity("Clean text", out var matchedWord);

        // Assert
        Assert.False(result);
        Assert.Empty(matchedWord);
    }

    [Fact]
    public void ContainsProfanity_WithMultipleProfaneWords_ReturnsFirstMatch()
    {
        // Arrange
        ProfanityFilterService.Instance.InitializeForTesting(new[] { "bad", "offensive" });

        // Act
        var result =
            ProfanityFilterService.Instance.ContainsProfanity("This is bad and offensive", out var matchedWord);

        // Assert
        Assert.True(result);
        Assert.Equal("bad", matchedWord);
    }

    #endregion

    #region Special Character Tests

    [Fact]
    public void ContainsProfanity_WithMultipleSpaces_DetectsProfanity()
    {
        // Arrange
        ProfanityFilterService.Instance.InitializeForTesting(new[] { "bad" });

        // Act
        var result = ProfanityFilterService.Instance.ContainsProfanity("This  is    bad   stuff");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ContainsProfanity_WithLeadingTrailingWhitespace_DetectsProfanity()
    {
        // Arrange
        ProfanityFilterService.Instance.InitializeForTesting(new[] { "badword" });

        // Act
        var result = ProfanityFilterService.Instance.ContainsProfanity("  badword  ");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ContainsProfanity_WithConcatenatedProfanity_DetectsNoSpaceVersion()
    {
        // Arrange
        ProfanityFilterService.Instance.InitializeForTesting(new[] { "badword" });

        // Act - testing the no-space concatenation check
        var result = ProfanityFilterService.Instance.ContainsProfanity("bad word");

        // Assert - This should match because the filter checks concatenated versions
        Assert.True(result);
    }

    #endregion

    #region Word Count Tests

    [Fact]
    public void WordCount_AfterInitialization_ReturnsCorrectCount()
    {
        // Arrange
        var words = new[] { "word1", "word2", "word3" };
        ProfanityFilterService.Instance.InitializeForTesting(words);

        // Act
        var count = ProfanityFilterService.Instance.WordCount;

        // Assert
        Assert.Equal(3, count);
    }

    [Fact]
    public void WordCount_WhenUninitialized_ReturnsZero()
    {
        // Arrange
        ProfanityFilterService.Instance.ResetForTesting();

        // Act
        var count = ProfanityFilterService.Instance.WordCount;

        // Assert
        Assert.Equal(0, count);
    }

    #endregion

    #region Multi-Language Support Tests

    [Fact]
    public void ContainsProfanity_DetectsGermanProfanity()
    {
        // Arrange - Simulates merged word lists from multiple languages
        ProfanityFilterService.Instance.InitializeForTesting(new[]
        {
            "badword", // English
            "schimpfwort", // German
            "palabrota" // Spanish
        });

        // Act
        var result = ProfanityFilterService.Instance.ContainsProfanity("Das ist ein schimpfwort");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ContainsProfanity_DetectsFrenchProfanity()
    {
        // Arrange - Simulates merged word lists from multiple languages
        ProfanityFilterService.Instance.InitializeForTesting(new[]
        {
            "badword", // English
            "merde" // French
        });

        // Act
        var result = ProfanityFilterService.Instance.ContainsProfanity("C'est de la merde");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ContainsProfanity_DetectsRussianProfanity()
    {
        // Arrange - Simulates merged word lists from multiple languages
        // Note: Using Cyrillic characters
        ProfanityFilterService.Instance.InitializeForTesting(new[]
        {
            "badword", // English
            "ругательство" // Russian placeholder word
        });

        // Act
        var result = ProfanityFilterService.Instance.ContainsProfanity("Это ругательство тест");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ContainsProfanity_MergedWordList_DetectsAllLanguages()
    {
        // Arrange - Simulates merged word lists from all supported languages
        ProfanityFilterService.Instance.InitializeForTesting(new[]
        {
            "badword", // English
            "schimpfwort", // German
            "palabrota", // Spanish
            "grosmot", // French
            "ругательство" // Russian
        });

        // Act & Assert - All languages should be detected
        Assert.True(ProfanityFilterService.Instance.ContainsProfanity("This has badword"));
        Assert.True(ProfanityFilterService.Instance.ContainsProfanity("Das hat schimpfwort"));
        Assert.True(ProfanityFilterService.Instance.ContainsProfanity("Esto tiene palabrota"));
        Assert.True(ProfanityFilterService.Instance.ContainsProfanity("C'est un grosmot"));
        Assert.True(ProfanityFilterService.Instance.ContainsProfanity("Это ругательство"));
    }

    [Fact]
    public void ContainsProfanity_MergedWordList_WordCountIncludesAllLanguages()
    {
        // Arrange - Initialize with words from multiple languages
        var allWords = new[]
        {
            "word1", "word2", // English (2)
            "wort1", "wort2", // German (2)
            "palabra1", // Spanish (1)
            "mot1", "mot2", "mot3", // French (3)
            "слово1" // Russian (1)
        };
        ProfanityFilterService.Instance.InitializeForTesting(allWords);

        // Act
        var count = ProfanityFilterService.Instance.WordCount;

        // Assert
        Assert.Equal(9, count);
    }

    [Fact]
    public void ContainsProfanity_MergedWordList_DuplicatesAreIgnored()
    {
        // Arrange - Some words may appear in multiple languages
        var wordsWithDuplicates = new[]
        {
            "nazi", // English
            "nazi", // German (same word)
            "porno", // English
            "porno" // German (same word)
        };
        ProfanityFilterService.Instance.InitializeForTesting(wordsWithDuplicates);

        // Act
        var count = ProfanityFilterService.Instance.WordCount;

        // Assert - Duplicates should be deduplicated
        Assert.Equal(2, count);
    }

    #endregion
}