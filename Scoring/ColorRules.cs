// ===============================================
// Scoring/ColorRules.cs
// Color selection based on thresholds (Profiles → Appearance)
// ===============================================

using System.Drawing;
using System.Numerics;
using ImGuiNET;
using WellCrafted.Settings;

namespace WellCrafted.Scoring
{
    public static class ColorRules
    {
        // Score badge → System.Drawing.Color
        public static Color GetScoreColor(float score, ProfilesUiSettings ui)
        {
            if (ScoringRules.IsScoreFavorite(score)) return ui.FavoriteWeightColor.Value;
            if (ScoringRules.IsScoreBanned(score))   return ui.LowWeightColor.Value;

            if (score <= ui.LowThreshold.Value)      return ui.LowWeightColor.Value;
            if (score >= ui.HighThreshold.Value)     return ui.HighWeightColor.Value;
            return ui.MidWeightColor.Value;
        }

        // Table row background → packed uint (soft alpha)
        public static uint GetRowBackgroundColor(float val, ProfilesUiSettings ui)
        {
            var c = GetScoreColor(val, ui);
            var ca = Color.FromArgb(36, c.R, c.G, c.B);
            return ToU32(ca);
        }

        // Slider text color → packed uint
        public static uint GetSliderColor(float weight, ProfilesUiSettings ui)
        {
            if (ScoringRules.IsWeightFavorite(weight)) return ToU32(ui.FavoriteWeightColor.Value);
            if (ScoringRules.IsWeightBanned(weight))   return ToU32(ui.LowWeightColor.Value);

            if (weight <= ui.LowThreshold.Value)       return ToU32(ui.LowWeightColor.Value);
            if (weight >= ui.HighThreshold.Value)      return ToU32(ui.HighWeightColor.Value);
            return ToU32(ui.MidWeightColor.Value);
        }

        // Helpers
        public static uint ToU32(Color c)
        {
            var v = new Vector4(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);
            return ImGui.ColorConvertFloat4ToU32(v);
        }
    }
}
