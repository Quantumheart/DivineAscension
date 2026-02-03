# Milestone Configuration

Server administrators can customize civilization milestones by modifying the JSON configuration file.

## Overview

Milestones are achievements that civilizations complete to advance their rank and earn bonuses. There are two types:

- **Major milestones**: Advance civilization rank by 1 and provide permanent bonuses
- **Minor milestones**: Provide one-time prestige payouts (and sometimes temporary bonuses)

## Configuration File

Location: `assets/divineascension/config/milestones.json`

The file is loaded at server startup. Changes require a server restart to take effect.

## Milestone Structure

Each milestone in the `milestones` array has the following structure:

```json
{
  "id": "unique_identifier",
  "name": "Display Name",
  "description": "Description shown to players",
  "type": "major",
  "trigger": {
    "type": "trigger_type",
    "threshold": 2
  },
  "rankReward": 1,
  "prestigePayout": 250,
  "permanentBenefit": {
    "type": "benefit_type",
    "amount": 0.05
  }
}
```

### Required Fields

| Field | Type | Description |
|-------|------|-------------|
| `id` | string | Unique identifier (lowercase, underscores) |
| `name` | string | Display name shown in UI |
| `description` | string | Description shown to players |
| `type` | string | `"major"` or `"minor"` |
| `trigger` | object | Trigger conditions (see below) |
| `rankReward` | integer | Rank increase (typically 1 for major, 0 for minor) |
| `prestigePayout` | integer | One-time prestige award |

### Optional Fields

| Field | Type | Description |
|-------|------|-------------|
| `permanentBenefit` | object | Permanent bonus (see below) |
| `temporaryBenefit` | object | Temporary bonus with duration (see below) |

## Trigger Types

| Trigger Type | Description | Threshold Meaning |
|--------------|-------------|-------------------|
| `religion_count` | Number of religions in civilization | Count |
| `domain_count` | Number of unique deity domains | Count (1-4) |
| `holy_site_count` | Total holy sites across all religions | Count |
| `holy_site_tier` | Highest tier reached by any holy site | Tier (1-3) |
| `ritual_count` | Total rituals completed | Count |
| `member_count` | Total members across all religions | Count |
| `diplomatic_relationship` | Has NAP or Alliance with another civ | 1 = true |
| `war_kill_count` | PvP kills during active wars | Count |
| `all_major_milestones` | All major milestones completed | Count (typically 5) |

## Benefit Types

### Permanent Benefits

| Benefit Type | Description | Amount Format |
|--------------|-------------|---------------|
| `prestige_multiplier` | Bonus to all prestige gains | Decimal (0.05 = +5%) |
| `favor_multiplier` | Bonus to all favor gains | Decimal (0.10 = +10%) |
| `all_rewards_multiplier` | Bonus to all rewards | Decimal (0.02 = +2%) |
| `conquest_multiplier` | Bonus to conquest-related rewards | Decimal (0.05 = +5%) |
| `holy_site_slot` | Additional holy site slots | Integer (1 = +1 slot) |
| `unlock_blessing` | Unlocks a civilization blessing | Set `blessingId` field |

### Temporary Benefits

Temporary benefits include an additional `durationDays` field:

```json
"temporaryBenefit": {
  "type": "conquest_multiplier",
  "amount": 0.05,
  "durationDays": 7
}
```

## Examples

### Adding a New Major Milestone

```json
{
  "id": "grand_temple",
  "name": "Grand Temple",
  "description": "Upgrade any holy site to Tier 3 (Cathedral).",
  "type": "major",
  "trigger": {
    "type": "holy_site_tier",
    "threshold": 3
  },
  "rankReward": 1,
  "prestigePayout": 600,
  "permanentBenefit": {
    "type": "favor_multiplier",
    "amount": 0.15
  }
}
```

### Adding a New Minor Milestone

```json
{
  "id": "first_blood",
  "name": "First Blood",
  "description": "Achieve your first PvP kill during a war.",
  "type": "minor",
  "trigger": {
    "type": "war_kill_count",
    "threshold": 1
  },
  "rankReward": 0,
  "prestigePayout": 50,
  "temporaryBenefit": {
    "type": "conquest_multiplier",
    "amount": 0.10,
    "durationDays": 1
  }
}
```

### Modifying an Existing Milestone

To increase the prestige payout for "First Alliance":

```json
{
  "id": "first_alliance",
  "name": "First Alliance",
  "description": "Form an alliance by adding a second religion to your civilization.",
  "type": "major",
  "trigger": {
    "type": "religion_count",
    "threshold": 2
  },
  "rankReward": 1,
  "prestigePayout": 500,
  "permanentBenefit": {
    "type": "prestige_multiplier",
    "amount": 0.05
  }
}
```

## Validation

The milestone loader validates:

- All required fields are present
- Trigger types are recognized
- Benefit types are recognized
- Major milestones have `rankReward: 1`
- Minor milestones have `rankReward: 0`

Invalid milestones are logged as warnings and skipped.

## Default Milestones

The mod ships with 9 default milestones (5 major, 4 minor). See the [Player Guide](../../PLAYER_GUIDE.md#milestones--ranks) for the complete list with triggers and rewards.

## Related Documentation

- [Player Guide - Milestones & Ranks](../../PLAYER_GUIDE.md#milestones--ranks)
- [Rank System Reference - Civilization Ranks](../reference/rank_system_reference.md#civilization-ranks)
