# Divine Ascension - Icons Directory

This directory contains all icon assets for the Divine Ascension mod.

## Structure

```
icons/
├── deities/          # 4 deity symbols (32x32 PNG)
│   ├── khoras.png
│   ├── lysa.png
│   ├── aethra.png
│   ├── gaia.png
│
└── perks/            # 40 perk icons organized by deity
    ├── khoras/       # 10 icons for Khoras (War)
    ├── lysa/         # 10 icons for Lysa (Hunt)
    ├── aethra/       # 10 icons for Aethra (Light)
    ├── gaia/         # 10 icons for Gaia (Earth)
```

## Icon Specifications

**Format:** PNG with transparency (RGBA)
**Size:** 32x32 pixels
**Style:** Pixel art / Low-res fantasy (Vintage Story aesthetic)

## Documentation

For detailed information about icon generation, see:

- **`/docs/topics/art-assets/icon_specifications.md`** - Detailed visual descriptions for each icon
- **`/docs/topics/art-assets/icon_manifest.json`** - JSON manifest for tracking generation progress
- **`/docs/topics/art-assets/icon_generation_guide.md`** - Practical guide for generating icons

## Usage in Code

Icons are referenced using the Vintage Story asset path format:

```csharp
// Deity symbols
string khorasIcon = "divineascension:textures/icons/deities/khoras.png";

// Perk icons
string perkIcon = "divineascension:textures/icons/perks/khoras/warriors_resolve.png";
```

## Status

Total icons needed: **44**

- Deity symbols: 4
- Perk icons: 40 (10 per deity × 4 deities)

Check `/docs/topics/art-assets/icon_manifest.json` for current generation progress.
