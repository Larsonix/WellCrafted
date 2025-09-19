// ===============================================
// Overlay/OverlayRenderer.cs
// Hidden mods + score badge + visible echo (draw-list; tidy bubbles)
// ===============================================

// (only the changed parts shown)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using WellCrafted.Settings;
using WellCrafted.Profiles;
using WellCrafted.Mapping;
using WellCrafted.Scoring;
using WellCrafted.Util;
using WellCrafted.Diagnostics;

namespace WellCrafted.Overlay
{
    public class OverlayRenderer
    {
        private readonly WellCraftedSettings _settings;
        private readonly HiddenMappingLoader _loader;
        private readonly ProfilesService _profiles;

        // Log "unknown hidden" once per normalized visible line (if enabled)
        private readonly HashSet<string> _loggedUnknown = new(StringComparer.OrdinalIgnoreCase);

        public OverlayRenderer(WellCraftedSettings settings, HiddenMappingLoader loader, ProfilesService profiles)
        {
            _settings = settings;
            _loader   = loader;
            _profiles = profiles;
        }

        public void RenderOverlay(GameSnapshot snapshot)
        {
            if (snapshot == null) return;

            var profile = _profiles.GetActiveProfile();
            var dl = ImGui.GetForegroundDrawList();

            for (int i = 0; i < 3; i++)
            {
                var choice = snapshot.Choices[i];
                var rectOpt = Guard.GetSafeRect(choice);
                if (rectOpt == null) continue;

                var rect = rectOpt.Value;

                if (_settings.Overlay.DrawDebugRects.Value)
                {
                    var dbg = ToU32(_settings.Overlay.DebugRectColor.Value);
                    dl.AddRect(new(rect.X, rect.Y), new(rect.Right, rect.Bottom), dbg, 4f, ImDrawFlags.None, 2f);
                }

                float baseX = rect.X + _settings.Overlay.HiddenOffsetX.Value;
                float baseY = rect.Bottom + 2 + _settings.Overlay.HiddenOffsetY.Value + _settings.Overlay.LabelYOffset.Value;

                var visible = snapshot.ChoiceTexts.ElementAtOrDefault(i) ?? string.Empty;

                var hiddenLines = _loader.GetHiddenModsFor(visible);
                var weightsHidden = new List<float>();
                float lineY = baseY;

                if (hiddenLines == null || hiddenLines.Count == 0)
                {
                    // Optional one-shot debug for unmapped visible lines
                    if (_settings.LogUnknownHidden.Value)
                    {
                        var key = ScoringRules.Normalize(visible);
                        if (!string.IsNullOrWhiteSpace(key) && _loggedUnknown.Add(key))
                            Logger.Debug($"Unknown hidden for visible: \"{visible}\" (norm:\"{key}\")");
                    }

                    DrawBubbleText(dl, new(baseX, lineY), "No mod  (0.0)", _settings.ProfilesUI.MidWeightColor.Value);
                    lineY += _settings.Overlay.TextSize.Value;
                }
                else
                {
                    foreach (var h in hiddenLines)
                    {
                        var w = ScoringRules.GetWeight(profile.Hidden, h);
                        weightsHidden.Add(w);

                        var label = $"{h.Replace('%', '#')}  ({w:0.0})";
                        DrawBubbleText(dl, new(baseX, lineY), label, ColorRules.GetSliderColor(w, _settings.ProfilesUI));
                        lineY += _settings.Overlay.TextSize.Value;
                    }
                }

                var wDef = ScoringRules.GetWeight(profile.VisibleDefault, visible);
                var wDes = ScoringRules.GetWeight(profile.VisibleDesecrated, visible);

                if (ScoringRules.IsWeightBanned(wDef) || ScoringRules.IsWeightBanned(wDes) ||
                    (weightsHidden.Any() && weightsHidden.Any(ScoringRules.IsWeightBanned)))
                {
                    DrawBadge(dl, rect, ScoringRules.BANNED_SCORE, "BANNED");
                }
                else
                {
                    var total = ScoringRules.CombinePanelScores(wDef, wDes, weightsHidden, profile);
                    DrawBadge(dl, rect, total, total.ToString("0.0"));
                }

                if (_settings.Overlay.ShowVisibleEcho.Value && !string.IsNullOrWhiteSpace(visible))
                {
                    DrawBubbleText(dl, new(baseX, lineY), visible, _settings.ProfilesUI.MidWeightColor.Value);
                }
            }
        }

        // NEW: score-aware badge
        private void DrawBadge(ImDrawListPtr dl, ExileCore2.Shared.RectangleF rect, float score, string text)
        {
            if (!_settings.Overlay.ShowScoreBadge.Value) return;

            var pos = new Vector2(
                rect.X + _settings.Overlay.BadgeOffsetX.Value,
                rect.Bottom - 18 + _settings.Overlay.BadgeOffsetY.Value
            );

            var col = ColorRules.GetScoreColor(score, _settings.ProfilesUI);
            DrawBubbleText(dl, pos, text, col);
        }

        // Overload: ColorNode → packed
        private void DrawBubbleText(ImDrawListPtr dl, Vector2 pos, string text, System.Drawing.Color color)
            => DrawBubbleText(dl, pos, text, ToU32(color));

        private void DrawBubbleText(ImDrawListPtr dl, Vector2 pos, string text, uint packed)
        {
            if (_settings.Overlay.PixelSnap.Value)
            {
                pos.X = MathF.Round(pos.X);
                pos.Y = MathF.Round(pos.Y);
            }

            var size = ImGui.CalcTextSize(text);

            if (_settings.Overlay.UseBubbles.Value)
            {
                var padX = _settings.Overlay.BubblePadX.Value;
                var padY = _settings.Overlay.BubblePadY.Value;
                var r    = _settings.Overlay.BubbleRoundness.Value;

                var bg = ToU32(_settings.Overlay.BubbleBgColor.Value);
                dl.AddRectFilled(new(pos.X - padX, pos.Y - padY), new(pos.X + size.X + padX, pos.Y + size.Y + padY), bg, r);
            }

            dl.AddText(pos, packed, text);
        }

        private static uint ToU32(System.Drawing.Color c)
        {
            var v = new Vector4(c.R/255f, c.G/255f, c.B/255f, c.A/255f);
            return ImGui.ColorConvertFloat4ToU32(v);
        }
    }
}
