# Divine Ascension Documentation

This directory contains contributor documentation for the Divine Ascension mod. It is organized by topic to keep design notes, implementation guides, and references easy to find.

**New Players**: See [`PLAYER_GUIDE.md`](PLAYER_GUIDE.md) for a comprehensive guide to playing with Divine Ascension.

**Contributors**: Build and test instructions live in the root `README.md`. Start there for environment setup and test commands.

## Topic Areas

- `topics/reference/` — Reference material for game systems and content (deities, blessings, favor, icons)
- `topics/implementation/` — Implementation guides and technical deep-dives
- `topics/ui-design/` — UI design notes, migration plans, and polish checklists
- `topics/testing/` — Testing guides, task breakdowns, and summaries
- `topics/planning/` — Planning notes, phase breakdowns, and scope decisions
- `topics/integration/` — Cross-system integration notes and calculations
- `topics/analysis/` — Exploration and analysis of external mods/systems

Each folder may contain indexes (e.g., `EXPLORATION_INDEX.md`) and focused documents. Prefer browsing the folder first to discover context and related files.

## Finding Documentation

### By development activity

- Implementing a new feature → `topics/implementation/` and `topics/planning/`
- Working on UI → `topics/ui-design/`
- Writing tests → `topics/testing/`
- Integrating systems → `topics/integration/`
- Researching external mods → `topics/analysis/`

### By system

- Blessings → see `topics/implementation/` and `topics/reference/`
- Favor → see `topics/reference/` and `topics/integration/`
- Deities → see `topics/reference/`
- Buffs → see `topics/implementation/`
- UI → see `topics/ui-design/`
- Testing → see `topics/testing/`

## Contributing to documentation

When adding new docs:

1. Place the file under the appropriate topic folder
2. Name files descriptively (short, specific, hyphen-separated)
3. Cross-link related docs when helpful instead of duplicating content
4. Keep comments minimal and style consistent with the repo
5. If you add a new sub-area, add a short bullet for it above

Last updated: 2025-12-04
