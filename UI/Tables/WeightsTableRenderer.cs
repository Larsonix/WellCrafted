// ===============================================
// UI/Tables/WeightsTableRenderer.cs
// ImGui table (Default/Desecrated/Hidden); no per-tick save
// ===============================================

using System;
using System.Collections.Generic;
using ImGuiNET;
using WellCrafted.Settings;
using WellCrafted.Profiles;
using WellCrafted.Scoring;

namespace WellCrafted.UI.Tables
{
    public class WeightsTableRenderer
    {
        private readonly WellCraftedSettings _settings;
        private readonly ProfilesService _profiles;

        public WeightsTableRenderer(WellCraftedSettings settings, ProfilesService profiles)
        {
            _settings = settings;
            _profiles = profiles;
        }

        public void RenderWeightsTable(string category, List<string> modifiers)
        {
            ImGui.Text($"{category} Weights (-11=BANNED, -10..+11; step 1)");

            var flags = ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerV |
                        ImGuiTableFlags.ScrollY | ImGuiTableFlags.Resizable | ImGuiTableFlags.SizingStretchProp;

            if (!ImGui.BeginTable($"wc_{category}_weights", 2, flags)) return;

            ImGui.TableSetupColumn("Modifier", ImGuiTableColumnFlags.None, 0.70f);
            ImGui.TableSetupColumn("Weight",   ImGuiTableColumnFlags.WidthFixed, 160f);
            ImGui.TableHeadersRow();

            var profile = _profiles.GetActiveProfile();
            foreach (var mod in modifiers)
                RenderRow(profile, category, mod);

            ImGui.EndTable();

            if (ImGui.Button($"Save {category}")) _profiles.SaveProfiles();
            ImGui.SameLine();
            ImGui.TextDisabled(" Sliders edit in-memory; click Save to persist.");
        }

        private void RenderRow(WeightProfile profile, string category, string modifier)
        {
            ImGui.TableNextRow();

            var normKey = ScoringRules.Normalize(modifier);
            var dict = CategoryDict(profile, category);

            float cur = 0f;
            dict?.TryGetValue(normKey, out cur);

            ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0,
                ColorRules.GetRowBackgroundColor(cur, _settings.ProfilesUI));

            ImGui.TableSetColumnIndex(0);
            ImGui.TextUnformatted(modifier);

            ImGui.TableSetColumnIndex(1);
            ImGui.PushID($"{category}_{normKey}");

            int v = (int)Math.Clamp(Math.Round(cur), ScoringRules.BANNED_WEIGHT, ScoringRules.FAVORITE_WEIGHT);
            ImGui.PushStyleColor(ImGuiCol.Text, ColorRules.GetSliderColor(v, _settings.ProfilesUI));

            string fmt =
                ScoringRules.IsWeightBanned(v)   ? "Banned"   :
                ScoringRules.IsWeightFavorite(v) ? "Favorite" :
                "%d";

            if (ImGui.SliderInt("##w", ref v, ScoringRules.BANNED_WEIGHT, ScoringRules.FAVORITE_WEIGHT, fmt, ImGuiSliderFlags.AlwaysClamp))
            {
                // Edit only in memory; user explicitly saves
                _profiles.UpdateWeight(category, normKey, v, save: false);
            }

            ImGui.PopStyleColor();
            ImGui.PopID();
        }

        private static Dictionary<string, float> CategoryDict(WeightProfile p, string category) =>
            category?.ToLowerInvariant() switch
            {
                "default"    => p.VisibleDefault,
                "desecrated" => p.VisibleDesecrated,
                "hidden"     => p.Hidden,
                _            => null
            };
    }
}
