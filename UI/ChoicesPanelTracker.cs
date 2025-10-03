// ===============================================
// UI/ChoicesPanelTracker.cs
// Tracks 3-choices panel visibility + generation flips with a grace window.
// Works directly from GameSnapshot; no DevTree path usage.
// ===============================================

using System;
using WellCrafted.Settings;
using WellCrafted.Util; // Guard.GetSafeRect

namespace WellCrafted.UI
{
    /// <summary>
    /// Tracks when the “3 choices” panel is visible, detects panel rebuilds (“generation flips”)
    /// by hashing the three choice rectangles, and runs a grace window to relax text gating.
    /// </summary>
    public sealed class ChoicesPanelTracker
    {
        private readonly WellCraftedSettings _settings;

        public bool PanelVisible { get; private set; }
        public uint Generation { get; private set; }
        public long GraceUntilMs { get; private set; }

        private bool _wasVisible;
        private ulong _lastGenKey;

        public ChoicesPanelTracker(WellCraftedSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>Convenience: uses Environment.TickCount64 for time source.</summary>
        public void Refresh(GameSnapshot snapshot)
        {
            Update(snapshot, Environment.TickCount64);
        }

        /// <summary>Call once per frame with the current snapshot and a time source in milliseconds.</summary>
        public void Update(GameSnapshot snapshot, long nowMs)
        {
            if (snapshot == null || !snapshot.RootOk)
            {
                SetVisible(false);
                return;
            }

            // Panel is considered visible if any of the three choices is visible.
            var vis = snapshot.ChoiceVisible;
            bool visible = vis != null && (vis[0] || vis[1] || vis[2]);
            if (!visible)
            {
                SetVisible(false);
                return;
            }

            // Build a stable generation key from the three choice rectangles.
            // Using geometry avoids depending on Element addresses and is robust to text lag.
            ulong a = PackRect(Guard.GetSafeRect(snapshot.Choices[0]));
            ulong b = PackRect(Guard.GetSafeRect(snapshot.Choices[1]));
            ulong c = PackRect(Guard.GetSafeRect(snapshot.Choices[2]));
            ulong genKey = Mix(a, b, c);

            if (!_wasVisible || genKey != _lastGenKey)
            {
                Generation++;
                int grace = Math.Max(0, _settings.ChoicesGraceMs?.Value ?? 3000);
                GraceUntilMs = nowMs + grace;
                _lastGenKey = genKey;
            }

            SetVisible(true);
        }

        public bool InGrace(long nowMs) => nowMs < GraceUntilMs;

        private void SetVisible(bool v)
        {
            _wasVisible = PanelVisible;
            PanelVisible = v;
            if (!v)
            {
                GraceUntilMs = 0;
                _lastGenKey = 0;
            }
        }

        private static ulong PackRect(ExileCore2.Shared.RectangleF? rOpt)
        {
            if (rOpt == null) return 0UL;
            ExileCore2.Shared.RectangleF r = rOpt.Value;

            // Round to reduce jitter, then pack into 64 bits: 16 bits per field.
            unchecked
            {
                ulong x = (ulong)(long)Math.Round(r.X) & 0xFFFFUL;
                ulong y = (ulong)(long)Math.Round(r.Y) & 0xFFFFUL;
                ulong w = (ulong)(long)Math.Round(r.Width) & 0xFFFFUL;
                ulong h = (ulong)(long)Math.Round(r.Height) & 0xFFFFUL;

                return x | (y << 16) | (w << 32) | (h << 48);
            }
        }

        private static ulong Mix(ulong a, ulong b, ulong c)
        {
            // small xorshift-style mix
            ulong x = 0x9E3779B97F4A7C15UL;
            x ^= a + (x << 6) + (x >> 2);
            x ^= b + (x << 6) + (x >> 2);
            x ^= c + (x << 6) + (x >> 2);
            return x;
        }
    }
}
