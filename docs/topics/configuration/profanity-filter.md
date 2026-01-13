# Profanity Filter Configuration

The Divine Ascension mod includes a built-in profanity filter that validates user-provided names and descriptions to
prevent inappropriate content.

## Features

- **Multi-language support**: Filters profanity in English, German, Spanish, French, and Russian
- **L33t speak detection**: Catches common character substitutions (e.g., `$h1t`, `4ss`)
- **Repetition detection**: Catches stretched words (e.g., `shiiiit`, `assss`)
- **Word boundary detection**: Prevents false positives on legitimate words like "assassin" or "peacock"
- **Case-insensitive**: Matches regardless of capitalization

## Protected Content

The filter validates:

- Religion names (creation via commands and GUI)
- Deity names (creation and updates)
- Religion descriptions
- Civilization names

## Admin Commands

Server administrators with root privileges can enable or disable the profanity filter using:

```
/da config profanityfilter [on|off|status]
```

- `on` or `enable` - Enable the profanity filter
- `off` or `disable` - Disable the profanity filter
- `status` or no argument - Show current filter state and word count

The setting is stored per-world and persists across server restarts.

## Customizing Word Lists

### Default Word Lists

The mod ships with word lists for five languages in:

```
assets/divineascension/config/profanity/
├── en.txt (English)
├── de.txt (German)
├── es.txt (Spanish)
├── fr.txt (French)
└── ru.txt (Russian)
```

### Custom Override

To provide a custom comprehensive word list, create a single file:

```
assets/divineascension/config/profanity-filter.txt
```

When this file exists, it replaces all language-specific files.

### Word List Format

- One word or phrase per line
- UTF-8 encoding
- Lines starting with `#` are treated as comments
- Empty lines are ignored

Example:

```
# Custom profanity list
badword1
badword2
inappropriate phrase
```

## Use Cases for Disabling

Some servers may prefer to disable the filter:

- Mature/adult communities with trusted players
- Private servers where moderation is handled differently
- Servers where filtering causes false positives with legitimate names from specific cultures or languages

## Integration Points

The filter is checked at the following entry points:

| Entry Point                      | What's Validated                          |
|----------------------------------|-------------------------------------------|
| `/religion create` command       | Religion name, Deity name                 |
| `/religion setdeityname` command | Deity name                                |
| `/religion description` command  | Description                               |
| `/civ create` command            | Civilization name                         |
| GUI religion creation            | Religion name, Deity name                 |
| GUI civilization creation        | Civilization name                         |
| Network packet handlers          | All of the above (server-side validation) |

## Technical Details

- **Thread-safe**: Uses singleton pattern with proper locking
- **Fail-open**: If the filter fails to initialize, content is allowed rather than blocked
- **Performance**: Uses HashSet for O(1) lookups; generates variants efficiently
