# WellCrafted Plugin v1.7.2

An ExileCore2 plugin that analyzes Well of Souls choices, maps visible modifiers to hidden outcomes, provides intelligent scoring with user-configurable profiles, and renders an overlay to help make optimal waystone crafting decisions.


## New features in 1.7.2 (since 1.6.3)

- **Robust choice panel detection**  
  - Overlay now gates strictly on the actual 3-choices panel visibility (no longer inferred from Reveal button).  
  - Rectangles must be valid (non-zero size) before overlay draws, eliminating top-left flicker.

- **Panel generation tracking**  
  - Detects when the 3-choices panel is rebuilt (element IDs/rects change).  
  - Clears caches and restarts detection reliably on every cycle.

- **Grace window for fast interactions**  
  - New setting `ChoicesGraceMs` (default 3000 ms).  
  - During grace, overlay accepts empty text and fills in as choices populate.  
  - Prevents missed overlays when clicking quickly through Confirm → Reveal cycles.

- **Settings**  
  - `ChoicesGraceMs` exposed in the menu (0–10000 ms).  
  - Diagnostics/Profiles unchanged.


### **Profiles** (`WellCraftedProfiles.json`)
```json
{
  "schema": 2,
  "active": "Default",
  "profiles": {
    "Default": {
      "visibleDefault": { "pack size": 2.0 },
      "visibleDesecrated": { "abyss pits": 5.0 },
      "hidden": { "map item quantity": 8.0 },
      "multDefault": 0.0,
      "multDesecrated": 0.0,
      "multHidden": 0.0
    }
  }
}
```

### **Hidden Mapping** (`data/HiddenMap.user.json`)
```json
{
  "schema": 2,
  "mappings": [
    {
      "match": "Abysses lead to an Abyssal Depths",
      "hidden": ["+20% Map Item Drop Chance"]
    }
  ]
}
```

## Version History

### 1.7.2
- Added strict rectangle validity checks to eliminate flicker.


### 1.7.1
- Improved grace handling and overlay stability during rapid clicks.


### 1.7.0
- Panel visibility now keyed to actual 3-choices container.
- Panel generation flips tracked for reliable rebinds.
- Grace window setting exposed in menu.

### 1.6.3
- Normalization finalized with **multi-`#` collapse** (e.g., `1#` ≈ `#`) for reliable matches.
- Scoring parity for “overrun” combinations (visible weights now apply correctly).
- Overlay gating refined: draw only when panel **and at least one choice** are ready.

### 1.6.2
- Loader hardening:
  - Case-insensitive JSON properties.
  - Deterministic merge order (built-in ← seeds ← user).
  - Commit-after-parse to preserve last good data on errors.
- Diagnostics clarity improved: active paths and mapping counts surfaced.

### 1.6.1
- Multi-line / combined choice support:
  - Exact match → per-clause split (newline/semicolon) → safe containment fallback.
  - Merges results and de-duplicates hidden mods.

### 1.6.0
- Single-source normalization across the plugin:
  - Bracket runs keep the visible token.
  - Alternations `a|b|c` keep the last option.
  - Numbers → `#`; punctuation/whitespace unified.
- Profiles and mappings now share the same normalizer.

### 1.5.4
- Initial overlay gating so rendering happens only when the choice panel is present.
- Minor overlay polish (debug rectangles, bubble label tuning).

### 1.5.3
- Data paths clarified; seeds under `Source/WellCrafted/data` (runtime copy handled).
- Loader reads consistently from the plugin’s `data` folder.

### 1.5.2
- Diagnostics improvements: **Reload Mappings**, **Export Current**, clearer logs.
- One-shot logging for unknown hidden keys to aid mapping coverage.

### 1.5.1
- 100 ms snapshot smoothing to eliminate transient empty frames during fast interactions.
- Immediate draw preserved (no added delay).

### 1.5.0
- Baseline modular refactor (core, mapping, scoring, profiles, UI).
- Overlay with hidden mod lines and scoring.
- Profiles v2 with backups and per-tab multipliers.

# **v1.3.0**: Complete architectural refactor to multi-file modular design
# **v1.2.8**: Last stable monolithic version (baseline reference)
# **v1.2.x**: Added panel multipliers, banned (-11) logic, schema v2
# **v1.1.x**: Basic profile system, hidden mapping
# **v1.0.x**: Initial overlay and scoring system
