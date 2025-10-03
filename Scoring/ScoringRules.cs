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
        // ── Banned sentinels ───────────────────────────────────────────────────
        public const int   BANNED_WEIGHT = -11;                      // weight row
        public const float BANNED_SCORE  = float.NegativeInfinity;   // final total

        public static bool IsWeightBanned(float w) => w <= BANNED_WEIGHT;
        public static bool IsScoreBanned(float s)  => s == BANNED_SCORE;

        // Legacy callers used IsBanned for either type:
        public static bool IsBanned(float v) => v == BANNED_SCORE || v <= BANNED_WEIGHT;

        // ── Single source of normalization ─────────────────────────────────────
private static readonly Regex RxTrim   = new(@"[^a-z0-9 \-#%{}|\[\]]", RegexOptions.Compiled);
private static readonly Regex RxSpaces = new(@"\s{2,}", RegexOptions.Compiled);

public static string Normalize(string s)
{
    if (string.IsNullOrWhiteSpace(s)) return string.Empty;

    // Work on a mutable copy so we can interpret bracket runs correctly
    var src = s;

    // Normalize odd Unicode spaces and dashes early
    src = src.Replace('\u00A0', ' ')
             .Replace('\u202F', ' ')
             .Replace('\u2007', ' ')
             .Replace('–', '-')
             .Replace('—', '-');

    // Replace every [...] or {...} by the text the game would SHOW:
    // - if there's "a|b|c" inside, keep the LAST option (c)
    // - if there's no '|', keep the token as-is (e.g., [Presence])
    string ReplaceBracketMatch(System.Text.RegularExpressions.Match m)
    {
        var body = m.Groups[1].Value.Trim();
        if (body.Contains("|"))
        {
            var parts = body.Split('|');
            body = parts[parts.Length - 1].Trim();
        }
        return " " + body + " ";
    }

    src = System.Text.RegularExpressions.Regex.Replace(src, @"\[(.*?)\]", ReplaceBracketMatch);
    src = System.Text.RegularExpressions.Regex.Replace(src, @"\{(.*?)\}", ReplaceBracketMatch);

    // Lowercase after we’ve extracted tokens
    src = src.ToLowerInvariant();

    // Canonicalize numbers/percents to '#'
    src = System.Text.RegularExpressions.Regex.Replace(src, @"\d+([.,]\d+)?", "#");

    // NEW: collapse multi-# so seeds like '1#' normalize the same as live '#'
    src = System.Text.RegularExpressions.Regex.Replace(src, @"#{2,}", "#");

    // Keep only a-z, 0-9, space, '#', and '-' (strip punctuation/artifacts)
    src = System.Text.RegularExpressions.Regex.Replace(src, @"[^a-z0-9 #\-]", " ");

    // Tiny alias to keep keys stable (matches your seeds)
    src = src.Replace("packs", "pack");

    // Collapse whitespace
    src = System.Text.RegularExpressions.Regex.Replace(src, @"\s{2,}", " ").Trim();

    return src;
}

        // ── Weight helpers ─────────────────────────────────────────────────────
        public static float GetWeight(Dictionary<string, float> dict, string key)
        {
            if (dict == null || string.IsNullOrWhiteSpace(key)) return 0f;
            var norm = Normalize(key);
            return dict.TryGetValue(norm, out var v) ? v : 0f;
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
            return map.TryGetValue(norm, out var v) ? v : 0f;
        }

        // ── Combine panels (uses MultDefault/MultDesecrated/MultHidden) ────────
        public static float CombinePanelScores(
            float visibleDefault,
            float visibleDesecrated,
            IList<float> hiddenWeights,
            WeightProfile profile)
        {
            // any banned weight -> whole choice banned
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
