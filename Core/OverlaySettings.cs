// ===============================================
// Settings/OverlaySettings.cs — ordered + requested defaults
// ===============================================

using System.Drawing;
using ExileCore2.Shared.Attributes;
using ExileCore2.Shared.Nodes;

namespace WellCrafted.Settings
{
    public class OverlaySettings
    {
        // ── Text & position ───────────────────────────────────────────────────
        [Menu(null, "Hidden text nominal height (line spacing, px)")]
        public RangeNode<int> TextSize { get; set; } = new RangeNode<int>(18, 1, 50); // 25

        [Menu(null, "Hidden Offset X (px)")]
        public RangeNode<int> HiddenOffsetX { get; set; } = new RangeNode<int>(4, -800, 800); // 15

        [Menu(null, "Hidden Offset Y (px)")]
        public RangeNode<int> HiddenOffsetY { get; set; } = new RangeNode<int>(-4, -800, 800); // 0

        [Menu(null, "Badge Offset X (px)")]
        public RangeNode<int> BadgeOffsetX { get; set; } = new RangeNode<int>(626, -800, 800); // 645

        [Menu(null, "Badge Offset Y (px)")]
        public RangeNode<int> BadgeOffsetY { get; set; } = new RangeNode<int>(-9, -800, 800); // -10

        [Menu(null, "Label Y Offset (px)")]
        public RangeNode<int> LabelYOffset { get; set; } = new RangeNode<int>(-30, -100, 200); // -30

        // ── Visibility / rendering toggles ───────────────────────────────────
        [Menu(null, "Show Score Badge")]
        public ToggleNode ShowScoreBadge { get; set; } = new ToggleNode(true); // checked

        [Menu(null, "Also show the visible text (echo) above hidden line")]
        public ToggleNode ShowVisibleEcho { get; set; } = new ToggleNode(false);

        [Menu(null, "Snap text position to pixels (sharper)")]
        public ToggleNode PixelSnap { get; set; } = new ToggleNode(false);

        [Menu(null, "Avoid overlapping the game text (force below)")]
        public ToggleNode AvoidOverlap { get; set; } = new ToggleNode(false);

        [Menu(null, "Debug rectangle color")]
        public ColorNode DebugRectColor { get; set; } = new ColorNode(Color.Cyan);

        [Menu(null, "Draw debug rectangles on detected option slots")]
        public ToggleNode DrawDebugRects { get; set; } = new ToggleNode(false);

        // ── Bubble background behind text ────────────────────────────────────
        [Menu(null, "Draw bubbles behind overlay text")]
        public ToggleNode UseBubbles { get; set; } = new ToggleNode(true); // checked

        [Menu(null, "Bubble background color")]
        public ColorNode BubbleBgColor { get; set; } = new ColorNode(Color.FromArgb(160, 15, 15, 15));

        [Menu(null, "Bubble corner radius (px)")]
        public RangeNode<int> BubbleRoundness { get; set; } = new RangeNode<int>(6, 0, 20); // 6

        [Menu(null, "Bubble padding X (px)")]
        public RangeNode<int> BubblePadX { get; set; } = new RangeNode<int>(6, 0, 20); // 6

        [Menu(null, "Bubble padding Y (px)")]
        public RangeNode<int> BubblePadY { get; set; } = new RangeNode<int>(1, 0, 12); // 8 (assumed your “A” meant 8)
    }
}
