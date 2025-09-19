# WellCrafted Plugin (ExileCore2 / PoE2)

A Path of Exile 2 plugin for ExileCore2 that helps evaluate **Well of Souls** choices for waystones.  
It maps visible modifiers → hidden outcomes, scores them using user profiles, and renders an overlay to make crafting decisions easier.
Automation to come (hopefully).

---

## ✨ Features

- **Overlay**
  - Shows hidden mods and a score badge directly on the 3 waystone choices
  - Configurable text size, position offsets, colors, and bubble background
  - Optional echo of visible text
  - Debug rectangles for developers

- **Profiles**
  - Multiple weight profiles (Default / Desecrated / Hidden)
  - Adjustable weight sliders (−11 = *Banned*, +10 = *Max Favorable*)
  - Color thresholds for red/white/green feedback
  - Save / load profiles via JSON (`WellCraftedProfiles.json`)

- **Hidden Mapping**
  - Built-in database of visible → hidden mod mappings
  - Supports user overrides via JSON (`HiddenMap.user.json`)
  - Merge priority: Built-in < Seeds < User file
  - Logs unmapped lines for easy contribution

- **Settings**
  - Toggle overlay hotkey (default **F7**)
  - “Show Profiles Tab” toggle (enabled by default)
  - “Show DevTree” toggle for advanced debugging
  - Color and layout options under the Overlay submenu

📜 Version History
  - v1.3.0 — Modular refactor, overlay polish, profiles & mapping stable
  - v1.2.8 — Last “monolithic” stable version
  - v1.2.x — Added banned (−11) logic and panel multipliers
  - v1.1.x — Early profile system and hidden mapping
  - v1.0.x — Initial overlay


## 📂 File Layout

Plugins/Source/WellCrafted/
├── WellCraftedPlugin.cs # Entry point
├── Settings/ # Overlay + profile settings
├── Profiles/ # Profile model, store, service
├── Mapping/ # Hidden mod mapping
├── Scoring/ # Scoring + color rules
├── Overlay/ # OverlayRenderer
├── UI/ # Panels and ImGui tables
├── Diagnostics/ # Logger
└── Util/ # Helpers (Json, Guard, ImGuiEx)
