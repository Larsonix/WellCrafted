# WellCrafted Plugin v1.3.0

A modular ExileCore2 plugin for Path of Exile 2 that analyzes Well of Souls choices, maps visible modifiers to hidden outcomes, provides intelligent scoring with user-configurable profiles, and renders an overlay to help make optimal waystone crafting decisions.

## Architecture Overview

This plugin has been completely refactored from a monolithic single-file approach to a clean, modular architecture that follows separation of concerns principles. The new structure makes the codebase maintainable, extensible, and easier to debug.

## File Structure

```
Plugins/Source/WellCrafted/
├── WellCraftedPlugin.cs              # Main plugin entry point & lifecycle
├── WellCrafted.csproj                # Project file
├── README.md                         # This file
│
├── Settings/
│   ├── WellCraftedSettings.cs        # Root settings configuration
│   ├── ProfilesUiSettings.cs         # UI colors, thresholds, table sizing
│   └── OverlaySettings.cs            # Overlay toggles, font size, position
│
├── Profiles/
│   ├── ProfileModel.cs               # WeightProfile & ProfilesFile models
│   ├── ProfilesStore.cs              # JSON load/save, schema migration
│   └── ProfilesService.cs            # CRUD operations, active profile mgmt
│
├── Mapping/
│   ├── HiddenMapping.cs              # Built-in mapping table + utilities
│   └── HiddenMappingLoader.cs        # External JSON merge + diagnostics
│
├── Scoring/
│   ├── ScoringRules.cs               # Core scoring algorithms & normalization
│   └── ColorRules.cs                 # Color selection based on thresholds
│
├── UI/
│   ├── PanelRoot.cs                  # Main window controller
│   ├── ProfilesPanel.cs              # Profiles management UI
│   └── Tables/
│       └── WeightsTableRenderer.cs   # ImGui table rendering utilities
│
├── Overlay/
│   └── OverlayRenderer.cs            # In-game overlay rendering
│
├── Diagnostics/
│   └── Logger.cs                     # Centralized logging wrapper
│
└── Util/
    ├── Guard.cs                      # Defensive programming utilities
    ├── JsonUtil.cs                   # JSON serialization helpers
    └── ImGuiHelpersEx.cs            # Safe ImGui wrapper patterns
```

## Core Features

### 1. **Profile System**
- Multiple user-configurable profiles with weight preferences
- Per-panel multipliers for Default/Desecrated/Hidden modifiers
- Schema versioning with automatic migration from v1 to v2
- Profile CRUD operations (Create, Rename, Delete, Clone)
- Automatic backup system (keeps 3 most recent)

### 2. **Scoring System**
- Weight range: -11 to +10 (where -11 = BANNED = negative infinity)
- Sign-aware panel multipliers: `scaled = weight * (1 + multiplier * sign(weight))`
- Hidden modifier aggregation (currently average, extensible for min/softmin)
- Hard veto system: any banned modifier makes choice unpickable
- Configurable color thresholds for visual feedback

### 3. **Hidden Mapping**
- Built-in mapping database for visible → hidden modifier relationships
- External JSON file support for user customizations
- Fuzzy matching with text normalization
- Merge system: built-in ← seeds ← user (user has highest priority)
- Real-time mapping editor with import/export capabilities

### 4. **Overlay System**
- Compact single-line display: `HiddenMods — Score`
- Configurable positioning, text size, pixel snapping
- Hover highlighting and optional visible text echo
- Score badge with banned/numeric display
- Debug rectangle visualization

### 5. **UI Components**
- Clean tabbed interface for Default/Desecrated/Hidden weights
- Color-coded sliders with "Banned" display for -11 values
- Row background coloring that matches overlay colors
- Real-time score preview and threshold visualization
- Appearance customization (colors, thresholds)

## Key Design Principles

### **Data Safety**
- Never wipe user profiles during updates
- Schema migration preserves existing data
- Automatic backup system before saves
- Defensive programming with null checks and fallbacks

### **Scoring Consistency**
- Same color logic used for overlay, sliders, and row backgrounds
- All text normalization goes through single `ScoringRules.Normalize()` method
- Banned logic (-11 = negative infinity) enforced consistently
- Score thresholds user-configurable via settings

### **Modularity**
- Clear separation between data, business logic, and UI
- Services communicate through well-defined interfaces
- Easy to extend with new aggregation methods or automation features
- Comprehensive error handling with graceful degradation

### **Performance**
- TimeCache for game state snapshots (100ms refresh)
- Efficient text normalization with regex compilation
- Minimal ImGui state management
- Selective UI updates only when necessary

## Configuration Files

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

## Future Extensions

The architecture is designed to easily accommodate:

- **Automation Module**: Hotkey-driven reroll automation with safety checks
- **Advanced Aggregation**: Min, SoftMin(k), weighted average for hidden mods  
- **CSV Export**: Current filtered view export for external analysis
- **Test Harness**: Automated scoring invariant verification
- **Plugin Bridge**: Integration with other ExileCore2 plugins

## Version History

- **v1.3.0**: Complete architectural refactor to multi-file modular design
- **v1.2.8**: Last stable monolithic version (baseline reference)
- **v1.2.x**: Added panel multipliers, banned (-11) logic, schema v2
- **v1.1.x**: Basic profile system, hidden mapping
- **v1.0.x**: Initial overlay and scoring system
