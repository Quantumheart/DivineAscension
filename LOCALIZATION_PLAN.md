# Divine Ascension Localization Implementation Plan

## Executive Summary

This document outlines the complete plan to add multi-language translation support to the Divine Ascension mod for Vintage Story. Currently, all 500-600+ user-facing strings are hardcoded in C#. This plan details how to integrate with Vintage Story's built-in localization system, refactor the codebase, and prepare for community translations.

**Estimated Scope**: Medium-to-large refactoring affecting 173+ files
**Estimated Strings**: 500-600+ translatable strings
**Target Languages**: English (base) + community-contributed languages

---

## 1. Research Findings - Vintage Story Localization System

### 1.1 How Vintage Story Handles Localization

**Language File Location**:
```
assets/{modid}/lang/
├── en.json          # English (base language)
├── de.json          # German
├── fr.json          # French
└── ...
```

**File Format**: Simple JSON key-value pairs
```json
{
  "divineascension:ui-create-religion": "Create New Religion",
  "divineascension:deity-gaia-name": "Gaia",
  "divineascension:blessing-swiftfoot-name": "Swiftfoot"
}
```

**API Access**:
- `Lang.Get("divineascension:ui-create-religion")` - Gets translation in current language
- `Lang.GetL(languageCode, "key")` - Gets translation for specific language
- `api.Logger` and other systems automatically use player's language

**Development Tools**:
- `/debug expclang` - Auto-generates lang entries for items/blocks
- `.reload lang` - Reloads all language files (for testing)

### 1.2 Key Naming Convention

**Format**: `{modid}:{category}-{subcategory}-{identifier}`

Examples:
- `divineascension:ui-button-create` - UI button text
- `divineascension:deity-gaia-name` - Deity name
- `divineascension:blessing-swiftfoot-description` - Blessing description
- `divineascension:command-error-noreligion` - Command error message
- `divineascension:rank-favor-initiate` - Rank name

---

## 2. Architecture Design

### 2.1 Localization Service

**Purpose**: Centralized service for all translation lookups with fallback support.

**Location**: `DivineAscension/Localization/LocalizationService.cs`

**Key Features**:
- Wraps Vintage Story's `Lang` API
- Provides typed access to translations (deities, blessings, UI, commands)
- Handles missing translations gracefully (fallback to English)
- Supports format strings with parameters (e.g., player names, counts)
- Singleton pattern accessible throughout the mod

**Interface**:
```csharp
public interface ILocalizationService
{
    string GetUI(string key, params object[] args);
    string GetDeity(DeityType deity, string field);
    string GetBlessing(string blessingId, string field);
    string GetCommand(string key, params object[] args);
    string GetRank(RankType type, int level);
    string GetStat(string statName);
}
```

### 2.2 Localization Keys

**Location**: `DivineAscension/Localization/LocalizationKeys.cs`

**Purpose**: Centralized constants for all translation keys to prevent typos and enable refactoring.

**Structure**:
```csharp
public static class LocalizationKeys
{
    public static class UI
    {
        public const string CreateReligion = "divineascension:ui-button-create-religion";
        public const string PlayerProgress = "divineascension:ui-label-player-progress";
        // ... 200+ UI keys
    }

    public static class Deities
    {
        public const string GaiaName = "divineascension:deity-gaia-name";
        public const string GaiaTitle = "divineascension:deity-gaia-title";
        // ... deity keys
    }

    public static class Blessings
    {
        public const string SwiftfootName = "divineascension:blessing-swiftfoot-name";
        public const string SwiftfootDescription = "divineascension:blessing-swiftfoot-description";
        // ... 80 blessing keys
    }

    public static class Commands
    {
        public const string ErrorNoReligion = "divineascension:command-error-noreligion";
        // ... 250+ command keys
    }
}
```

### 2.3 Integration Points

**Mod Initialization** (`DivineAscensionModSystem.cs`):
```csharp
public override void Start(ICoreAPI api)
{
    base.Start(api);
    LocalizationService.Initialize(api);
    // ... existing code
}
```

**TextRenderer Update** (`GUI/UI/Utilities/TextRenderer.cs`):
- Add overloads that accept localization keys
- Deprecate direct string parameters (or keep for backwards compatibility)

**UI Renderers** (45 files):
- Replace all hardcoded strings with `LocalizationService.GetUI(key)`
- Example: `"Create New Religion"` → `LocalizationService.GetUI(LocalizationKeys.UI.CreateReligion)`

---

## 3. Implementation Phases

### Phase 1: Foundation (Days 1-2)

**Goal**: Set up localization infrastructure

**Tasks**:
1. ✅ Research Vintage Story's localization system *(COMPLETED)*
2. Create `Localization/LocalizationService.cs`
   - Implement `ILocalizationService` interface
   - Add wrapper methods for `Lang.Get()` and `Lang.GetL()`
   - Add parameter formatting support
   - Add fallback handling for missing translations
3. Create `Localization/LocalizationKeys.cs`
   - Define key structure (UI, Deities, Blessings, Commands, Ranks, Stats)
   - Add constants for all categories
4. Create directory structure: `assets/divineascension/lang/`
5. Initialize empty `en.json` file with header
6. Update `DivineAscensionModSystem.cs` to initialize localization service

**Deliverables**:
- Working `LocalizationService` class
- Complete `LocalizationKeys` definitions
- Empty language file structure
- Mod initialization updated

---

### Phase 2: String Extraction (Days 3-5)

**Goal**: Extract all hardcoded strings and create English baseline

**Tasks**:
1. **Deities** (`GUI/UI/Utilities/DeityInfoHelper.cs`)
   - Extract 4 deities × 4 fields = 16 strings
   - Add to `en.json` under `deity-*` keys
   - Update helper to use `LocalizationService.GetDeity()`

2. **Blessings** (`Systems/BlessingDefinitions.cs`)
   - Extract 40 blessings × 2 (name + description) = 80 strings
   - Add to `en.json` under `blessing-*` keys
   - Refactor definitions to use localization keys instead of hardcoded strings

3. **UI Text** (`GUI/UI/Renderers/**/*.cs` - 45 files)
   - Extract 233+ UI strings (buttons, labels, headers, tooltips)
   - Add to `en.json` under `ui-*` keys
   - Group by renderer category (religion, civilization, diplomacy, favor, blessings)

4. **Commands** (`Commands/*.cs` - 5 files)
   - Extract 252+ command messages (errors, success, help text)
   - Add to `en.json` under `command-*` keys
   - Handle dynamic content (player names, counts) using format parameters

5. **System Text**
   - Rank names: `rank-favor-*` and `rank-prestige-*`
   - Stat names: `stat-*`
   - Validation messages: `validation-*`
   - Status text: `status-*`

**Deliverables**:
- Complete `en.json` with 500-600+ entries
- All keys defined in `LocalizationKeys.cs`
- Documentation of parameter placeholders (e.g., `{0}` for player name)

**Tools**:
- Manual extraction + categorization
- Grep searches for common patterns (`"string"`, `TextCommandResult.*("`, etc.)

---

### Phase 3: Code Refactoring (Days 6-10)

**Goal**: Replace all hardcoded strings with localization calls

**Tasks**:

#### 3.1 Core Systems
1. **TextRenderer** (`GUI/UI/Utilities/TextRenderer.cs`)
   - Add `RenderLocalizedLabel(string key, params object[] args)`
   - Add `RenderLocalizedInfoText(string key, params object[] args)`
   - Update all rendering methods to support localized keys

2. **DeityInfoHelper** (`GUI/UI/Utilities/DeityInfoHelper.cs`)
   - Update `GetDeityInfo()` to return localized strings
   - Ensure deity selection UI uses localized names/descriptions

3. **BlessingDefinitions** (`Systems/BlessingDefinitions.cs`)
   - Refactor constructor to accept blessing ID instead of name/description
   - Load name/description from localization service
   - Update all 40 blessing instantiations

#### 3.2 UI Renderers (45 files)

**Approach**: Process in batches by functional area

**Batch 1 - Religion UI** (10 files):
- `ReligionMainRenderer.cs`
- `ReligionCreationRenderer.cs`
- `ReligionMembersRenderer.cs`
- `ReligionRolesRenderer.cs`
- `ReligionSettingsRenderer.cs`
- etc.

**Batch 2 - Civilization UI** (8 files):
- `CivilizationMainRenderer.cs`
- `CivilizationCreationRenderer.cs`
- `CivilizationMembersRenderer.cs`
- etc.

**Batch 3 - Diplomacy UI** (5 files):
- `DiplomacyMainRenderer.cs`
- `DiplomacyRelationRenderer.cs`
- etc.

**Batch 4 - Favor/Blessings UI** (12 files):
- `FavorProgressRenderer.cs`
- `BlessingTreeRenderer.cs`
- `BlessingTooltipRenderer.cs`
- etc.

**Batch 5 - Shared UI** (10 files):
- `PlayerStatsRenderer.cs`
- `ConfirmationDialogRenderer.cs`
- `ErrorMessageRenderer.cs`
- etc.

**For each file**:
1. Read existing file
2. Identify all hardcoded strings
3. Replace with `LocalizationService.GetUI(LocalizationKeys.UI.*)`
4. Handle dynamic content with format parameters
5. Test UI rendering

#### 3.3 Commands (5 files)

**Files**:
- `Commands/ReligionCommands.cs`
- `Commands/FavorCommands.cs`
- `Commands/CivilizationCommands.cs`
- `Commands/DiplomacyCommands.cs`
- `Commands/AdminCommands.cs`

**For each file**:
1. Replace `TextCommandResult.Success("message")` with `TextCommandResult.Success(LocalizationService.GetCommand(LocalizationKeys.Commands.*))`
2. Replace `TextCommandResult.Error("message")` similarly
3. Handle parameters (player names, counts, etc.) using format strings
4. Update command help text

---

### Phase 4: Testing & Validation (Days 11-12)

**Goal**: Ensure all strings are properly localized and no regressions

**Tasks**:
1. **Manual Testing**
   - Launch mod and verify all UI text displays correctly
   - Test all dialogs: Religion, Civilization, Diplomacy, Favor, Blessings
   - Execute all commands and verify messages
   - Test edge cases (missing translations, parameters)

2. **Automated Testing**
   - Update test assertions to use localized strings or be locale-agnostic
   - Verify all localization keys exist in `en.json`
   - Test `LocalizationService` fallback behavior

3. **Missing Translation Detection**
   - Create validation script to check for:
     - Keys defined in `LocalizationKeys.cs` but not in `en.json`
     - Unused keys in `en.json`
     - Hardcoded strings remaining in code

4. **Language Reload Testing**
   - Test `.reload lang` command
   - Modify `en.json` and verify changes appear without restart

**Deliverables**:
- All tests passing
- No hardcoded strings remaining
- Validation script for translation completeness

---

### Phase 5: Documentation & Community Prep (Days 13-14)

**Goal**: Prepare for community translations

**Tasks**:
1. **Translation Guide** (`docs/TRANSLATION_GUIDE.md`)
   - How to create a new language file
   - Key naming conventions
   - Parameter placeholders (`{0}`, `{1}`, etc.)
   - Context for game-specific terms (deities, blessings, ranks)
   - Cultural sensitivity guidelines

2. **Translator Instructions**
   - Step-by-step: Copy `en.json` → `{language}.json`
   - List of languages Vintage Story supports
   - How to test translations in-game
   - Submission process (GitHub PR, Crowdin, etc.)

3. **Sample Translation** (Create `de.json` or another language)
   - Translate a subset of strings to demonstrate format
   - Include deity names, UI, and commands
   - Serve as reference for translators

4. **Update README**
   - Add "Translations" section
   - Link to translation guide
   - List supported languages
   - Credit translators

5. **Developer Documentation**
   - How to add new translatable strings
   - How to use `LocalizationService`
   - Best practices for localization-friendly code

**Deliverables**:
- Complete translation guide
- Sample translation file
- Updated README
- Developer documentation

---

## 4. Key Challenges & Solutions

### Challenge 1: Dynamic Content in Strings

**Problem**: Many messages include player names, counts, religion names
```csharp
$"You have joined {religionName}"
$"{playerName} was kicked from the religion"
```

**Solution**: Use format strings in JSON
```json
{
  "divineascension:command-success-joined": "You have joined {0}",
  "divineascension:command-success-kicked": "{0} was kicked from the religion"
}
```

In code:
```csharp
LocalizationService.GetCommand(LocalizationKeys.Commands.SuccessJoined, religionName)
```

### Challenge 2: Blessing/Deity Definitions in Code

**Problem**: Blessings are currently defined with hardcoded names/descriptions in constructors

**Current**:
```csharp
new BlessingDefinition("Swiftfoot", "Increases movement speed by 10%", ...)
```

**Solution**: Use blessing IDs and load from localization
```csharp
new BlessingDefinition("swiftfoot", LocalizationService, ...)
```

BlessingDefinition constructor:
```csharp
public BlessingDefinition(string id, ILocalizationService localization, ...)
{
    Id = id;
    Name = localization.GetBlessing(id, "name");
    Description = localization.GetBlessing(id, "description");
}
```

### Challenge 3: Large File Count (45 UI Renderers)

**Problem**: Refactoring 45 renderer files is time-consuming and error-prone

**Solution**:
- Process in functional batches (Religion → Civilization → Diplomacy → etc.)
- Create automated tests to catch missing translations
- Use grep/regex to find remaining hardcoded strings after each batch

### Challenge 4: Test Updates

**Problem**: Tests currently assert on hardcoded English strings
```csharp
Assert.Equal("Religion created!", result.StatusMessage);
```

**Solution**: Either:
1. Use localization in tests:
   ```csharp
   Assert.Equal(LocalizationService.GetCommand(LocalizationKeys.Commands.ReligionCreated), result.StatusMessage);
   ```
2. Make tests locale-agnostic (check success/error status instead of exact message)

### Challenge 5: Deity/Rank Names as Identifiers

**Problem**: Some code may use deity/rank names for logic (not just display)

**Solution**:
- Keep internal IDs separate from display names
- Use enums for deities: `DeityType.Gaia`, `DeityType.Aethra`, etc.
- Use rank levels (int) instead of names for logic

---

## 5. File Impact Summary

### New Files
- `DivineAscension/Localization/LocalizationService.cs`
- `DivineAscension/Localization/ILocalizationService.cs`
- `DivineAscension/Localization/LocalizationKeys.cs`
- `DivineAscension/assets/divineascension/lang/en.json`
- `docs/TRANSLATION_GUIDE.md`

### Modified Files (Core - 8 files)
- `DivineAscensionModSystem.cs` - Initialize localization
- `Systems/BlessingDefinitions.cs` - Use localization
- `GUI/UI/Utilities/DeityInfoHelper.cs` - Use localization
- `GUI/UI/Utilities/TextRenderer.cs` - Add localized rendering methods

### Modified Files (UI Renderers - 45 files)
- All files in `GUI/UI/Renderers/**/*.cs`

### Modified Files (Commands - 5 files)
- All files in `Commands/*.cs`

### Modified Files (Tests - ~20 files)
- Test files that assert on string messages

**Total Modified Files**: ~78 files
**Total New Files**: 5 files
**Total Impact**: 83 files

---

## 6. Translation Statistics

### Estimated String Counts by Category

| Category | Count | Examples |
|----------|-------|----------|
| UI Labels & Buttons | 233+ | "Create", "Delete", "Assign Role", "Religion Name" |
| Command Messages | 252+ | "Religion created!", "You are not in a religion" |
| Blessing Names | 40 | "Swiftfoot", "Iron Will", "Divine Protection" |
| Blessing Descriptions | 40 | "Increases movement speed by 10%" |
| Deity Names | 4 | "Gaia", "Aethra", "Lysa", "Khoras" |
| Deity Titles | 4 | "The Earth Mother", "The Sky Father" |
| Deity Domains | 4 | "Earth, Nature, Growth" |
| Deity Descriptions | 4 | Long-form lore text |
| Rank Names (Favor) | 5 | "Initiate", "Devoted", "Zealot", "Champion", "Exalted" |
| Rank Names (Prestige) | 5 | "Fledgling", "Established", "Renowned", "Legendary", "Mythic" |
| Stat Names | 20+ | "Movement Speed", "Melee Damage", "Max Health" |
| Validation Messages | 30+ | "Name must be at least 3 characters", "Role not found" |
| **TOTAL** | **600-650** | |

### Translation Workload per Language

- **Easy strings** (short labels): ~300 strings × 30 seconds = 2.5 hours
- **Medium strings** (command messages): ~250 strings × 1 minute = 4 hours
- **Hard strings** (lore/descriptions): ~50 strings × 3 minutes = 2.5 hours
- **Review & testing**: 2 hours

**Estimated time per language**: ~11 hours for a competent translator

---

## 7. Post-Implementation Enhancements

### 7.1 Crowdin Integration (Future)
- Set up Crowdin project for community translations
- Automate export/import of JSON files
- Integrate with BetterTranslations mod ecosystem

### 7.2 Translation Validation Tools
- CI/CD check for missing translation keys
- Tool to detect unused keys
- Automated diff when new strings are added

### 7.3 Plural Forms
- Handle plural forms for count-based strings
- Example: "1 member" vs "5 members"
- Use Vintage Story's plural support if available

### 7.4 Date/Time Formatting
- Localize timestamp displays
- Respect regional date formats

---

## 8. Risk Mitigation

### Risk 1: Breaking Changes
**Mitigation**:
- Work on feature branch (`claude/add-mod-translations-9hhmQ`)
- Incremental commits per phase
- Comprehensive testing before merging

### Risk 2: Missing Translations During Refactor
**Mitigation**:
- Create validation script early
- Run after each batch of changes
- Add CI check to prevent merges with missing keys

### Risk 3: Performance Impact
**Mitigation**:
- Cache frequently-used translations
- Benchmark UI rendering before/after
- Profile if performance degrades

### Risk 4: Translator Confusion
**Mitigation**:
- Provide clear context in translation guide
- Add comments in JSON for ambiguous strings
- Create visual reference guide with screenshots

---

## 9. Success Criteria

✅ **Technical**:
- All 500-600+ strings extracted to `en.json`
- Zero hardcoded user-facing strings in code
- All UI/commands display localized text
- Tests pass with localization enabled
- `.reload lang` works correctly

✅ **Quality**:
- No regression bugs in existing features
- Performance comparable to pre-localization
- Translation keys follow naming convention
- Code is maintainable (no magic strings)

✅ **Documentation**:
- Translation guide complete
- Developer docs updated
- Sample translation provided
- README updated

✅ **Community Readiness**:
- Easy for translators to contribute
- Clear submission process
- Recognition system for contributors

---

## 10. Timeline Summary

| Phase | Duration | Key Deliverables |
|-------|----------|------------------|
| **Phase 1**: Foundation | 2 days | LocalizationService, LocalizationKeys, lang structure |
| **Phase 2**: String Extraction | 3 days | Complete en.json with 600+ entries |
| **Phase 3**: Code Refactoring | 5 days | All 78 files refactored |
| **Phase 4**: Testing & Validation | 2 days | All tests passing, validation complete |
| **Phase 5**: Documentation | 2 days | Translation guide, sample translation, docs |
| **TOTAL** | **14 days** | Fully localized mod ready for translations |

---

## 11. Next Steps

1. **Get approval** for this plan from project maintainers
2. **Create GitHub issue** tracking the localization effort
3. **Begin Phase 1** (Foundation) - Set up infrastructure
4. **Commit incrementally** - One phase at a time
5. **Test thoroughly** - After each phase
6. **Open PR** - When all phases complete

---

## References

- [Vintage Story Modding: Asset System](https://wiki.vintagestory.at/Modding:Asset_System)
- [Vintage Story Modding: Modding Efficiently](https://wiki.vintagestory.at/Modding:Modding_Efficiently/en)
- [Vintage Story API: vsapi repository](https://github.com/anegostudios/vsapi)
- [BetterTranslations mod](https://mods.vintagestory.at/bettertranslations)
- [Crowdin: Vintage Story Mods](https://crowdin.com/project/vintage-story-mods)

---

**Document Version**: 1.0
**Last Updated**: 2026-01-04
**Author**: Claude (AI Assistant)
**Status**: Awaiting Approval
