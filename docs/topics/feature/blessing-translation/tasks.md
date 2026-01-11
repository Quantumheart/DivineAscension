# Blessing Translation - Task Checklist

## Phase 1: Create Helper Utility

### Task 1.1: Create BlessingLocalizationHelper

- [ ] Create file `/DivineAscension/GUI/UI/Utilities/BlessingLocalizationHelper.cs`
- [ ] Implement `GetNameKey(string blessingId, DeityType deity)` method
- [ ] Implement `GetDescriptionKey(string blessingId, DeityType deity)` method
- [ ] Implement `NormalizeBlessingId(string blessingId)` helper
- [ ] Implement `GetLocalizedName(Blessing blessing)` method
- [ ] Implement `GetLocalizedDescription(Blessing blessing)` method
- [ ] Add fallback to `Blessing.Name`/`Description` when key missing

## Phase 2: Add Localization Keys

### Task 2.1: Add Khoras Blessing Keys

- [ ] Open `/DivineAscension/Services/LocalizationKeys.cs`
- [ ] Add `#region Blessing Names - Khoras` section
- [ ] Add `BLESSING_KHORAS_CRAFTSMANS_TOUCH_NAME` constant
- [ ] Add `BLESSING_KHORAS_CRAFTSMANS_TOUCH_DESC` constant
- [ ] Add `BLESSING_KHORAS_MASTERWORK_TOOLS_NAME` constant
- [ ] Add `BLESSING_KHORAS_MASTERWORK_TOOLS_DESC` constant
- [ ] Add `BLESSING_KHORAS_FORGEBORN_ENDURANCE_NAME` constant
- [ ] Add `BLESSING_KHORAS_FORGEBORN_ENDURANCE_DESC` constant
- [ ] Add `BLESSING_KHORAS_LEGENDARY_SMITH_NAME` constant
- [ ] Add `BLESSING_KHORAS_LEGENDARY_SMITH_DESC` constant
- [ ] Add `BLESSING_KHORAS_AVATAR_OF_FORGE_NAME` constant
- [ ] Add `BLESSING_KHORAS_AVATAR_OF_FORGE_DESC` constant
- [ ] Add `BLESSING_KHORAS_IRON_WILL_NAME` constant
- [ ] Add `BLESSING_KHORAS_IRON_WILL_DESC` constant
- [ ] Add `BLESSING_KHORAS_SMELTERS_EFFICIENCY_NAME` constant
- [ ] Add `BLESSING_KHORAS_SMELTERS_EFFICIENCY_DESC` constant
- [ ] Add `BLESSING_KHORAS_ANVIL_MASTERY_NAME` constant
- [ ] Add `BLESSING_KHORAS_ANVIL_MASTERY_DESC` constant
- [ ] Add `BLESSING_KHORAS_BLESSED_METALWORK_NAME` constant
- [ ] Add `BLESSING_KHORAS_BLESSED_METALWORK_DESC` constant
- [ ] Add `BLESSING_KHORAS_FORGE_COMMUNION_NAME` constant
- [ ] Add `BLESSING_KHORAS_FORGE_COMMUNION_DESC` constant

### Task 2.2: Add Lysa Blessing Keys

- [ ] Add `#region Blessing Names - Lysa` section
- [ ] Add `BLESSING_LYSA_HUNTERS_INSTINCT_NAME` constant
- [ ] Add `BLESSING_LYSA_HUNTERS_INSTINCT_DESC` constant
- [ ] Add `BLESSING_LYSA_SWIFT_PURSUIT_NAME` constant
- [ ] Add `BLESSING_LYSA_SWIFT_PURSUIT_DESC` constant
- [ ] Add `BLESSING_LYSA_PREDATORS_FOCUS_NAME` constant
- [ ] Add `BLESSING_LYSA_PREDATORS_FOCUS_DESC` constant
- [ ] Add `BLESSING_LYSA_NATURES_BOUNTY_NAME` constant
- [ ] Add `BLESSING_LYSA_NATURES_BOUNTY_DESC` constant
- [ ] Add `BLESSING_LYSA_AVATAR_OF_HUNT_NAME` constant
- [ ] Add `BLESSING_LYSA_AVATAR_OF_HUNT_DESC` constant
- [ ] Add `BLESSING_LYSA_KEEN_SENSES_NAME` constant
- [ ] Add `BLESSING_LYSA_KEEN_SENSES_DESC` constant
- [ ] Add `BLESSING_LYSA_STALKERS_PATIENCE_NAME` constant
- [ ] Add `BLESSING_LYSA_STALKERS_PATIENCE_DESC` constant
- [ ] Add `BLESSING_LYSA_WILD_ENDURANCE_NAME` constant
- [ ] Add `BLESSING_LYSA_WILD_ENDURANCE_DESC` constant
- [ ] Add `BLESSING_LYSA_PACK_TACTICS_NAME` constant
- [ ] Add `BLESSING_LYSA_PACK_TACTICS_DESC` constant
- [ ] Add `BLESSING_LYSA_MOONLIT_STRIKE_NAME` constant
- [ ] Add `BLESSING_LYSA_MOONLIT_STRIKE_DESC` constant

### Task 2.3: Add Aethra Blessing Keys

- [ ] Add `#region Blessing Names - Aethra` section
- [ ] Add `BLESSING_AETHRA_GREEN_THUMB_NAME` constant
- [ ] Add `BLESSING_AETHRA_GREEN_THUMB_DESC` constant
- [ ] Add `BLESSING_AETHRA_BOUNTIFUL_HARVEST_NAME` constant
- [ ] Add `BLESSING_AETHRA_BOUNTIFUL_HARVEST_DESC` constant
- [ ] Add `BLESSING_AETHRA_SEASONS_WISDOM_NAME` constant
- [ ] Add `BLESSING_AETHRA_SEASONS_WISDOM_DESC` constant
- [ ] Add `BLESSING_AETHRA_FERTILE_TOUCH_NAME` constant
- [ ] Add `BLESSING_AETHRA_FERTILE_TOUCH_DESC` constant
- [ ] Add `BLESSING_AETHRA_AVATAR_OF_GROWTH_NAME` constant
- [ ] Add `BLESSING_AETHRA_AVATAR_OF_GROWTH_DESC` constant
- [ ] Add `BLESSING_AETHRA_SEED_BLESSING_NAME` constant
- [ ] Add `BLESSING_AETHRA_SEED_BLESSING_DESC` constant
- [ ] Add `BLESSING_AETHRA_CROP_RESILIENCE_NAME` constant
- [ ] Add `BLESSING_AETHRA_CROP_RESILIENCE_DESC` constant
- [ ] Add `BLESSING_AETHRA_HARVEST_KEEPER_NAME` constant
- [ ] Add `BLESSING_AETHRA_HARVEST_KEEPER_DESC` constant
- [ ] Add `BLESSING_AETHRA_NOURISHING_AURA_NAME` constant
- [ ] Add `BLESSING_AETHRA_NOURISHING_AURA_DESC` constant
- [ ] Add `BLESSING_AETHRA_EARTH_COMMUNION_NAME` constant
- [ ] Add `BLESSING_AETHRA_EARTH_COMMUNION_DESC` constant

### Task 2.4: Add Gaia Blessing Keys

- [ ] Add `#region Blessing Names - Gaia` section
- [ ] Add `BLESSING_GAIA_CLAY_SHAPING_NAME` constant
- [ ] Add `BLESSING_GAIA_CLAY_SHAPING_DESC` constant
- [ ] Add `BLESSING_GAIA_KILN_MASTERY_NAME` constant
- [ ] Add `BLESSING_GAIA_KILN_MASTERY_DESC` constant
- [ ] Add `BLESSING_GAIA_EARTHEN_RESILIENCE_NAME` constant
- [ ] Add `BLESSING_GAIA_EARTHEN_RESILIENCE_DESC` constant
- [ ] Add `BLESSING_GAIA_MASTER_POTTER_NAME` constant
- [ ] Add `BLESSING_GAIA_MASTER_POTTER_DESC` constant
- [ ] Add `BLESSING_GAIA_AVATAR_OF_EARTH_NAME` constant
- [ ] Add `BLESSING_GAIA_AVATAR_OF_EARTH_DESC` constant
- [ ] Add `BLESSING_GAIA_STEADY_HANDS_NAME` constant
- [ ] Add `BLESSING_GAIA_STEADY_HANDS_DESC` constant
- [ ] Add `BLESSING_GAIA_CLAY_ABUNDANCE_NAME` constant
- [ ] Add `BLESSING_GAIA_CLAY_ABUNDANCE_DESC` constant
- [ ] Add `BLESSING_GAIA_FIRED_PERFECTION_NAME` constant
- [ ] Add `BLESSING_GAIA_FIRED_PERFECTION_DESC` constant
- [ ] Add `BLESSING_GAIA_EARTHBOUND_STRENGTH_NAME` constant
- [ ] Add `BLESSING_GAIA_EARTHBOUND_STRENGTH_DESC` constant
- [ ] Add `BLESSING_GAIA_TERRA_COMMUNION_NAME` constant
- [ ] Add `BLESSING_GAIA_TERRA_COMMUNION_DESC` constant

## Phase 3: Add English Translations

### Task 3.1: Extract Current Blessing Strings

- [ ] Open `/DivineAscension/Systems/BlessingDefinitions.cs`
- [ ] Document all 40 blessing names and descriptions
- [ ] Create mapping of BlessingId → Name → Description

### Task 3.2: Add Khoras Translations to en.json

- [ ] Open `/DivineAscension/assets/divineascension/lang/en.json`
- [ ] Add `divineascension:blessing.khoras.craftsmans_touch.name` entry
- [ ] Add `divineascension:blessing.khoras.craftsmans_touch.desc` entry
- [ ] Add remaining 9 Khoras blessing name/desc pairs (18 entries total)

### Task 3.3: Add Lysa Translations to en.json

- [ ] Add `divineascension:blessing.lysa.hunters_instinct.name` entry
- [ ] Add `divineascension:blessing.lysa.hunters_instinct.desc` entry
- [ ] Add remaining 9 Lysa blessing name/desc pairs (18 entries total)

### Task 3.4: Add Aethra Translations to en.json

- [ ] Add `divineascension:blessing.aethra.green_thumb.name` entry
- [ ] Add `divineascension:blessing.aethra.green_thumb.desc` entry
- [ ] Add remaining 9 Aethra blessing name/desc pairs (18 entries total)

### Task 3.5: Add Gaia Translations to en.json

- [ ] Add `divineascension:blessing.gaia.clay_shaping.name` entry
- [ ] Add `divineascension:blessing.gaia.clay_shaping.desc` entry
- [ ] Add remaining 9 Gaia blessing name/desc pairs (18 entries total)

## Phase 4: Add Other Language Translations

### Task 4.1: Add French Translations

- [ ] Open `/DivineAscension/assets/divineascension/lang/fr.json`
- [ ] Add all 40 Khoras blessing translations (names + descriptions)
- [ ] Add all 40 Lysa blessing translations
- [ ] Add all 40 Aethra blessing translations
- [ ] Add all 40 Gaia blessing translations

### Task 4.2: Add Spanish Translations

- [ ] Open `/DivineAscension/assets/divineascension/lang/es.json`
- [ ] Add all 80 blessing entries (can copy English as placeholder)

### Task 4.3: Add German Translations

- [ ] Open `/DivineAscension/assets/divineascension/lang/de.json`
- [ ] Add all 80 blessing entries (can copy English as placeholder)

### Task 4.4: Add Russian Translations

- [ ] Open `/DivineAscension/assets/divineascension/lang/ru.json`
- [ ] Add all 80 blessing entries (can copy English as placeholder)

## Phase 5: Update UI Consumers

### Task 5.1: Update BlessingTooltipData

- [ ] Open `/DivineAscension/GUI/UI/Renderers/Blessings/BlessingTooltipData.cs`
- [ ] Add `using` statement for `BlessingLocalizationHelper`
- [ ] Replace `Name = blessing.Name` with `Name = BlessingLocalizationHelper.GetLocalizedName(blessing)`
- [ ] Replace `Description = blessing.Description` with `Description = BlessingLocalizationHelper.GetLocalizedDescription(blessing)`

### Task 5.2: Update BlessingNodeRenderer

- [ ] Open `/DivineAscension/GUI/UI/Renderers/Blessings/BlessingNodeRenderer.cs`
- [ ] Add `using` statement for `BlessingLocalizationHelper`
- [ ] Replace `state.Blessing!.Name` with `BlessingLocalizationHelper.GetLocalizedName(state.Blessing!)`

### Task 5.3: Update BlessingCommands

- [ ] Open `/DivineAscension/Commands/BlessingCommands.cs`
- [ ] Add `using` statement for `BlessingLocalizationHelper`
- [ ] Replace all `blessing.Name` usages with `BlessingLocalizationHelper.GetLocalizedName(blessing)`
- [ ] Replace all `blessing.Description` usages with `BlessingLocalizationHelper.GetLocalizedDescription(blessing)`

### Task 5.4: Search for Other Blessing Name Usages

- [ ] Search codebase for `\.Name` on Blessing objects
- [ ] Search codebase for `\.Description` on Blessing objects
- [ ] Update any additional consumers found

## Phase 6: Testing

### Task 6.1: Unit Tests

- [ ] Create test file `/DivineAscension.Tests/GUI/UI/Utilities/BlessingLocalizationHelperTests.cs`
- [ ] Test `GetNameKey()` returns correct format for each deity
- [ ] Test `GetDescriptionKey()` returns correct format
- [ ] Test `NormalizeBlessingId()` handles various ID formats
- [ ] Test `GetLocalizedName()` returns translated string
- [ ] Test `GetLocalizedName()` falls back to English when key missing

### Task 6.2: Build Verification

- [ ] Run `dotnet build DivineAscension.sln -c Debug`
- [ ] Fix any compilation errors
- [ ] Run `dotnet test`
- [ ] Fix any test failures

### Task 6.3: Manual Testing - English

- [ ] Start game in English
- [ ] Open blessing tree UI (Shift+G → Blessings tab)
- [ ] Verify all 40 blessing names display correctly
- [ ] Hover over blessings, verify tooltips show descriptions
- [ ] Run `/blessing list` command, verify names in output

### Task 6.4: Manual Testing - French

- [ ] Switch game language to French
- [ ] Open blessing tree UI
- [ ] Verify blessing names display in French
- [ ] Verify tooltip descriptions in French
- [ ] Run `/blessing list` command, verify French output

### Task 6.5: Manual Testing - Fallback

- [ ] Temporarily remove one blessing key from fr.json
- [ ] Switch to French
- [ ] Verify that blessing falls back to English name
- [ ] Restore the key after testing

## Phase 7: Finalization

### Task 7.1: Code Review Checklist

- [ ] All new code follows project conventions
- [ ] No hardcoded strings remain for blessing names
- [ ] Fallback mechanism works correctly
- [ ] No unused imports or dead code

### Task 7.2: Documentation

- [ ] Update CLAUDE.md if needed (localization section)
- [ ] Add comments to BlessingLocalizationHelper explaining key derivation

### Task 7.3: Commit & Push

- [ ] Stage all changes
- [ ] Create commit: `feat: add blessing name and description translations`
- [ ] Push to feature branch

---

## Summary

- **New Files:** 1 (BlessingLocalizationHelper.cs)
- **Modified Files:** 9 (LocalizationKeys.cs, 5 lang files, 3 UI consumers)
- **New Translation Entries:** 80 per language file (400 total across 5 languages)
- **Test Files:** 1 (BlessingLocalizationHelperTests.cs)
