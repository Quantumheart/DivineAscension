# Profanity Filter Integration Guide

This document explains how to integrate a comprehensive profanity word list into the Divine Ascension mod for effective content moderation.

## Overview

The Divine Ascension mod includes a profanity filtering system that prevents inappropriate content in:
- Religion names
- Religion descriptions
- Civilization names

The filter uses a configurable word list with case-insensitive whole-word matching to detect and block offensive content.

## Quick Start

### For Server Administrators

1. **Download the LDNOOBW Word List** (recommended source):
   - Repository: https://github.com/LDNOOBW/List-of-Dirty-Naughty-Obscene-and-Otherwise-Bad-Words
   - Direct link to English list: https://raw.githubusercontent.com/LDNOOBW/List-of-Dirty-Naughty-Obscene-and-Otherwise-Bad-Words/master/en

2. **Choose Your Integration Method**:

   **Option A: Replace Embedded Resource (Mod Distribution)**
   - Replace the content of `DivineAscension/Resources/profanity-filter.txt` with the downloaded word list
   - Rebuild the mod
   - Distribute the updated mod

   **Option B: Server Override (No Rebuild Required)**
   - Create directory: `<ModsFolder>/divineascension/config/`
   - Save the word list as `profanity-filter.txt` in that directory
   - The mod will automatically load this file instead of the embedded resource
   - This allows server admins to customize the filter without rebuilding

3. **Restart the Server**
   - The word list is loaded during mod initialization
   - Check the server log for confirmation: `[DivineAscension ProfanityFilter] Initialized with X filtered words`

## File Format

The profanity filter expects a simple text file format:

```
# Comments start with #
# Blank lines are ignored
# One word per line
# Case insensitive matching

badword1
badword2
offensive
inappropriate
```

### Format Rules:
- **One word per line** - Each line should contain a single word/phrase to filter
- **Comments** - Lines starting with `#` are treated as comments and ignored
- **Blank lines** - Empty lines are skipped
- **Case insensitive** - Words are matched regardless of case (e.g., "BadWord" matches "badword")
- **No quotes needed** - Just the raw word, no quotation marks

## How It Works

### Detection Logic

The profanity filter uses **whole-word matching** to avoid false positives:

1. **Word Boundaries** - Splits text on whitespace and common separators (`-`, `_`, `.`, `,`, etc.)
2. **Individual Word Check** - Each word is checked against the profanity list
3. **Concatenation Check** - Also checks the text with spaces/separators removed (catches "bad word" vs "badword")
4. **Case Normalization** - All comparisons are case-insensitive

### Examples:

| Input                 | Word List    | Result  | Reason                                    |
|-----------------------|--------------|---------|-------------------------------------------|
| "Holy Church"         | ["hell"]     | ✅ Pass | "hell" not present as a whole word        |
| "Hellfire Temple"     | ["hell"]     | ❌ Fail | "hell" detected (word boundary match)     |
| "The Assassins"       | ["ass"]      | ✅ Pass | "ass" is part of "assassins", not a word  |
| "Bad Religion"        | ["bad"]      | ❌ Fail | "bad" detected as separate word           |
| "BAD_RELIGION"        | ["bad"]      | ❌ Fail | Case-insensitive, underscore is separator |

### Integration Points

The profanity filter validates at these entry points:

1. **Religion Creation**
   - Command: `/religion create <name> <deity>`
   - Network: CreateReligionRequestPacket
   - Validates: Religion name

2. **Religion Description**
   - Command: `/religion description <text>`
   - Network: EditDescriptionRequestPacket
   - Validates: Description text

3. **Civilization Creation**
   - Command: `/civ create <name>`
   - Network: CivilizationActionRequestPacket (action: "create")
   - Validates: Civilization name

All validation occurs **before** entity creation, ensuring offensive content never enters the database.

## Customization

### Adding Custom Words

Server administrators can extend the word list with server-specific terms:

```
# LDNOOBW base list
[paste LDNOOBW content here]

# Server-specific additions
customterm1
customterm2
serverspecificword
```

### Language Support

The current implementation focuses on English, but multi-language support can be added:

1. Create additional word list files (e.g., `profanity-filter-fr.txt`, `profanity-filter-de.txt`)
2. Update `ProfanityFilterService.cs` to load multiple language files
3. Check text against all loaded language lists

### Disabling the Filter

If you want to disable profanity filtering entirely:

1. **Method 1**: Create an empty word list file at `<ModsFolder>/divineascension/config/profanity-filter.txt`
2. **Method 2**: Modify the embedded `Resources/profanity-filter.txt` to be empty and rebuild

**Note**: The filter "fails open" - if uninitialized or the word list is empty, all content is allowed.

## Testing

The profanity filter includes comprehensive unit tests in `DivineAscension.Tests/Services/ProfanityFilterServiceTests.cs`.

To run tests:
```bash
dotnet test --filter FullyQualifiedName~ProfanityFilterServiceTests
```

Test coverage includes:
- Basic profanity detection
- Case insensitivity
- Word boundary detection
- Partial match prevention (avoiding "Scunthorpe problem")
- Special character handling
- Edge cases (null, empty, whitespace)

## Troubleshooting

### Filter Not Working

**Symptoms**: Offensive names are not being blocked

**Solutions**:
1. Check server logs for initialization message
2. Verify word list file exists and is readable
3. Ensure word list format is correct (one word per line, no extra formatting)
4. Test with a known word from the list

### False Positives

**Symptoms**: Legitimate names are being blocked

**Solutions**:
1. Review the word list for overly broad terms
2. Remove problematic words from the custom word list
3. Consider the whole-word matching behavior - partial word matches should not trigger

### Performance Issues

**Symptoms**: Server lag during religion/civilization creation

**Solutions**:
- The filter uses HashSet for O(1) lookups - performance should be excellent
- If issues persist, check the size of your word list (10,000+ words is fine)
- Profile using server performance tools

## Word List Maintenance

### Updating the Word List

1. **Pull latest LDNOOBW changes**:
   ```bash
   curl -O https://raw.githubusercontent.com/LDNOOBW/List-of-Dirty-Naughty-Obscene-and-Otherwise-Bad-Words/master/en
   ```

2. **Review changes** before deploying:
   - Compare old and new lists
   - Test with existing server content
   - Consider announcing updates to players

3. **Deploy**:
   - Replace the word list file
   - Restart server
   - Verify in logs

### Community Maintenance

The LDNOOBW project is community-maintained. You can contribute:
- Report missing terms via GitHub issues
- Submit pull requests with additions
- Follow the project for updates

## License and Attribution

The Divine Ascension profanity filter implementation is part of the mod's codebase.

### LDNOOBW Word List License

The LDNOOBW word list is licensed under **CC-BY-4.0**:
- ✅ Commercial use allowed
- ✅ Modification allowed
- ✅ Distribution allowed
- ⚠️ Attribution required

**Required Attribution**:
When distributing the mod with the LDNOOBW word list, include:
- Link to: https://github.com/LDNOOBW/List-of-Dirty-Naughty-Obscene-and-Otherwise-Bad-Words
- License: CC-BY-4.0
- Authors: LDNOOBW contributors

## Support

For issues related to:
- **Profanity filter functionality**: Open an issue on the Divine Ascension repository
- **Word list content**: Open an issue on the LDNOOBW repository
- **Server-specific problems**: Contact your server administrator

## Technical Details

### Architecture

- **Service**: `ProfanityFilterService` (Singleton pattern)
- **Initialization**: During mod startup in `DivineAscensionModSystem.Start()`
- **Storage**: HashSet<string> for O(1) lookup performance
- **Loading**: Embedded resource with optional file override
- **Validation**: Integrated at command and network handler layers

### Performance

- **Initialization**: One-time cost at server startup (~milliseconds for 10K words)
- **Per-check**: O(n) where n = number of words in the input text (typically <10)
- **Lookup**: O(1) hash table lookup per word
- **Memory**: ~100KB-1MB depending on word list size

### Thread Safety

The ProfanityFilterService singleton is thread-safe for reads after initialization. The word list is immutable after loading, making it safe for concurrent access from multiple threads.

## Future Enhancements

Potential improvements for future versions:
- Multi-language support
- Configurable severity levels (warning vs. blocking)
- Regex pattern support for advanced matching
- Server admin commands for runtime word list management
- Detailed logging of blocked attempts (with privacy considerations)
- Whitelist support for allowed exceptions

---

**Last Updated**: 2026-01-11
**Mod Version**: 3.2.1+
**Contact**: See Divine Ascension repository for support
