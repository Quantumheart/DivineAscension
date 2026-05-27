---
name: ledger-chapter
description: Redesign a Divine Ascension UI pane to the codex/ledger "chapter" destination spec — chapter title strip with right-side domain glyph and edit pencil, prose intro, dotted-leader stat block, prose body block with inline edit toggle, collapse-when-empty subsections, right-aligned ledger footer with primitive marks (dagger, diamond), ornamental dividers between sections. Use when user says "redo as ledger chapter", "convert to ledger style", "make it look like This Order", references the #273 destination spec or `palette.md`, or asks to rebuild a religion/civilization/blessing/holysite info pane in the manuscript style.
---

# Ledger Chapter Redesign

Convert a Divine Ascension renderer to the manuscript-chapter look pioneered by `ReligionInfoRenderer` (PR for #309). The page should read top-to-bottom like a chapter in an illuminated codex.

## Workflow

Execute autonomously. Stop only when a data field needed by the spec is missing and would require a server/data change (then ask before going out of scope).

0. **Branch from `origin/master`.** Before any edits: `git fetch origin master && git checkout -b <topic> origin/master`. Pick `<topic>` from the issue (e.g. `ledger-chapter-316`). Never stack on the current feature branch unless user says so.

1. **Read the spec source.** If the user names a GitHub issue, `gh issue view <n>` — pull the target layout, pain points list, and acceptance criteria. Read `docs/topics/ui-design/palette.md` and `docs/topics/ui-design/design-spec.md` if either exists in the repo for color/typography rules.

2. **Locate the pane.** Find the renderer (`*Renderer.cs`), its sub-renderers under a sibling folder, the view model (`*ViewModel.cs`), the state class (`*State.cs`), the event union (`*Event.cs`), and the state-manager reducer (`*StateManager.cs`). Read each top-to-bottom.

3. **Map the chapter sections.** Translate the spec's mockup into a top-to-bottom list:
   - Title strip (left chapter title; right entity name; **domain glyph on the right** via `DomainGlyphRenderer`; founder pencil at the far right)
   - Auto-generated prose intro sentence built from existing VM data
   - Dotted-leader stat block via `ChromeRenderer.DrawLeader`
   - Ornamental divider via `ChromeRenderer.DrawDivider`
   - Prose body section (default) with `✎` pencil toggle → inline `TextInput.DrawMultiline` + Save (hidden until dirty) / Cancel
   - Founder-only collapsible subsection (single italic line when empty; full list when present)
   - Right-aligned footer actions (destructive action carries a dagger primitive, no red tint)

4. **Plumb data, don't invent it.** If the prose intro / stat row references a field not on the VM:
   - Prefer reading from an existing manager via the state-manager that builds the VM.
   - Extend the VM constructor + `With*` clones + `Loading()` factory; keep the property names alphabetised/grouped the way the struct already is.
   - Update the **one** construction site (the `DrawXxx` method on the state manager).
   - If the field truly doesn't exist (no founding-month timestamp, etc.) and the issue's "Out of scope" forbids data changes, omit it and write the prose around what *is* available. Don't add network packets.

5. **Add the missing event/state for inline edit toggles.** Pattern:
   - `InfoEvent.EditXxxOpen`, `EditXxxCancel`. `SaveXxxClicked` likely already exists.
   - `InfoState.IsEditingXxx` boolean + `Reset()` clears it.
   - Reducer cases: Open → set true + seed buffer from current value; Cancel → clear; Save → existing logic + clear `IsEditingXxx`.
   - VM gets `bool IsEditingXxx { get; }`; render branches on it.

6. **Use primitives, never font glyphs.** The bundled ImGui font has no Dingbats / General Punctuation coverage — `✎`, `†`, `✦` render as `?`. Always use:
   - `ChromeRenderer.DrawPencil(drawList, cx, cy, size, color)` for `✎`.
   - `ChromeRenderer.DrawDagger(drawList, cx, cy, size, color)` for `†`.
   - `ChromeRenderer.DrawDivider(drawList, x, y, width)` for `── ✦ ──`.
   - `DomainGlyphRenderer.Draw(drawList, domain, min, max, color?)` for deity-domain marks (hammer / leaf / swords / wheat / mountain).
   - For a glyph-as-button: empty-label `ButtonRenderer.DrawButton(...)` for the hit + frame, then paint the primitive on top centered in the button rect with `ColorPalette.LightText` (dark button surface → cream ink per palette §5).
   - For a label + adjacent glyph (e.g., `Disband †`): pass plain `"Disband"` to the button so it centers normally, then paint the primitive at `cursor + (width + labelWidth) / 2 + padding` and clamp to button bounds.

7. **Honor `palette.md` (Iron Gall).** Cross-check every color choice:
   - Body prose on parchment → `ColorPalette.White` (= ink). Empty-state placeholder → `ColorPalette.Grey`.
   - Founder name / rubric → `ColorPalette.Vermilion`.
   - Prestige bar fill → `ColorPalette.Lapis`; background → `ColorPalette.TableBackground` (folded edge).
   - Section sub-headings can use `Gold` sparingly (titles only).
   - Pass `Grey`/`White` *explicitly* to `TextRenderer.DrawInfoText` — its default is a hardcoded light-grey `Vector4` that drifts the palette.
   - Never `new Vector4(...)` inline for fills/borders/text; use a palette constant.

8. **Section spacing.** Use `ChromeRenderer.DrawDivider` between every chapter section (header→body, body→footer subsections). The footer doesn't need a leading divider — the page-turn indicator below the page provides the closure.

9. **Collapse-when-empty.** Subsections that reserve a panel even when empty (banned list, attachments, etc.) should render as a single italic-feeling line in `Grey` instead. Reserve panel space only when the list has rows.

10. **Footer actions.** Right-align. Compute from the right edge: `cursor = x + width; cursor -= width; draw; cursor -= gap`. Destructive action carries the dagger primitive, not a red tint.

11. **Update `ComputeContentHeight`.** The pane is scrollable — keep the height estimate honest (header height + prose lines + stat rows + divider height × N + body block + footer). Wrong heights kill the scrollbar.

12. **Localization.** Every visible string is a `LocalizationKeys.*` constant resolved via `LocalizationService.Instance.Get(...)`. Add new keys to `Constants/LocalizationKeys.cs` and English values to `assets/divineascension/lang/en.json` in the same order. Use `{0}`-style format args for substitutions (Founder name, member count, etc.).

13. **Remove what moves out.** If the spec says a control (member list, invite, etc.) moves to a sibling chapter, delete the renderer calls and `ComputeContentHeight` rows for it. Leave the underlying VM data alone — the sibling page will consume it.

14. **Build + test.** `dotnet build DivineAscension.sln -c Debug` then `dotnet test`. Both must be green before reporting done.

15. **Commit + PR only when asked.** Conventional Commits prefix `feat(ui):` for a new chapter, `refactor(ui):` for restyling without behavior change. No `Co-Authored-By` trailers ([[feedback_no_coauthor]]).

## Rules

- **No new texture assets, no new fonts, no Unicode glyphs outside Latin-1.** Everything ornamental is a `drawList` primitive.
- **No data/network changes** unless the issue explicitly allows it. If a prose field needs a timestamp the VM doesn't carry, simplify the prose; don't extend the packet.
- **One renderer per section.** Headers, prose body, ledger subsections, and footer actions each get their own static renderer under `Renderers/<Area>/Info/` (or equivalent). The top-level renderer composes them and owns scrolling + overlays.
- **Pure renderers.** `ViewModel + ImDrawListPtr → RenderResult { events, height }`. State mutation lives in the reducer.
- **Server is authoritative.** Permission checks (`IsFounder`, role gates) are mirrored in the renderer to hide controls; never trust the VM as the source of truth.

## Reference implementation

`DivineAscension/GUI/UI/Renderers/Religion/ReligionInfoRenderer.cs` and siblings under `Religion/Info/` — the "This Order" chapter shipped in PR #338 (closes #309) is the canonical example. Copy the structure, swap the area-specific bits.
