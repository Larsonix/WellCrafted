// ===============================================
// Scoring/ScoringRules.cs
// Weights, normalization, and score combination helpers
// ===============================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using WellCrafted.Profiles;

namespace WellCrafted.Scoring
{
    public static class ScoringRules
    {
        // ── Sentinels ──────────────────────────────────────────────────────────
        public const int   BANNED_WEIGHT    = -11;                     // weight row
        public const int   FAVORITE_WEIGHT  =  11;                     // weight row
        public const float BANNED_SCORE     = float.NegativeInfinity;  // final total
        public const float FAVORITE_SCORE   = float.PositiveInfinity;  // final total

        public static bool IsWeightBanned(float w)   => w <= BANNED_WEIGHT;
        public static bool IsWeightFavorite(float w) => w >= FAVORITE_WEIGHT;

        public static bool IsScoreBanned(float s)    => s == BANNED_SCORE;
        public static bool IsScoreFavorite(float s)  => s == FAVORITE_SCORE;

        // Legacy convenience:
        public static bool IsBanned(float v) => v == BANNED_SCORE || v <= BANNED_WEIGHT;

        // ── Single source of normalization ─────────────────────────────────────
        private static readonly Regex RxTrim   = new(@"[^a-z0-9 \-#%{}|\[\]]", RegexOptions.Compiled);
        private static readonly Regex RxSpaces = new(@"\s{2,}", RegexOptions.Compiled);

        public static string Normalize(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;

            var src = s;

            // Normalize odd spaces/dashes
            src = src.Replace('\u00A0', ' ')
                     .Replace('\u202F', ' ')
                     .Replace('\u2007', ' ')
                     .Replace('–', '-')
                     .Replace('—', '-');

            // Replace [a|b|c] / {a|b|c} with the last option (what the game shows)
            static string ReplaceBracketMatch(Match m)
            {
                var body = m.Groups[1].Value.Trim();
                if (body.Contains("|"))
                {
                    var parts = body.Split('|');
                    body = parts[parts.Length - 1].Trim();
                }
                return " " + body + " ";
            }

            src = Regex.Replace(src, @"\[(.*?)\]", ReplaceBracketMatch);
            src = Regex.Replace(src, @"\{(.*?)\}", ReplaceBracketMatch);

            src = src.ToLowerInvariant();

            // Canonicalize numbers to '#'
            src = Regex.Replace(src, @"\d+([.,]\d+)?", "#");
            // Collapse multi-#
            src = Regex.Replace(src, @"#{2,}", "#");
            // Keep only allowed chars
            src = Regex.Replace(src, @"[^a-z0-9 #\-]", " ");
            // Tiny alias to keep keys stable
            src = src.Replace("packs", "pack");
            // Collapse spaces
            src = Regex.Replace(src, @"\s{2,}", " ").Trim();

            return src;
        }

        // ── Weight helpers ─────────────────────────────────────────────────────
        public static float GetWeight(Dictionary<string, float> dict, string key)
        {
            if (dict == null || string.IsNullOrWhiteSpace(key)) return 0f;
            var norm = Normalize(key);

            if (dict.TryGetValue(norm, out var v)) return v;

            // Canonicalize any "overrun ..." variant to the slider row
            if (norm.Contains("overrun"))
            {
                if (dict.TryGetValue("area is overrun by the abyssal", out v)) return v;
                if (dict.TryGetValue("area is overrun by the abyss", out v))   return v;
            }

            return 0f;
        }

        public static float GetWeight(WeightProfile profile, string category, string key)
        {
            if (profile == null) return 0f;
            var norm = Normalize(key);

            var map = category?.ToLowerInvariant() switch
            {
                "default"    => profile.VisibleDefault,
                "desecrated" => profile.VisibleDesecrated,
                "hidden"     => profile.Hidden,
                _            => null
            };
            if (map == null) return 0f;

            if (map.TryGetValue(norm, out var v)) return v;

            // Same "overrun" canonicalization for profile lookups
            if (norm.Contains("overrun"))
            {
                if (map.TryGetValue("area is overrun by the abyssal", out v)) return v;
                if (map.TryGetValue("area is overrun by the abyss", out v))   return v;
            }

            return 0f;
        }

        // ── Combine panels (uses MultDefault/MultDesecrated/MultHidden) ────────
        public static float CombinePanelScores(
            float visibleDefault,
            float visibleDesecrated,
            IList<float> hiddenWeights,
            WeightProfile profile)
        {
            // FAVORITE overrides everything
            if (IsWeightFavorite(visibleDefault) ||
                IsWeightFavorite(visibleDesecrated) ||
                (hiddenWeights?.Any(IsWeightFavorite) ?? false))
                return FAVORITE_SCORE;

            // Any banned weight -> whole choice banned
            if (IsWeightBanned(visibleDefault) ||
                IsWeightBanned(visibleDesecrated) ||
                (hiddenWeights?.Any(IsWeightBanned) ?? false))
                return BANNED_SCORE;

            float multDefault    = profile?.MultDefault    ?? 0f;
            float multDesecrated = profile?.MultDesecrated ?? 0f;
            float multHidden     = profile?.MultHidden     ?? 0f;

            float hiddenAvg = 0f;
            if (hiddenWeights != null && hiddenWeights.Count > 0)
                hiddenAvg = hiddenWeights.Average();

            var sum =
                visibleDefault    * (1f + multDefault) +
                visibleDesecrated * (1f + multDesecrated) +
                hiddenAvg         * (1f + multHidden);

            return MathF.Max(-20f, MathF.Min(20f, sum));
        }

        public static string Format1(float v) => v.ToString("0.0", CultureInfo.InvariantCulture);
    }
}
