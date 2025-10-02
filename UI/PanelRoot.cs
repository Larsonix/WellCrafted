// ===============================================
// UI/PanelRoot.cs
// Main plugin window controller
// ===============================================

using System;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using WellCrafted.Settings;
using WellCrafted.Profiles;
using WellCrafted.Mapping;
using WellCrafted.Diagnostics;

namespace WellCrafted.UI
{
    public class PanelRoot
    {
        private readonly WellCraftedSettings _settings;
        private readonly ProfilesService _profilesService;
        private readonly ProfilesPanel _profilesPanel;
        private string _editorBuffer = string.Empty;

        public PanelRoot(WellCraftedSettings settings, ProfilesService profilesService)
        {
            _settings = settings;
            _profilesService = profilesService;
            _profilesPanel = new ProfilesPanel(settings, profilesService);
        }

        public void RenderProfilesPanel()
        {
            if (!ImGui.Begin("WellCrafted — Profiles"))
            {
                ImGui.End();
                return;
            }

            try
            {
                _profilesPanel.Render();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error rendering profiles panel: {ex.Message}");
                ImGui.Text("Error rendering profiles panel. Check logs for details.");
            }

            ImGui.End();
        }

        public void RenderDiagnosticsPanel(GameSnapshot snapshot, HiddenMappingLoader mappingLoader)
        {
            if (!ImGui.Begin("WellCrafted — Diagnostics"))
            {
                ImGui.End();
                return;
            }

            try
            {
                RenderSnapshotInfo(snapshot);
                ImGui.Separator();
                RenderMappingEditor(mappingLoader);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error rendering diagnostics panel: {ex.Message}");
                ImGui.Text("Error rendering diagnostics panel. Check logs for details.");
            }

            ImGui.End();
        }

        private void RenderSnapshotInfo(GameSnapshot snapshot)
        {
            ImGui.Text($"Root ok: {(snapshot?.Root != null ? "Yes" : "No")}");
            
            if (snapshot != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    var found = snapshot.Choices[i] != null ? "Yes" : "No";
                    var text = snapshot.ChoiceTexts.Count > i ? (snapshot.ChoiceTexts[i] ?? "n/a") : "n/a";
                    ImGui.Text($"Choice {i + 1} found: {found}   Text: {text}");
                }

                ImGui.Separator();
                ImGui.Text($"Reveal found: {(snapshot.RevealBtn != null ? "Yes" : "No")}");
                ImGui.Text($"Confirm found: {(snapshot.ConfirmBtn != null ? "Yes" : "No")}");
                ImGui.Text($"Reroll found: {(snapshot.RerollBtn != null ? "Yes" : "No")}");
            }
        }

        private void RenderMappingEditor(HiddenMappingLoader mappingLoader)
        {
            ImGui.Text("Mapping Editor");
            ImGui.Text("Seeds: " + mappingLoader.GetSeedsPath());
            ImGui.SameLine();
            ImGui.Text("User: " + mappingLoader.GetUserPath());

            if (ImGui.Button("Reload Mappings"))
            {
                mappingLoader.LoadMappings();
                Logger.Info("Mappings reloaded from disk");
            }

            ImGui.SameLine();
            if (ImGui.Button("Export Current"))
            {
                mappingLoader.ExportCurrentMappings();
            }

            ImGui.Separator();
            ImGui.TextWrapped("Paste lines as: Visible => Hidden1; Hidden2");
            ImGui.InputTextMultiline("##editor", ref _editorBuffer, 65536, new Vector2(600, 220));

            if (ImGui.Button("Import from Buffer"))
            {
                try
                {
                    mappingLoader.ImportFromBuffer(_editorBuffer);
                    _editorBuffer = string.Empty; // Clear buffer on successful import
                }
                catch (Exception ex)
                {
                    Logger.Error($"Import failed: {ex.Message}");
                }
            }
        }
    }
}