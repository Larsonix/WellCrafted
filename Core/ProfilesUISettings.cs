// ===============================================
// Settings/ProfilesUiSettings.cs — CLEAN
// Single source for colors/thresholds used by overlay + tables.
// Only "Show Profiles Tab" is visible in the main settings.
// ===============================================

using System.Drawing;
using ExileCore2.Shared.Attributes;
using ExileCore2.Shared.Nodes;

namespace WellCrafted.Settings
{
    public class ProfilesUiSettings
    {
        // Edited exclusively inside the Profiles panel (no [Menu] here)
        public ColorNode LowWeightColor      { get; set; } = new ColorNode(Color.FromArgb(230, 60, 60));
        public ColorNode MidWeightColor      { get; set; } = new ColorNode(Color.FromArgb(240, 240, 240));
        public ColorNode HighWeightColor     { get; set; } = new ColorNode(Color.FromArgb(60, 200, 90));
        public ColorNode FavoriteWeightColor { get; set; } = new ColorNode(Color.FromArgb(245, 210, 60)); // warm yellow

        public RangeNode<float> LowThreshold  { get; set; } = new RangeNode<float>(-4.0f, -20.0f, 0.0f);
        public RangeNode<float> HighThreshold { get; set; } = new RangeNode<float>( 4.0f,   0.0f, 20.0f);

        // Used by the Profiles panel to size tables
        public RangeNode<int> TableRowsPerCategory { get; set; } = new RangeNode<int>(15, 5, 25);

        // The only control exposed in the Settings root
        [Menu(null, "Show Profiles Tab (?)")]
        public ToggleNode ShowProfilesTab { get; set; } = new ToggleNode(true);
    }
}
