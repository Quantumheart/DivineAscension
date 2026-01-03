# Divine Ascension UI Design Specification

Complete design documentation for the Divine Ascension user interface system.

---

## 1. WINDOW SPECIFICATIONS

### Main Window Dimensions
- **Base Width:** 1400px
- **Base Height:** 900px
- **Minimum Margin from Screen Edges:** 128px (64px per side)
- **Actual Size:** `Math.Min(BaseWidth, screenWidth - 128)` × `Math.Min(BaseHeight, screenHeight - 128)`
- **Position:** Centered on screen viewport
- **Source:** `/DivineAscension/GUI/GuiDialog.cs` (lines 25-26, 198-212)

### Window Configuration
- **Window Rounding:** 0px (no rounded corners)
- **Window Border Size:** 0px (no visible border)
- **Window Padding:** 0px (Vector2.Zero)
- **Frame Rounding:** 0px
- **Hotkey:** Shift+G (toggle)
- **Close Key:** ESC

### Window Flags
- `NoTitleBar` - No title bar
- `NoResize` - Fixed size, user cannot resize
- `NoMove` - Fixed position, user cannot drag
- `NoScrollbar` - No scrollbar on main window
- `NoScrollWithMouse` - Scroll wheel doesn't affect main window

### Window Colors
- **Background:** `rgba(0.16, 0.12, 0.09, 1.0)` - Dark brown #291e16
- **Frame Border:** `rgba(0.24, 0.18, 0.13, 1.0)` - Lighter brown #3d2e20, 4px thickness

---

## 2. COLOR PALETTE

All colors defined in `/DivineAscension/GUI/UI/Utilities/ColorPalette.cs`

### Primary Colors
| Color | RGBA | Hex | Usage |
|-------|------|-----|-------|
| **Gold** | `(0.996, 0.682, 0.204, 1.0)` | #feae34 | Primary accent, selected states, highlights |
| **White** | `(0.9, 0.9, 0.9, 1.0)` | #e5e5e5 | Primary text color |
| **Grey** | `(0.573, 0.502, 0.416, 1.0)` | #92806a | Secondary text, disabled states |

### Background Colors
| Color | RGBA | Hex | Usage |
|-------|------|-----|-------|
| **DarkBrown** | `(0.24, 0.18, 0.13, 1.0)` | #3d2e20 | Panel backgrounds, component backgrounds |
| **LightBrown** | `(0.35, 0.26, 0.19, 1.0)` | #59422f | Hover states, lighter panels |
| **Background** | `(0.16, 0.12, 0.09, 0.95)` | #291e16 @ 95% | Main window background |

### State/Feedback Colors
| Color | RGBA | Usage |
|-------|------|-------|
| **Red** | `(0.8, 0.2, 0.2, 1.0)` | Error messages, danger actions |
| **Green** | `(0.2, 0.8, 0.2, 1.0)` | Success messages, valid states |
| **Yellow** | `(0.8, 0.8, 0.2, 1.0)` | Warning messages |

### Overlay Colors
| Color | RGBA | Usage |
|-------|------|-------|
| **BlackOverlay** | `(0, 0, 0, 0.8)` | Modal background (80% opacity) |
| **BlackOverlayLight** | `(0, 0, 0, 0.7)` | Lighter overlay (70% opacity) |

### Blessing Node State Colors
| Color | RGBA | Hex | Usage |
|-------|------|-----|-------|
| **ColorLocked** | `(0.573, 0.502, 0.416, 1.0)` | #92806a | Locked blessings (grey) |
| **ColorUnlockable** | `(0.478, 0.776, 0.184, 1.0)` | #7ac62f | Available to unlock (lime green) |
| **ColorUnlocked** | `(0.996, 0.682, 0.204, 1.0)` | #feae34 | Already unlocked (gold) |
| **ColorSelected** | `(1.0, 1.0, 1.0, 1.0)` | #ffffff | Selected border (white) |
| **ColorHover** | `(0.8, 0.8, 1.0, 1.0)` | #cccfff | Hover state (light blue tint) |

### Color Modification Functions
- **Darken(color, factor=0.7)** - Multiplies RGB by factor
- **Lighten(color, factor=1.3)** - Multiplies RGB by factor (capped at 1.0)
- **WithAlpha(color, alpha)** - Changes alpha channel

---

## 3. TYPOGRAPHY

### Font Family
- **Typeface:** ImGui default font (no custom font loaded)
- **Rendering:** Via ImGui.AddText() with specific font sizes

### Font Sizes by Context
| Context | Size (px) | Color | File |
|---------|-----------|-------|------|
| **Dialog Header** | 20f | Gold | ReligionCreateRenderer.cs |
| **Section Header** | 16f | Gold | DiplomacyTabRenderer.cs |
| **Label (Standard)** | 14f | White | TextRenderer.DrawLabel() |
| **Dropdown Text** | 13f | White | Dropdown.cs |
| **Table Header** | 13f | Grey | DiplomacyTabRenderer.cs |
| **Table Data** | 13f | White | DiplomacyTabRenderer.cs |
| **Status Messages** | 13f | Red/Green/Yellow | TextRenderer.cs |
| **Info Text** | 12f | Grey | TextRenderer.DrawInfoText() |
| **Icon Initials** | 16f | White | BlessingNodeRenderer.cs |

### Text Rendering Methods
**Source:** `/DivineAscension/GUI/UI/Utilities/TextRenderer.cs`

1. **DrawLabel(fontSize=14f, color=White)**
   - Standard label text
   - White text on transparent background
   - Usage: Field labels, section headers

2. **DrawInfoText(fontSize=12f, color=Grey)**
   - Word-wrapped description text
   - Grey color for secondary information
   - Line height: `fontSize + 6f` (18px total)
   - Automatic word wrapping on space

3. **DrawErrorText(fontSize=13f, color=Red)**
   - Error messages
   - Red color for visibility

4. **DrawSuccessText(fontSize=13f, color=Green)**
   - Success confirmations
   - Green color

5. **DrawWarningText(fontSize=13f, color=Yellow)**
   - Warning messages
   - Yellow color

### Line Heights
- **Standard:** `fontSize + 6f` pixels
- **Tooltip Line Height:** 16f pixels
- **Line Spacing:** 6f pixels between lines

---

## 4. LAYOUT STRUCTURE

### Main Layout Hierarchy
```
MainDialogRenderer.Draw()
├─ Header Section (130px)
│  └─ ReligionHeaderRenderer
├─ Tab Bar (36px)
│  └─ Religion | Blessings | Civilization
└─ Content Area (remaining height)
   └─ Tab-specific renderers
```

### Layout Dimensions
| Element | Value | Description |
|---------|-------|-------------|
| **Base Padding** | 16px | Standard outer padding (left/right/top) |
| **Tab Height** | 36px | Height of main tab bar |
| **Header Height** | 130px | Religion header height (base) |
| **Close Button Size** | 24px | Top-right close button (square) |
| **Content Area Width** | `windowWidth - 32px` | Minus left/right padding |
| **Content Area Height** | `windowHeight - headerHeight - tabHeight - padding` | Remaining space |

### Position Calculations
```csharp
x = 16f                    // Left padding
y = 16f                    // Top padding
width = windowWidth - 32f  // Full width minus padding
contentY = headerHeight + tabHeight + 8f
```

---

## 5. TAB SYSTEM

### Main Tabs (3 total)
1. **Religion** (temple icon)
2. **Blessings** (meditation icon)
3. **Civilization** (castle icon)

**Dimensions:**
- **Tab Width:** `(windowWidth / 3) - tabSpacing` (equal distribution)
- **Tab Height:** 36px
- **Tab Spacing:** 4px (gap between tabs)
- **Icon Size:** 20px
- **Icon Padding Left:** 8px
- **Icon to Text Spacing:** 6px

**Tab States:**
| State | Background | Border | Text | Border Width |
|-------|------------|--------|------|--------------|
| Selected | `Gold * 0.4` | Gold | White | 2px |
| Hover | LightBrown | `Grey * 0.5` | Grey | 1px |
| Normal | DarkBrown | `Grey * 0.5` | Grey | 1px |

### Sub-Tabs

**Religion Sub-Tabs:**
- Browse, Info, Activity, Roles, Invites, Create
- **Tab Width:** 130px (fixed)
- **Tab Spacing:** 6px

**Civilization Sub-Tabs:**
- Browse, Info, Invites, Create, Diplomacy
- **Tab Width:** 150px (fixed)
- **Tab Spacing:** 6px

---

## 6. COMPONENT SPECIFICATIONS

### 6.1 BUTTONS

#### Standard Button
**Source:** `/DivineAscension/GUI/UI/Components/Buttons/ButtonRenderer.cs`

**Dimensions:**
- **Width:** Variable (passed as parameter)
- **Height:** Variable (passed as parameter, typically 36px)
- **Border Radius:** 4px
- **Border Width:** 1.5px

**Spacing:**
- **Padding Left:** 8px
- **Padding Right:** 8px
- **Icon to Text Spacing:** 6px (when icon present)
- **Icon Size:** 20px

**States:**
| State | Background | Border | Text | Cursor |
|-------|------------|--------|------|--------|
| Normal | `baseColor * 0.8` | `Gold * 0.7` | White | Default |
| Hover | `baseColor * 1.2` | `Gold * 0.7` | White | Hand |
| Active | `baseColor * 0.7` | `Gold * 0.7` | White | Hand |
| Disabled | `DarkBrown * 0.5` | `Grey * 0.3` | `Grey * 0.7` | Default |

#### Small Button
- **Border Radius:** 4px
- **Border Width:** 1px
- **Text Color:** Grey (normal), White (hover)
- **Background:** DarkBrown (normal), Red (hover for dangerous actions)
- **Typical Dimensions:** 60-140px × 20-24px

#### Close Button
- **Size:** 24px × 24px (square)
- **Border Radius:** 4px
- **Background:** DarkBrown (normal), LightBrown (hover)
- **X Mark Color:** Grey (normal), White (hover)
- **X Line Width:** 2px
- **X Padding from Edges:** 25% of button size

---

### 6.2 INPUT COMPONENTS

#### Text Input (Single-Line)
**Source:** `/DivineAscension/GUI/UI/Components/Inputs/TextInput.cs`

**Dimensions:**
- **Height:** 32px (typical)
- **Border Radius:** 4px
- **Border Width:** 1px

**Spacing:**
- **Text Padding Left:** 8px
- **Text Vertical Alignment:** `(height - 16) / 2`

**Colors:**
- **Background:** `DarkBrown * 0.7`
- **Border:** `Grey * 0.5`
- **Text Color:** White
- **Placeholder Color:** `Grey * 0.7`
- **Cursor Color:** White
- **Cursor Width:** 2px

**Behavior:**
- **Max Length:** 200 characters (default)
- **Cursor Blink Rate:** 2 Hz
- **Mouse Cursor:** TextInput on hover/active

#### Text Input (Multi-Line)
- **Height:** Variable (passed as parameter)
- **Text Padding:** 8px (left and top)
- **Line Height:** ~16px
- **Max Length:** 500 characters (default)
- **Supports:** Line breaks with Enter key
- **All other styling:** Same as single-line

---

### 6.3 CHECKBOX

**Source:** `/DivineAscension/GUI/UI/Components/Inputs/CheckboxRenderer.cs`

**Dimensions:**
- **Size:** 20px × 20px (square)
- **Border Radius:** 3px
- **Border Width:** 1.5px
- **Label Padding:** 8px (from checkbox to text)

**States:**
| State | Background | Border | Checkmark |
|-------|------------|--------|-----------|
| Unchecked | `DarkBrown * 0.7` | `Grey * 0.5` | None |
| Unchecked Hover | `LightBrown * 0.7` | `Grey * 0.5` | None |
| Checked | `DarkBrown * 0.7` | Gold | Gold, 2px width |
| Checked Hover | `LightBrown * 0.7` | Gold | Gold, 2px width |

---

### 6.4 DROPDOWN

**Source:** `/DivineAscension/GUI/UI/Components/Inputs/Dropdown.cs`

#### Dropdown Button
- **Border Radius:** 4px
- **Border Width:** 1px
- **Border Color:** `Grey * 0.5`
- **Text Padding Left:** 12px
- **Text Color:** White
- **Font Size:** 13px (customizable)
- **Background:** `DarkBrown * 0.7` (normal), `LightBrown * 0.7` (hover)
- **Arrow Position:** `x + width - 20`
- **Arrow Color:** Grey

#### Dropdown Menu
- **Item Height:** 40px (default)
- **Menu Margin from Button:** 2px
- **Background:** ColorPalette.Background
- **Border Width:** 2px
- **Border Color:** `Gold * 0.7`
- **Text Padding Left:** 12px
- **Font Size:** 13px (customizable)

**Menu Item States:**
| State | Background |
|-------|------------|
| Normal | Transparent |
| Hover | `LightBrown * 0.6` |
| Selected | `DarkBrown * 0.8` |

---

### 6.5 PROGRESS BAR

**Source:** `/DivineAscension/GUI/UI/Renderers/Components/ProgressBarRenderer.cs`

**Dimensions:**
- **Width:** Variable
- **Height:** Variable (typically 14-20px)
- **Border Radius:** 4px
- **Border Width:** 1px (normal), 2px (glow)

**Colors:**
- **Border:** `Gold * 0.5`
- **Fill Color:** Customizable (passed as parameter)
- **Background Color:** Customizable (passed as parameter)

**Glow Effect:**
- **Triggers When:** Progress > 80% and showGlow=true
- **Animation:** `Sin(ImGui.GetTime() * 3.0) * 0.3 + 0.7`
- **Alpha Range:** 0.4 to 1.0
- **Border Width:** 2px

**Label:**
- **Color:** White
- **Position:** Centered horizontally and vertically

---

### 6.6 TABLES

#### Diplomacy Table
**Source:** `/DivineAscension/GUI/UI/Renderers/Civilization/DiplomacyTabRenderer.cs`

**Row Specifications:**
- **Row Height:** 24px
- **Alternating Row Background:** `rgba(0.15, 0.15, 0.15, 0.3)` (even rows only)

**Column Layout:**
| Column | X Position | Width | Alignment | Content |
|--------|-----------|-------|-----------|---------|
| Civilization | 0 | 220px | Left | Civilization name |
| Status | 220 | 200px | Left | Diplomatic status |
| Established | 420 | 140px | Right | Date (MM/dd/yy) |
| Expires | 560 | 140px | Right | Expiry date |
| Violations | 700 | 120px | Center | Count/Max |
| Actions | 820 | Variable | Left | Buttons |

**Header:**
- **Font Size:** 13px
- **Color:** Grey
- **Height:** 24px (same as row)

**Data:**
- **Font Size:** 13px
- **Color:** White (default), varies by content
- **Text Clipping:** PushClipRect/PopClipRect on all columns

**Spacing:**
- **Section Spacing:** 20px (between sections)
- **Column Gap:** 10px (safety margin for clipping)

---

### 6.7 LISTS

#### Scrollable List Components
**Common Specifications:**
- **Scrollbar Width:** 16px
- **Border Radius:** 4px
- **List Background:** `DarkBrown * 0.5`

#### Member List
- **Item Height:** 30px
- **Item Spacing:** 4px
- **Item Padding:** 8px
- **Item Background:** `DarkBrown * 0.8`
- **Button Width:** 50px
- **Button Spacing:** 5px
- **Scroll Speed:** 30px per wheel tick

#### Religion List
- **Item Height:** 80px
- **Item Spacing:** 8px
- **Item Padding:** 12px
- **Icon Size:** 48px
- **Icon Border Width:** 2px
- **Scroll Speed:** 40px per wheel tick

**Item States:**
| State | Background | Border Width | Border Color |
|-------|------------|--------------|--------------|
| Normal | DarkBrown | 1px | `Grey * 0.5` |
| Hover | `LightBrown * 0.7` | 1px | `Grey * 0.5` |
| Selected | `Gold * 0.3` | 2px | Gold |

**Text Layout (from top):**
- **Religion Name:** 16px font, Gold, at `padding + iconSize + padding` from left
- **Deity Info:** 13px font, White, 22px from top
- **Member Count:** 12px font, Grey, 42px from top

#### Ban List
- **Item Height:** 40px (taller for 2 lines)
- **Item Spacing:** 4px
- **Item Padding:** 8px
- **Button Width:** 60px
- **Scroll Speed:** 30px per wheel tick

---

### 6.8 TOOLTIPS

**Source:** Various renderers (e.g., ReligionListRenderer)

**Dimensions:**
- **Max Width:** 300px
- **Padding:** 12px
- **Line Spacing:** 6px
- **Line Height:** 16px
- **Border Radius:** 4px
- **Border Width:** 2px

**Colors:**
- **Border:** `Gold * 0.6`
- **Background:** DarkBrown

**Typography:**
| Element | Font Size | Color |
|---------|-----------|-------|
| Title | 16px | Gold |
| Subtitle | 13px | White |
| Section Headers | 12px | Grey |
| Body Text | 13px | White |

**Positioning:**
- **Offset from Mouse:** 16px (X and Y)
- **Edge Detection:** Flips to opposite side if extending past window
- **Minimum Margin:** 4px from window edges

---

### 6.9 ERROR BANNER

**Source:** `/DivineAscension/GUI/UI/Components/ErrorBannerRenderer.cs`

**Dimensions:**
- **Height:** 44px (default)
- **Border Radius:** 6px
- **Border Width:** 1.5px
- **Padding Right:** 10px (for action buttons)

**Colors:**
- **Background:** `Red * 0.35`
- **Border:** `Red * 0.8`

**Icon:**
- **Position:** x + 16px, centered vertically
- **Radius:** 10px
- **Color:** `Red * 0.8`
- **Text:** "!" centered in circle

**Message:**
- **Position:** x + 36px, centered vertically
- **Color:** White
- **Font Size:** 14px

**Action Buttons:**
- **Width:** 86px
- **Height:** 28px
- **Spacing:** 8px
- **Vertical Alignment:** Centered
- **Dismiss Button Color:** DarkBrown
- **Retry Button Color:** `Gold * 0.8`

**Consumed Height:** height + 8px (includes spacing)

---

## 7. BLESSING TREE LAYOUT

**Source:** `/DivineAscension/GUI/BlessingTreeLayout.cs`

### Node Specifications
- **Node Width:** 64px
- **Node Height:** 64px
- **Border Radius:** 4px
- **Border Width:** 2px (normal), 3px (selected)

### Spacing
- **Vertical Spacing (between tiers):** 120px
- **Horizontal Spacing (nodes in tier):** 100px
- **Top Padding:** 40px
- **Left Padding:** 40px

### Glow Effect (Unlockable Nodes)
- **Animation Type:** Sine wave pulse
- **Period:** 2 seconds (π radians)
- **Alpha Range:** 0.3 to 1.0 (synchronized across all unlockable nodes)
- **Glow Padding:** 8px around node

### Node States
| State | Border Color | Background |
|-------|--------------|------------|
| Locked | Grey (#92806a) | Semi-transparent |
| Unlockable | Lime Green (#7ac62f) + Glow | Semi-transparent |
| Unlocked | Gold (#feae34) | Semi-transparent |
| Selected | White border (3px) | - |
| Hover | Light blue tint | - |

---

## 8. HEADER SPECIFICATIONS

**Source:** `/DivineAscension/GUI/UI/Renderers/Blessing/ReligionHeaderRenderer.cs`

### Religion Header
- **Height:** 130px (fixed)
- **Background:** Dark brown rectangle
- **Border Radius:** 4px
- **Border Width:** 2px
- **Border Color:** `Gold * 0.5`
- **Inner Padding:** 16px

### Header Layout (2-column when civilization exists)
- **Column 1:** Religion/Deity info (left)
- **Column 2:** Civilization info (right, when applicable)
- **Column Spacing:** 16px
- **Column Width:** `(innerWidth - columnSpacing) / 2`

### Icon Sizes
- **Deity Icon:** 48px × 48px
- **Civilization Icon:** 48px × 48px

---

## 9. SPACING CONSTANTS SUMMARY

| Element | Spacing Value |
|---------|---------------|
| **Base Padding (Outer)** | 16px |
| **Button Icon/Text Padding** | 8px |
| **Button Icon Spacing** | 6px |
| **Checkbox Label Padding** | 8px |
| **Input Text Padding (Left)** | 8px |
| **Dropdown Text Padding** | 12px |
| **List Item Padding** | 8-12px |
| **List Item Spacing** | 4-8px |
| **Section Spacing** | 20px |
| **Tooltip Padding** | 12px |
| **Tab Spacing (Main)** | 4px |
| **Tab Spacing (Sub)** | 6px |
| **Icon Spacing** | 6px |
| **Table Column Gap** | 10px |
| **Form Padding** | 20px |
| **Button Spacing** | 12px |

---

## 10. BORDER SPECIFICATIONS SUMMARY

| Component | Radius | Width | Color |
|-----------|--------|-------|-------|
| **Button** | 4px | 1.5px | Gold*0.7 (enabled), Grey*0.3 (disabled) |
| **Small Button** | 4px | 1px | Varies |
| **Checkbox** | 3px | 1.5px | Gold (checked), Grey*0.5 (unchecked) |
| **Text Input** | 4px | 1px | Grey*0.5 |
| **Dropdown Button** | 4px | 1px | Grey*0.5 |
| **Dropdown Menu** | 4px | 2px | Gold*0.7 |
| **List Item** | 4px | 1-2px | Grey*0.5 / Gold (selected) |
| **Progress Bar** | 4px | 1-2px | Gold*0.5 |
| **Tooltip** | 4px | 2px | Gold*0.6 |
| **Error Banner** | 6px | 1.5px | Red*0.8 |
| **Header Panel** | 4px | 2px | Gold*0.5 |
| **Blessing Node** | 4px | 2-3px | Varies by state |

---

## 11. ANIMATION SPECIFICATIONS

### Blessing Node Glow
- **Type:** Sine wave pulse
- **Period:** 2 seconds (π radians)
- **Alpha Range:** 0.3 to 1.0
- **Formula:** `0.5f + 0.5f * Sin(time * π)`
- **Applies To:** Unlockable blessings only
- **Synchronization:** All unlockable nodes pulse together

### Progress Bar Glow
- **Type:** Sine wave oscillation
- **Trigger:** Progress > 80%
- **Alpha Formula:** `Sin(time * 3.0) * 0.3 + 0.7`
- **Alpha Range:** 0.4 to 1.0
- **Speed Factor:** 3.0 (faster than node glow)

### Text Cursor Blink
- **Rate:** 2 Hz (2 blinks per second)
- **Formula:** `(ImGui.GetTime() % 2) < 1`
- **Width:** 2px
- **Color:** White

---

## 12. CURSOR STATES

| Context | Cursor Type |
|---------|-------------|
| **Buttons (hover)** | Hand |
| **Tabs (hover)** | Hand |
| **Clickable Items (hover)** | Hand |
| **Text Inputs (hover/active)** | Text Input |
| **Default Areas** | Default |
| **Disabled Elements** | Default (no change) |

---

## 13. KEY FILE LOCATIONS

### Core System
- Main Dialog: `/DivineAscension/GUI/GuiDialog.cs`
- Dialog Manager: `/DivineAscension/GUI/GuiDialogManager.cs`
- Main Layout: `/DivineAscension/GUI/UI/MainDialogRenderer.cs`

### Utilities
- Color Palette: `/DivineAscension/GUI/UI/Utilities/ColorPalette.cs`
- Text Renderer: `/DivineAscension/GUI/UI/Utilities/TextRenderer.cs`

### Components
- Button Renderer: `/DivineAscension/GUI/UI/Components/Buttons/ButtonRenderer.cs`
- Checkbox Renderer: `/DivineAscension/GUI/UI/Components/Inputs/CheckboxRenderer.cs`
- Text Input: `/DivineAscension/GUI/UI/Components/Inputs/TextInput.cs`
- Dropdown: `/DivineAscension/GUI/UI/Components/Inputs/Dropdown.cs`
- Tab Control: `/DivineAscension/GUI/UI/Components/TabControl.cs`
- Error Banner: `/DivineAscension/GUI/UI/Components/ErrorBannerRenderer.cs`
- Progress Bar: `/DivineAscension/GUI/UI/Renderers/Components/ProgressBarRenderer.cs`
- Scrollable List: `/DivineAscension/GUI/UI/Components/Lists/ScrollableList.cs`

### Renderers
- Religion Header: `/DivineAscension/GUI/UI/Renderers/Blessing/ReligionHeaderRenderer.cs`
- Blessing Node: `/DivineAscension/GUI/UI/Renderers/Blessing/BlessingNodeRenderer.cs`
- Diplomacy Tab: `/DivineAscension/GUI/UI/Renderers/Civilization/DiplomacyTabRenderer.cs`
- Member List: `/DivineAscension/GUI/UI/Renderers/Religion/MemberListRenderer.cs`
- Religion List: `/DivineAscension/GUI/UI/Renderers/Religion/ReligionListRenderer.cs`
- Ban List: `/DivineAscension/GUI/UI/Renderers/Religion/BanListRenderer.cs`

### Layout
- Blessing Tree Layout: `/DivineAscension/GUI/BlessingTreeLayout.cs`

### State Management
- Main State: `/DivineAscension/GUI/State/GuiDialogState.cs`
- Religion Tab State: `/DivineAscension/GUI/State/ReligionTabState.cs`
- Civilization Tab State: `/DivineAscension/GUI/State/CivilizationTabState.cs`
- Diplomacy State: `/DivineAscension/GUI/State/Civilization/DiplomacyState.cs`

---

## 14. DESIGN PATTERNS & CONVENTIONS

### Naming Conventions
- **Font Size Constants:** Suffix with context (e.g., `LabelSize`, `HeaderSize`)
- **Color Constants:** Pascal case (e.g., `ColorPalette.Gold`)
- **Dimension Constants:** Camel case floats (e.g., `iconSize`, `rowHeight`)

### Rendering Patterns
1. **Calculate positions** before rendering
2. **Push styles** (ImGui.PushStyleVar/PushStyleColor)
3. **Draw background** elements first
4. **Draw text/icons** on top
5. **Pop styles** after rendering
6. **Use clipping** (PushClipRect/PopClipRect) to prevent overflow

### State Management Pattern
- **State classes** hold data (e.g., DiplomacyState)
- **ViewModel classes** compute derived data
- **Renderer classes** handle drawing only
- **Events** propagate user interactions back to state

### Color Usage Pattern
```csharp
// Modify base colors for states
normalColor = baseColor * 0.8f     // Slightly darker
hoverColor = baseColor * 1.2f      // Slightly lighter
activeColor = baseColor * 0.7f     // Much darker
disabledColor = baseColor * 0.5f   // Very dark
```

### Alignment Pattern
```csharp
// Center align
x = parentX + (parentWidth - elementWidth) / 2

// Right align
x = parentX + parentWidth - elementWidth

// Vertical center
y = parentY + (parentHeight - elementHeight) / 2
```

---

## 15. ACCESSIBILITY CONSIDERATIONS

### Visual Hierarchy
- **Primary actions:** Gold buttons
- **Secondary actions:** Grey/DarkBrown buttons
- **Dangerous actions:** Red buttons
- **Text hierarchy:** 20px headers > 14px labels > 12px info

### Color Contrast
- **Primary text (White)** on **Dark Brown** background: High contrast
- **Gold accents** on **Dark Brown**: Good visibility
- **Grey text** on **Dark Brown**: Medium contrast (secondary info)

### Interactive Feedback
- **Hover states:** Lighter backgrounds, hand cursor
- **Active states:** Darker backgrounds
- **Disabled states:** Muted colors, default cursor
- **Selection:** Gold highlights

---

## END OF SPECIFICATION

This document provides complete design specifications for the Divine Ascension UI system as implemented. All values are sourced directly from the codebase and represent the current implementation as of 2026-01-03.
