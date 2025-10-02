// ===============================================
// Mapping/HiddenMapping.cs
// Built-in mapping table + merge interface + helpers (clean)
// ===============================================

using System;
using System.Collections.Generic;
using System.Linq;
using WellCrafted.Scoring;

namespace WellCrafted.Mapping
{
    public class HiddenMapEntry
    {
        public string Match { get; set; }
        public List<string> Hidden { get; set; } = new();
    }

    // JSON schema wrapper used by loader (Import/Export)
    public class HiddenMap
    {
        public int Schema { get; set; } = 2;
        public List<HiddenMapEntry> Mappings { get; set; } = new();
    }

    public static class HiddenMapping
    {
        // Canonical hidden labels (must match Hidden tab keys)
        public const string H_Rare   = "# increased Number of Rare Monsters";
        public const string H_Magic  = "# increased Number of Magic Monsters";
        public const string H_Rarity = "# increased Item Rarity";
        public const string H_Way    = "# increased Waystones found";
        public const string H_Pack   = "# increased Pack Size";

        // Single-source normalization proxy
        public static string Normalize(string s) => ScoringRules.Normalize(s);

        public static List<HiddenMapEntry> GetBuiltInMappings()
        {
            var list = new List<HiddenMapEntry>
            {
                new()
                {
                    Match  = "Natural Monster Packs in Area are in a Union of Souls",
                    Hidden = new() { H_Pack }
                },
                new()
                {
                    Match  = "Natural Rare Monsters in Area are in a Union of Souls with the Map Boss",
                    Hidden = new() { H_Rare }
                },
                new()
                {
                    Match  = "Area has patches of Mana Siphoning Ground",
                    Hidden = new() { H_Pack }
                },
                new()
                {
                    Match  = "Players are Marked for Death for 10 seconds after killing a Rare or Unique monster",
                    Hidden = new() { H_Rarity, H_Pack }
                },
            };

            foreach (var e in list)
                e.Match = Normalize(e.Match);

            return list;
        }

        public static List<HiddenMapEntry> MergeMappings(List<HiddenMapEntry> baseMappings, List<HiddenMapEntry> userMappings)
        {
            var merged = new Dictionary<string, HiddenMapEntry>(StringComparer.OrdinalIgnoreCase);

            void AddRange(IEnumerable<HiddenMapEntry> src)
            {
                if (src == null) return;
                foreach (var m in src)
                {
                    if (m == null || string.IsNullOrWhiteSpace(m.Match)) continue;

                    var k = Normalize(m.Match);
                    if (!merged.TryGetValue(k, out var existing))
                    {
                        merged[k] = new HiddenMapEntry
                        {
                            Match  = k,
                            Hidden = new List<string>(m.Hidden ?? Enumerable.Empty<string>())
                        };
                    }
                    else
                    {
                        foreach (var h in m.Hidden ?? Enumerable.Empty<string>())
                            if (!existing.Hidden.Contains(h, StringComparer.OrdinalIgnoreCase))
                                existing.Hidden.Add(h);
                    }
                }
            }

            AddRange(baseMappings);
            AddRange(userMappings);

            return merged.Values.ToList();
        }

        // ────────────────────────────────────────────────────────────────────
        // IMPORTANT: “contains” matching to handle combined / two-line texts.
        // We normalize the whole visible block and include any mapping whose
        // normalized Match string is a substring of the normalized visible.
        // ────────────────────────────────────────────────────────────────────
        public static List<string> GetHiddenModsFor(string visible, List<HiddenMapEntry> mappings)
        {
            if (string.IsNullOrWhiteSpace(visible) || mappings == null || mappings.Count == 0)
                return null;

            var normVisible = Normalize(visible);
            if (string.IsNullOrWhiteSpace(normVisible)) return null;

            var hits = new List<string>();

            foreach (var m in mappings)
            {
                if (string.IsNullOrWhiteSpace(m?.Match)) continue;
                // exact OR contained → treat as a match
                if (normVisible.Equals(m.Match, StringComparison.OrdinalIgnoreCase) ||
                    normVisible.Contains(m.Match, StringComparison.OrdinalIgnoreCase))
                {
                    if (m.Hidden != null)
                        hits.AddRange(m.Hidden);
                }
            }

            return hits.Count > 0
                ? hits.Distinct(StringComparer.OrdinalIgnoreCase).ToList()
                : null;
        }

        public static List<string> GetAllKnownVisibleMods(List<HiddenMapEntry> mappings)
            => mappings == null ? new() : mappings.Select(m => m.Match).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(s => s).ToList();

        public static List<string> GetAllKnownHiddenMods(List<HiddenMapEntry> mappings)
            => mappings == null ? new() : mappings.SelectMany(m => m.Hidden ?? Enumerable.Empty<string>()).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(s => s).ToList();
    }
}
