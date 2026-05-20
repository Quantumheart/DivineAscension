# Third-party licenses

This document records bundled third-party assets and the licenses under
which they ship inside the Divine Ascension mod.

## DejaVu Serif (font)

- **File**: `DivineAscension/assets/divineascension/gui/fonts/DejaVuSerif.ttf`
- **Project**: [DejaVu Fonts](https://dejavu-fonts.github.io/)
- **Copyright**: © 2003 Bitstream, Inc. (Bitstream Vera); DejaVu modifications
  released into the public domain by the DejaVu project; supplemental glyphs
  © 2006 Tavmjong Bah (Arev Fonts).
- **License**: Bitstream Vera Fonts Copyright (a permissive MIT-style license)
  for the Bitstream-derived portions, public domain for DejaVu modifications,
  Arev-fonts permission grant for additional glyphs.
- **License text**: Bundled alongside the font at
  `DivineAscension/assets/divineascension/gui/fonts/DejaVuSerif-LICENSE.txt`.

Used as the default ImGui font for the mod's dialog chrome; the broader glyph
coverage compared to ImGui's stock font is what lets ornament codepoints
(`✦ ⚜ ✉ ★` etc.) render in titles and dividers instead of `?` boxes.
