// ===============================================
// UI/ProfilesPanel.cs
// Profiles UI (dropdown, create/rename/delete)
// ===============================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using WellCrafted.Settings;
using WellCrafted.Profiles;
using WellCrafted.UI.Tables;

namespace WellCrafted.UI
{
    public class ProfilesPanel
    {
        private readonly WellCraftedSettings _settings;
        private readonly ProfilesService _profilesService;
        private readonly WeightsTableRenderer _tableRenderer;
        private string _newProfileName = string.Empty;

        public ProfilesPanel(WellCraftedSettings settings, ProfilesService profilesService)
        {
            _settings = settings;
            _profilesService = profilesService;
            _tableRenderer = new WeightsTableRenderer(settings, profilesService);
        }

        public void Render()
        {
            RenderProfileManagement();
            ImGui.Separator();
            RenderAppearanceSettings();
            ImGui.Separator();
            RenderPanelMultipliers();
            ImGui.Separator();
            RenderWeightTabs();
        }

        private void RenderProfileManagement()
        {
            ImGui.Text("Active profile:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(260f);

            var currentProfile = _profilesService.ActiveProfile;
            if (ImGui.BeginCombo("##active_profile", currentProfile))
            {
                foreach (var profileName in _profilesService.GetProfileNames())
                {
                    bool isSelected = profileName.Equals(currentProfile, StringComparison.OrdinalIgnoreCase);
                    if (ImGui.Selectable(profileName, isSelected))
                        _profilesService.ActiveProfile = profileName;
                    if (isSelected) ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }

            ImGui.SameLine();
            bool canDelete = !currentProfile.Equals("Default", StringComparison.OrdinalIgnoreCase);
            if (!canDelete) ImGui.BeginDisabled();
            if (ImGui.Button("Delete"))
                _profilesService.DeleteProfile(currentProfile);
            if (!canDelete) ImGui.EndDisabled();

            ImGui.InputText("Name", ref _newProfileName, 64);
            ImGui.SameLine();

            if (ImGui.Button("Create"))
            {
                var name = _newProfileName?.Trim();
                if (!string.IsNullOrEmpty(name) && _profilesService.CreateProfile(name))
                {
                    _profilesService.ActiveProfile = name;
                    _newProfileName = string.Empty;
                }
            }

            ImGui.SameLine();
            if (ImGui.Button("Rename"))
            {
                var newName = _newProfileName?.Trim();
                if (!string.IsNullOrEmpty(newName))
                {
                    if (_profilesService.RenameProfile(currentProfile, newName))
                        _newProfileName = string.Empty;
                }
            }
        }

        private void RenderAppearanceSettings()
        {
            if (!ImGui.CollapsingHeader("Appearance")) return;

            var s = _settings.ProfilesUI;
            var vLow  = new Vector3(s.LowWeightColor.Value.R/255f,  s.LowWeightColor.Value.G/255f,  s.LowWeightColor.Value.B/255f);
            var vMid  = new Vector3(s.MidWeightColor.Value.R/255f,  s.MidWeightColor.Value.G/255f,  s.MidWeightColor.Value.B/255f);
            var vHigh = new Vector3(s.HighWeightColor.Value.R/255f, s.HighWeightColor.Value.G/255f, s.HighWeightColor.Value.B/255f);

            if (ImGui.ColorEdit3("Low weight", ref vLow))
                s.LowWeightColor.Value = System.Drawing.Color.FromArgb(255, (int)(vLow.X*255), (int)(vLow.Y*255), (int)(vLow.Z*255));
            if (ImGui.ColorEdit3("Normal weight", ref vMid))
                s.MidWeightColor.Value = System.Drawing.Color.FromArgb(255, (int)(vMid.X*255), (int)(vMid.Y*255), (int)(vMid.Z*255));
            if (ImGui.ColorEdit3("High weight", ref vHigh))
                s.HighWeightColor.Value = System.Drawing.Color.FromArgb(255, (int)(vHigh.X*255), (int)(vHigh.Y*255), (int)(vHigh.Z*255));

            var lowT  = s.LowThreshold.Value;
            var highT = s.HighThreshold.Value;
            if (ImGui.SliderFloat("Low threshold", ref lowT,  -20f, 0f)) s.LowThreshold.Value  = lowT;
            if (ImGui.SliderFloat("High threshold", ref highT, 0f,  20f)) s.HighThreshold.Value = highT;

            if (ImGui.Button("Reset Colors to Defaults"))
            {
                s.LowWeightColor.Value  = System.Drawing.Color.FromArgb(230, 60, 60);
                s.MidWeightColor.Value  = System.Drawing.Color.FromArgb(240, 240, 240);
                s.HighWeightColor.Value = System.Drawing.Color.FromArgb(60, 200, 90);
                s.LowThreshold.Value    = -4.0f;
                s.HighThreshold.Value   = 4.0f;
            }
        }

        private void RenderPanelMultipliers()
        {
            ImGui.Text("Panel bias (−1..+1). Center = neutral; right favors positives / mutes negatives; left favors negatives / mutes positives.");

            var p = _profilesService.GetActiveProfile();

            var mD = p.MultDefault;
            if (ImGui.SliderFloat("Default##multd", ref mD, -1f, 1f, "%.2f")) { p.MultDefault = mD; _profilesService.SaveProfiles(); }

            var mDs = p.MultDesecrated;
            if (ImGui.SliderFloat("Desecrated##multds", ref mDs, -1f, 1f, "%.2f")) { p.MultDesecrated = mDs; _profilesService.SaveProfiles(); }

            var mH = p.MultHidden;
            if (ImGui.SliderFloat("Hidden##multh", ref mH, -1f, 1f, "%.2f")) { p.MultHidden = mH; _profilesService.SaveProfiles(); }
        }

        private void RenderWeightTabs()
        {
            if (!ImGui.BeginTabBar("wc_prof_tabs")) return;

            if (ImGui.BeginTabItem("Default"))
            {
                _tableRenderer.RenderWeightsTable("Default", GetDefaultModifiers());
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Desecrated"))
            {
                _tableRenderer.RenderWeightsTable("Desecrated", GetDesecratedModifiers());
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Hidden"))
            {
                _tableRenderer.RenderWeightsTable("Hidden", GetHiddenModifiers());
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        // ===== Updated lists (numbers -> #) =====

        private List<string> GetDefaultModifiers()
        {
            return new List<string>
            {
                "Rare Monsters have # additional Modifier",
                "Area contains # additional packs of Undead",
                "Area contains # additional packs of Beasts",
                "Area contains # additional packs of Ezomyte Monsters",
                "Area contains # additional packs of Bramble Monsters",
                "Area contains # additional packs of Faridun Monsters",
                "Area contains # additional packs of Plagued Monsters",
                "Area contains # additional packs of Vaal Monsters",
                "Area contains # additional packs of Iron Guards",
                "Area contains # additional packs of Transcended Monsters",

                "# increased Magic Monsters",
                "# increased Rare Monsters",

                "Monsters deal Extra Fire Damage",
                "Monsters deal Extra Cold Damage",
                "Monsters deal Extra Lightning Damage",
                "Monsters deal Extra Chaos Damage",

                "Monsters have increased Attack Speed",
                "Monsters have increased Movement Speed",
                "Monsters have increased Cast Speed",
                "Monsters have increased Critical Hit Chance & Damage",

                "Monsters have more Life",
                "Monsters gain Elemental Resistances",
                "Monsters are Armoured",
                "Monsters are Evasive",
                "Monsters gain Energy Shield",

                "Monsters have chance to Poison on Hit",
                "Monsters have chance to inflict Bleeding on Hit",

                "Monsters have increased Ailment Threshold",
                "Monsters have increased Stun Threshold",
                "Monsters Break Armour on Hit",
                "Monsters have increased Accuracy Rating",
                "Monsters have increased Stun Buildup",
                "Monsters have increased Freeze Buildup",
                "Monsters have increased Shock Chance",
                "Monsters inflict increased Flammability Magnitude",

                "Monsters fire additional Projectiles",

                "Players are periodically Cursed with Enfeeble",
                "Players are periodically Cursed with Temporal Chains",
                "Players are periodically Cursed with Elemental Weakness",

                "Area has patches of Ignited Ground",
                "Area has patches of Chilled Ground",
                "Area has patches of Shocked Ground",

                "Monster Damage Penetrates Elemental Resistances",

                "Players have reduced Maximum Resistances",
                "Players gain reduced Flask Charges",
                "Players have less Recovery Rate of Life and ES",
                "Players have less Cooldown Recovery Rate",

                "Monsters take reduced Extra Damage from Crits",
                "Monsters have reduced Effect of Curses",
                "Monsters steal Charges on Hit"
            };
        }

        private List<string> GetDesecratedModifiers()
        {
            // Use the SPECIFIC effect lines for Abyssal, numbers -> #; ignore map drop chance
            return new List<string>
            {
                "Abyssal Monsters grant # increased Experience",
                "Abysses have # additional Pits", // (2–3) pits variant → Hidden: Rarity
                "Abysses in Area spawn # increased Monsters",
                "Area is overrun by the Abyssal", // (14–18) pits + overrun → Hidden: Pack Size
                "Abysses lead to an Abyssal Boss",
                "Area contains an additional Incubator Queen",
                "Abyss Pits have # chance to spawn all Monsters as at least Magic; Abyss Cracks have # chance to spawn all Monsters as at least Magic",
                "Abysses lead to an Abyssal Depths",
                "Monsters from Abysses have increased Difficulty and Reward for each closed Pit in that Abyss",
                "Abyss Pits in Area always have Rewards",
                "Natural Rare Monsters in Area have # extra Abyssal Modifier",

                // Suffixes (specific lines)
                "Natural Rare Monsters in Area Eat the Souls of slain Monsters in their Presence; Players steal the Eaten Souls of Slain Rare Monsters in Area",
                "Players and their Minions deal no damage for 3 out of every 10 seconds",
                "Players have # less Movement and Skill Speed for each time they've used a Skill Recently",
                "Monsters inflict 1 Grasping Vine on Hit",
                "Players are Marked for Death for 10 seconds after killing a Rare or Unique monster",
                "Area has patches of Mana Siphoning Ground",
                "Natural Monster Packs in Area are in a Union of Souls",
                "Natural Rare Monsters in Area are in a Union of Souls with the Map Boss"
            };
        }

        private List<string> GetHiddenModifiers()
        {
            return new List<string>
            {
                "# increased Number of Rare Monsters",
                "# increased Number of Magic Monsters",
                "# increased Item Rarity",
                "# increased Pack Size",
                "# increased Waystones found"
            };
        }
    }
}
