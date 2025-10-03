// ===============================================
// Settings/WellCraftedSettings.cs — ordered menu
// Adds: ChoicesGraceMs timing knob
// ===============================================

using System.Windows.Forms;
using ExileCore2.Shared.Attributes;
using ExileCore2.Shared.Interfaces;
using ExileCore2.Shared.Nodes;

namespace WellCrafted.Settings
{
    public class WellCraftedSettings : ISettings
    {
        // required by ISettings
        public ToggleNode Enable { get; set; } = new ToggleNode(true);

        // ── Top controls ──────────────────────────────────────────────────────
        [Menu(null, "Show/Hide overlay UI")]
        public HotkeyNodeV2 ToggleOverlay { get; set; } = new HotkeyNodeV2(Keys.F7);

        [Menu(null, "Log a debug line once for unknown hidden matches")]
        public ToggleNode LogUnknownHidden { get; set; } = new ToggleNode(true); // checked by default

        // (breathing space comes naturally from submenu separation)

        // ── Overlay ──────────────────────────────────────────────────────────
        [Submenu] public OverlaySettings Overlay { get; set; } = new OverlaySettings();

        // ── Profiles / DevTree toggles ───────────────────────────────────────
        // Profiles UI object is hidden; expose only the toggle
        public ProfilesUiSettings ProfilesUI { get; set; } = new ProfilesUiSettings();

        [Menu(null, "Show Profiles Tab")]
        public ToggleNode ShowProfilesTab => ProfilesUI.ShowProfilesTab; // default true

        // Diagnostics container (kept for your panel logic; not rendered here)
        public DiagnosticsSettings Diagnostics { get; set; } = new DiagnosticsSettings();
// DevTree data (hidden; your UI can use it if needed)

        [Menu(null, "Show Diagnostics")]
        public ToggleNode ShowDiagnostics => Diagnostics.ShowDiagnosticsTab;

        public DevTreeSettings DevTree { get; set; } = new DevTreeSettings();

        // ── Timings ─────────────────────────────────────────────────────────
        [Menu("Grace window (ms)", "Relaxed gating after panel rebuild (0–10000 ms)")]
        public RangeNode<int> ChoicesGraceMs { get; set; } = new RangeNode<int>(3000, 0, 10000);
    }

    // Kept hidden; used only by your own panel if needed
    public class DevTreeSettings
    {
        [Menu(null, "Root type (info)")] public TextNode RootType { get; set; } = new TextNode("WellOfSoulsWindow");
        [Menu(null, "Root index")] public RangeNode<int> RootIndex { get; set; } = new RangeNode<int>(83, 0, 300);
        [Menu(null, "Reveal chain (CSV)")] public TextNode RevealPath { get; set; } = new TextNode("3,1");
        [Menu(null, "Confirm chain (CSV)")] public TextNode ConfirmPath { get; set; } = new TextNode("3,2,0");
        [Menu(null, "Reroll chain (CSV)")] public TextNode RerollPath { get; set; } = new TextNode("3,8,0");
        [Menu(null, "Choice 1 chain (CSV)")] public TextNode Choice1Path { get; set; } = new TextNode("4,0,0,0");
        [Menu(null, "Choice 2 chain (CSV)")] public TextNode Choice2Path { get; set; } = new TextNode("4,0,1,0");
        [Menu(null, "Choice 3 chain (CSV)")] public TextNode Choice3Path { get; set; } = new TextNode("4,0,2,0");
    }

    public class DiagnosticsSettings
    {
        public ToggleNode ShowDiagnosticsTab { get; set; } = new ToggleNode(false);
    }
}
