// ===============================================
// UI/Overlay/OverlayRenderer.cs
// Hidden mods + score badge + visible echo (draw-list; tidy bubbles)
// Adds: choices-panel generation tracking + 3s grace window (configurable)
//       + strict rectangle validity checks to avoid (0,0,0,0) flicker
// ===============================================

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

        // Panel visibility/generation + grace
        private bool _wasVisible;
        private ulong _lastGenKey;
        private long _graceUntilMs;

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
            // Guard: only draw when the Well panel is actually present
            if (snapshot == null || !snapshot.RootOk) return;

            // Pre-read rectangles and validate them (avoid (0,0,0,0) flicker).
            var rects = new ExileCore2.Shared.RectangleF?[3];
            bool[] rectValid = new bool[3];
            for (int i = 0; i < 3; i++)
            {
                var ropt = Guard.GetSafeRect(snapshot.Choices[i]);
                rects[i] = ropt;
                rectValid[i] = IsRectValid(ropt);
            }
            bool anyRectValid = rectValid[0] || rectValid[1] || rectValid[2];

            // Determine current visibility of the 3-choices panel.
            // We require an actually valid rect on at least one lane.
            bool lanesVisible = snapshot.ChoiceVisible[0] || snapshot.ChoiceVisible[1] || snapshot.ChoiceVisible[2];
            bool panelVisible = lanesVisible && anyRectValid;

            // Build a generation key from the three choice rectangles (robust even when text lags).
            // Use only valid rects; invalid ones contribute zero so they don't thrash the key.
            ulong a = rectValid[0] ? PackRect(rects[0]!.Value) : 0UL;
            ulong b = rectValid[1] ? PackRect(rects[1]!.Value) : 0UL;
            ulong c = rectValid[2] ? PackRect(rects[2]!.Value) : 0UL;
            ulong genKey = Mix(a, b, c);

            long now = Environment.TickCount64;
            if (panelVisible)
            {
                if (!_wasVisible || genKey != _lastGenKey)
                {
                    // Panel (re)opened or rebuilt: start/refresh grace window
                    _graceUntilMs = now + Math.Max(0, _settings.ChoicesGraceMs?.Value ?? 3000);
                    _lastGenKey = genKey;

                    // reset one-shot unknowns for a clean run on new generation
                    _loggedUnknown.Clear();
                }
            }
            _wasVisible = panelVisible;

            if (!panelVisible) return;

            bool inGrace = now < _graceUntilMs;

            var profile = _profiles.GetActiveProfile();
            var dl = ImGui.GetForegroundDrawList();

            // During grace: relax the text presence gate. After grace: require at least some text.
            bool anyText = (snapshot.ChoiceTextOk[0] || snapshot.ChoiceTextOk[1] || snapshot.ChoiceTextOk[2]);
            if (!inGrace && !anyText) return;

            for (int i = 0; i < 3; i++)
            {
                if (!rectValid[i]) continue; // skip invalid targets entirely

                var rect = rects[i]!.Value;

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

                    // In grace, show a placeholder line to keep layout stable even if text hasn't appeared yet
                    if (inGrace || !string.IsNullOrWhiteSpace(visible))
                    {
                        DrawBubbleText(dl, new(baseX, lineY), "No mod  (0.0)", _settings.ProfilesUI.MidWeightColor.Value);
                        lineY += _settings.Overlay.TextSize.Value;
                    }
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

                if (_settings.Overlay.ShowVisibleEcho.Value && (inGrace || !string.IsNullOrWhiteSpace(visible)))
                {
                    // In grace, allow empty echo area (fills as text arrives)
                    var echo = string.IsNullOrWhiteSpace(visible) ? "" : visible;
                    DrawBubbleText(dl, new(baseX, lineY), echo, _settings.ProfilesUI.MidWeightColor.Value);
                }
            }
        }

        private static bool IsRectValid(ExileCore2.Shared.RectangleF? rOpt)
        {
            if (rOpt == null) return false;
            var r = rOpt.Value;
            // Treat tiny or zero rects as invalid; also guard negative sizes.
            return r.Width > 8 && r.Height > 8;
        }

        private static ulong PackRect(ExileCore2.Shared.RectangleF r)
        {
            unchecked
            {
                ulong x = (ulong)(long)Math.Round(r.X) & 0xFFFFUL;
                ulong y = (ulong)(long)Math.Round(r.Y) & 0xFFFFUL;
                ulong w = (ulong)(long)Math.Round(r.Width) & 0xFFFFUL;
                ulong h = (ulong)(long)Math.Round(r.Height) & 0xFFFFUL;
                return x | (y << 16) | (w << 32) | (h << 48);
            }
        }

        // score-aware badge
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
            if (string.IsNullOrEmpty(text)) return;

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

        private static ulong Mix(ulong a, ulong b, ulong c)
        {
            ulong x = 0x9E3779B97F4A7C15UL;
            x ^= a + (x << 6) + (x >> 2);
            x ^= b + (x << 6) + (x >> 2);
            x ^= c + (x << 6) + (x >> 2);
            return x;
        }
    }
}
