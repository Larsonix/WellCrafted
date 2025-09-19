// ===============================================
// Profiles/ProfileModel.cs
// WeightProfile model: Visible/Hidden dicts, metadata
// ===============================================

using System;
using System.Collections.Generic;

namespace WellCrafted.Profiles
{
    public class WeightProfile
    {
        // Per-panel weights (floats, -11..+10). Sentinel -11 = BANNED.
        public Dictionary<string, float> VisibleDefault { get; set; } = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, float> VisibleDesecrated { get; set; } = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, float> Hidden { get; set; } = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        // Panel bias multipliers [-1..+1], center=0 (sign-aware scaling)
        public float MultDefault { get; set; } = 0.0f;
        public float MultDesecrated { get; set; } = 0.0f;
        public float MultHidden { get; set; } = 0.0f;
    }

    public class ProfilesFile
    {
        public int Schema { get; set; } = 2;
        public Dictionary<string, WeightProfile> Profiles { get; set; } = new Dictionary<string, WeightProfile>(StringComparer.OrdinalIgnoreCase);
        public string Active { get; set; } = "Default";
    }
}
