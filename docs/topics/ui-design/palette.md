# UI Palette — Iron Gall (Illuminated Manuscript)

Authoritative reference for the colour system used by the dialog UI. Lives
alongside `design-spec.md` for window/typography rules and
`overlay_polish.md` for component-level finishing notes. **All swatches
are defined in `DivineAscension/GUI/UI/Utilities/ColorPalette.cs`** — when
you touch a colour, change the constant, not an inline `Vector4`.

The palette is modelled on a real illuminated manuscript: aged-vellum
page, iron-gall ink for primary text, sepia for secondary, gold leaf for
ornament, with **lapis blue**, **vermilion red**, and **verdigris green**
as the historical accent inks. This is why state colours feel "earthed"
instead of saturated UI primaries — the page is the source of truth, not
ImGui defaults.

---

## TL;DR — which constant for what?

| Need | Constant | Hex |
|---|---|---|
| Page background / window fill | `Background` | `#EFE4CC` |
| Sidebar, right rail, table rows | `TableBackground` | `#D9C49C` |
| Button face / tooltip popup / title strip | `DarkBrown` | `#5C4528` |
| Button hover / card body | `LightBrown` | `#7A5C38` |
| Panel border / frame line | `BorderColor` | `#A89472` |
| Primary text on the page | `White` | `#2D2418` |
| Secondary text on the page | `Grey` | `#6B5638` |
| Primary text on a **dark** surface | `LightText` | `#E5DBC2` |
| Hints / captions on dark | `MutedText` | `#8E7A5C` |
| Disabled label | `DisabledGray` | `#A8987C` |
| Gold-leaf ornament / accent | `Gold` | `#B8862E` |
| Active / second identity ink | `Lapis` | `#2E4A6E` |
| Founder / error rubric | `Vermilion` (= `Red`) | `#9C2A1F` |
| Success / unlocked | `Verdigris` (= `Green`) | `#4F6E3B` |
| Warning | `Yellow` | `#B5852B` |

---

## 1. Surfaces

The page is the source of truth. Anywhere you draw something, ask:
*"am I drawing on the page, on a folded edge, or on an inset panel?"*
That tells you which surface constant to fill with — and that in turn
determines whether the text on it is **ink** (`White`) or
**cream** (`LightText`).

| Constant | Hex | RGB (float) | Role |
|---|---|---|---|
| `Background` | `#EFE4CC` | `0.937, 0.894, 0.800` | Main window fill — the vellum page. Set on `ImGuiCol.WindowBg` in `GuiDialog.DrawWindow`. |
| `TableBackground` | `#D9C49C` | `0.851, 0.769, 0.612` | Folded vellum edge. Used by sidebar, right rail, table rows, and any inset that wants to read as "on the page but recessed". |
| `LightBrown` | `#7A5C38` | `0.478, 0.361, 0.220` | Mid sepia. Button hover, card body (`ReligionInvitesRenderer`, `CivilizationInvitesRenderer`, role cards). |
| `DarkBrown` | `#5C4528` | `0.361, 0.271, 0.157` | Deep sepia. Button rest face, tooltip popup background, title strip ribbon. |
| `BorderColor` | `#A89472` | `0.659, 0.580, 0.447` | Faded ink edge. Frame/border lines. Lighter than `DarkBrown` so borders read against dark surfaces. |

### Surface depth ladder

From lightest (page) to darkest (ink panel):

```
Background       #EFE4CC   ████████   parchment
TableBackground  #D9C49C   ██████     folded edge
BorderColor      #A89472   █████      faded edge
LightBrown       #7A5C38   ███        mid sepia (hover)
DarkBrown        #5C4528   ██         deep sepia (rest)
```

If a component needs a new "in-between" surface, prefer alpha-blending
an existing one (e.g. `WithAlpha(Gold, 0.18f)` for the unread-letter
highlight in `RightRailRenderer`) over introducing a new constant.

---

## 2. Inks — text and accents

### Text

| Constant | Hex | Use on | Contrast on parchment |
|---|---|---|---|
| `White` | `#2D2418` | Page-level text (the default). | **12.08:1 — AAA** |
| `Grey` | `#6B5638` | Secondary text on the page (timestamps, metadata, leader labels). | 5.52:1 — AA |
| `LightText` | `#E5DBC2` | Text on `DarkBrown` / `LightBrown` / `Vermilion` surfaces (buttons, tooltips, banners, title strip, modal titles, dropdown closed-face, card bodies). | 6.52:1 — AA on `DarkBrown` |
| `MutedText` | `#8E7A5C` | Hints / captions on dark surfaces. | — |
| `DisabledGray` | `#A8987C` | Disabled labels (greyed-out buttons, gated sidebar items). | — |

> **Note on `White`:** the name is a historical artefact — the constant
> now resolves to iron-gall ink (`#2D2418`). Most renderers draw text on
> the parchment page, so "primary text" = dark. **If you find yourself
> drawing text on a dark surface, use `LightText` explicitly** — don't
> rely on the default.

### Accent inks

| Constant | Hex | Role |
|---|---|---|
| `Gold` | `#B8862E` | Gold-leaf accents — chapter chevrons, diamond ornaments, dividers, the hover wash applied at the layout root in `MainLayoutCoordinator`, and the **religion name** in the right-rail header. |
| `Lapis` | `#2E4A6E` | Active state, second identity. Used for the **prestige progress bar**, the **civilization name** in the right-rail header, and (intended) the active sidebar item highlight when next iterated. |
| `Vermilion` (aka `Red`) | `#9C2A1F` | Rubric red. Founder badge, errors, danger callouts. |
| `Verdigris` (aka `Green`) | `#4F6E3B` | Aged-copper green. Success, unlocked blessing, completed milestones. |
| `Yellow` | `#B5852B` | Earthed ochre. Warnings. Avoid for body text — too close to gold. |
| `SuccessGreen` | `#6A8F4F` | Brighter verdigris for badges / status pills that need to pop. |
| `ErrorRed` | `#B83A2C` | Brighter vermilion for the same. |

### Contrast cheatsheet

Measured against the page (`#EFE4CC`):

| Foreground | Ratio | Verdict |
|---|---|---|
| Iron-gall ink | 12.08:1 | AAA — use freely for body text |
| Lapis | 7.17:1 | AAA — safe for headings + body |
| Vermilion | 6.02:1 | AA — safe for labels + headings |
| Sepia | 5.52:1 | AA — safe for secondary text |
| Verdigris | 4.69:1 | AA — safe at ≥14pt |
| **Gold leaf** | **2.56:1** | **LOW — ornaments only, NOT body text** |

> **Gold leaf is for ornament, not for content.** It's iconic against the
> page but only 2.56:1 — fine for diamonds, chevrons, dividers, the
> hover wash, and short titles where decorative > legible. If you need
> a heading colour that reads cleanly on parchment, reach for `Lapis`
> or `White` (ink). Real manuscripts solved this by gilding raised
> letterforms — we don't have that affordance, so prefer ink for
> functional text.

---

## 3. Deity domain inks

Each domain is tinted with an earthed manuscript-ink hue so deity icons,
tooltip rings, and "letter" envelope tints sit on the page without
clashing. Live in `DomainHelper.GetDeityColor` (string + enum overloads).

| Domain | Hex | Reading |
|---|---|---|
| Craft | `#B26A2A` | Copper — forge & craft |
| Wild | `#5C6E2A` | Olive — hunt & wild |
| Conquest | `#8E2E1F` | Dried-blood red — domination & victory |
| Harvest | `#A07628` | Wheat ochre — agriculture & light |
| Stone | `#5E5448` | Warm slate — earth & stone |
| Unknown / `None` | `#A89472` | Faded ink (matches `BorderColor`) |

Never reintroduce the old saturated RGB tints (`(0.8, 0.2, 0.2)` red,
`(0.4, 0.8, 0.3)` lime, `(0.6, 0.1, 0.3)` magenta) — they fight the
parchment.

---

## 4. Overlays & modifications

| Constant | Hex / Spec | Role |
|---|---|---|
| `BlackOverlay` | `rgba(0.18, 0.13, 0.08, 0.8)` | Modal dim. Warm-dark, so a dimmed page still feels like a dimmed page rather than a cold black wash. |
| `BlackOverlayLight` | `rgba(0.18, 0.13, 0.08, 0.7)` | Lighter version of the same. |

Helpers (live in `ColorPalette`):

- `Darken(color, factor = 0.7f)` — RGB × factor, alpha preserved.
- `Lighten(color, factor = 1.3f)` — RGB × factor, clamped to 1.0,
  alpha preserved.
- `WithAlpha(color, alpha)` — replace alpha channel.

These exist so you don't have to introduce new constants for one-off
"slightly dimmer gold" cases. Prefer `Gold * 0.6f` over a new
`DimGold` constant.

---

## 5. Surface ↔ text pairing rules

The single rule that, if you remember it, prevents 90% of contrast
regressions:

> **Light surface → ink text (`White`). Dark surface → cream text
> (`LightText`).** Everything else follows.

| Surface | Default text | Secondary text |
|---|---|---|
| `Background` (parchment) | `White` | `Grey` |
| `TableBackground` (folded edge) | `White` | `Grey` |
| `LightBrown` (card body, button hover) | `LightText` | `MutedText` |
| `DarkBrown` (button rest, tooltip, title strip) | `LightText` | `MutedText` |
| `Vermilion` (error banner) | `LightText` | — |
| `Gold` (selected button accent, ornament wash) | `LightText` | — |

When a renderer paints text without specifying a colour
(`TextRenderer.DrawLabel(...)` with the default arg), it falls back to
`White` — which is correct on parchment / folded edge, **wrong on any
dark surface**. The card components (`ReligionInvitesRenderer`,
`CivilizationInvitesRenderer`, role cards) all pass `LightText`
explicitly; new card renderers should follow that pattern.

---

## 6. Anti-patterns

Things that drift the palette and should be fixed when spotted:

1. **Inline `new Vector4(...)` for a fill, border, or text colour.**
   The palette is centralised so a theme tweak is one file. The
   only legitimate inline uses are alpha-channel overrides
   (`new Vector4(1, 1, 1, glowAlpha)`) and image tints. Even those
   should be reviewed first.
2. **Reaching for ImGui defaults.** The dialog pushes warm-gold
   `Hover` / `Active` / `FrameBg` colours at the root in
   `MainLayoutCoordinator` so nested renderers don't have to repeat
   the push. Don't pop-push back to ImGui's blue defaults.
3. **Using `Gold` as text colour for body content.** It's an ornament
   ink — 2.56:1 contrast. Titles can use it sparingly; paragraphs
   should not.
4. **Using `White` on a dark surface.** `White` now resolves to ink-
   dark; on a dark sepia button it disappears. Use `LightText`.
5. **Reintroducing the dark-leather palette** (`#3D2E20` / `#291E16`
   / saturated `(0.8, 0.2, 0.2)` red). The Iron Gall swap rolled
   those out — see commit `2343a84`.
6. **A second purple, blue, or teal accent.** The palette already has
   three accent inks (gold / lapis / vermilion) plus verdigris for
   success. If a fourth seems necessary, the design probably needs
   to disambiguate via shape or position, not colour.

---

## 7. Where each constant is actually consumed

Useful when grepping for "who paints with X" — pointer rather than
exhaustive index:

| Constant | Major consumers |
|---|---|
| `Background` | `GuiDialog.DrawWindow`, table "selected row" highlight (`ReligionTableRenderer`, `CivilizationTableRenderer`, `HolySiteTableRenderer`), `RankUpNotificationOverlay`, `RankProgressHudOverlay`, `Dropdown` menu items |
| `TableBackground` | `SidebarRenderer`, `RightRailRenderer`, all `*TableRenderer` row fills |
| `DarkBrown` | `ButtonRenderer.DrawButton/DrawCloseButton/DrawSmallButton/DrawIconButton`, `TextInput.DrawSingleLine/DrawMultiline`, `ChromeRenderer.BeginStyledTooltip`, `TitleStripRenderer` ribbon, `Dropdown` closed face |
| `LightBrown` | `ReligionInvitesRenderer` / `CivilizationInvitesRenderer` card bodies, role cards in `ReligionRolesBrowseRenderer`, `Dropdown` hover, `CivilizationDetailRenderer` member rows, hover lifts |
| `Gold` | `ChromeRenderer.DrawDiamond/DrawChevron/DrawDivider`, sidebar chapter chevrons + verse bullets, `MainLayoutCoordinator` hover/active wash, button border, religion-name title text in `ReligionInfoHeaderRenderer` |
| `Lapis` | `ReligionHeaderRenderer` prestige bar + civ name |
| `Vermilion` | `ReligionHeaderRenderer` Founder badge, `ErrorBannerRenderer` |
| `Verdigris` | `BlessingNodeRenderer` (intended, currently inline), `CivilizationMilestoneRenderer` completion state, `HolySiteDetailRenderer` |
| `White` (= ink) | Default text colour everywhere via `TextRenderer.DrawLabel`, all page content renderers, Vows of the Order sub-headings (`BlessingVowsTabRenderer`) |
| `LightText` | Button labels, tooltip body (`TooltipRenderer`), banner messages, title-strip identity name, modal titles, dropdown selected face, card-body text, Selected Vow info panel title / body / sub-headings (`BlessingInfoSection*`) |
| `MutedText` | Tooltip category line, Selected Vow info panel meta line (`BlessingInfoSectionHeader`) |
| `SuccessGreen` / `ErrorRed` | Status badges and met / unmet requirements on dark surfaces — tooltip (`TooltipRenderer`), Selected Vow info panel (`BlessingInfoSectionHeader` AVAILABLE / LOCKED chip, `BlessingInfoSectionStats` effect deltas, `BlessingInfoSectionRequirements` prereq lines). Use these over the muted `Verdigris` / `Vermilion` whenever the badge needs to pop on `DarkBrown`. |

---

## 8. Followups

Tracked here so the next palette pass doesn't have to re-discover them:

- `BlessingNodeRenderer.cs:131-134` still has inline state tints
  (locked / unlockable / branchLocked). Should resolve to
  `DisabledGray` / `Verdigris` / `Vermilion`.
- `HolySiteDetailRenderer` mystery panel uses inline purple.
- `DiplomacyTabRenderer` cell highlights need a contrast pass on the
  new folded-edge background.
- "Founder" label in `ReligionHeaderRenderer` is now visually correct
  but still hardcoded English — needs a `UI_RELIGION_HEADER_FOUNDER`
  localization key.
- The parchment **texture** asset itself (a noisy cream tile behind the
  panel content) is still pending — the bundling work was reverted
  earlier and needs the cimgui-atlas issue resolved before retry.
- Consider deepening `Gold` half a step (~`#9E6F1F`) if titles continue
  to read pale in-game; lifts contrast to 3.4:1 without losing the
  gold-leaf reading.
