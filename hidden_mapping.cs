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

            // Normalize matches once
            foreach (var e in list)
                e.Match = ScoringRules.Normalize(e.Match);

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
                    var k = ScoringRules.Normalize(m.Match);
                    if (!merged.TryGetValue(k, out var existing))
                    {
                        merged[k] = new HiddenMapEntry
                        {
                            Match  = k,
                            Hidden = new List<string>(m.Hidden)
                        };
                    }
                    else
                    {
                        foreach (var h in m.Hidden)
                            if (!existing.Hidden.Contains(h, StringComparer.OrdinalIgnoreCase))
                                existing.Hidden.Add(h);
                    }
                }
            }

            AddRange(baseMappings);
            AddRange(userMappings);

            return merged.Values.ToList();
        }

        // Query helpers used by the loader/overlay
        public static List<string> GetHiddenModsFor(string visible, List<HiddenMapEntry> mappings)
        {
            if (string.IsNullOrWhiteSpace(visible) || mappings == null || mappings.Count == 0)
                return null;

            var key = ScoringRules.Normalize(visible);
            var match = mappings.FirstOrDefault(m => string.Equals(m.Match, key, StringComparison.OrdinalIgnoreCase));
            return match != null ? match.Hidden.ToList() : null;
        }

        public static List<string> GetAllKnownVisibleMods(List<HiddenMapEntry> mappings)
            => mappings == null ? new() : mappings.Select(m => m.Match).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(s => s).ToList();

        public static List<string> GetAllKnownHiddenMods(List<HiddenMapEntry> mappings)
            => mappings == null ? new() : mappings.SelectMany(m => m.Hidden).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(s => s).ToList();
    }
}
