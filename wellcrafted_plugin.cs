// ===============================================
// WellCraftedPlugin.cs
// Plugin: WellCrafted (ExileCore2 / PoE2)
// Version: 1.4.0 (robust init + lazy self-heal + deep snapshot diagnostics)
// ===============================================

using System;
using System.Collections.Generic;
using System.Linq;
using ExileCore2;
using ExileCore2.Shared.Cache;
using ExileCore2.Shared.Interfaces;
using WellCrafted.Settings;
using WellCrafted.Profiles;
using WellCrafted.Mapping;
using WellCrafted.UI;
using WellCrafted.Overlay;
using WellCrafted.Util;
// Alias to disambiguate from ExileCore2.Logger
using Logger = WellCrafted.Diagnostics.Logger;

namespace WellCrafted
{
    public sealed class WellCraftedPlugin : BaseSettingsPlugin<WellCraftedSettings>
    {
        // Core Services
        private ProfilesService _profilesService;
        private HiddenMappingLoader _mappingLoader;
        private PanelRoot _panelRoot;
        private OverlayRenderer _overlayRenderer;

        // Cache for game state snapshots
        private TimeCache<GameSnapshot> _snapCache;

        // UI State
        private bool _showOverlay = true;
        private bool _initOk = false;

        public WellCraftedPlugin()
        {
            Name = "WellCrafted";
        }

        // ---------------------------------------------
        // Lifecycle
        // ---------------------------------------------
        public override bool Initialise()
        {
            Logger.Info("WellCrafted Initialising...");

            try
            {
                // Construct all services/components FIRST
                _profilesService = new ProfilesService(this.DirectoryFullName);
                _mappingLoader   = new HiddenMappingLoader(this.DirectoryFullName);
                _panelRoot       = new PanelRoot(Settings, _profilesService);
                _overlayRenderer = new OverlayRenderer(Settings, _mappingLoader, _profilesService);

                // Snapshot cache (created after services exist)
                _snapCache = new TimeCache<GameSnapshot>(CaptureSnapshot, 100);

                // Load data (independent guarded blocks)
                TryLoadMappings();
                TryLoadProfiles();

                _initOk = true;
                Logger.Info("WellCrafted initialised.");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Initialise fatal: {ex.Message}");
                // Still try to make fields non-null so Render can recover
                LazyEnsureServices();
                _initOk = false;
                return false;
            }
        }

        public override void Tick()
        {
            if (Settings == null || !Settings.Enable) return;

            if (Settings.ToggleOverlay.PressedOnce())
            {
                _showOverlay = !_showOverlay;
                Logger.Info($"Overlay: {(_showOverlay ? "ON" : "OFF")} (initOk={_initOk})");
            }
        }

        public override void Render()
        {
            if (Settings == null || !Settings.Enable) return;

            // Lazy self-heal: if anything core is missing, rebuild it here
            if (!ServicesReady())
            {
                LazyEnsureServices();
                if (!ServicesReady())
                {
                    Logger.Error(
                        $"Render aborted. svc:{_profilesService!=null} map:{_mappingLoader!=null} panel:{_panelRoot!=null} overlay:{_overlayRenderer!=null}");
                    return;
                }
            }

            GameSnapshot snapshot = null;
            try
            {
                snapshot = _snapCache != null ? _snapCache.Value : CaptureSnapshot();
            }
            catch (Exception ex)
            {
                Logger.Error($"Snapshot error: {ex.Message}");
                snapshot = new GameSnapshot { Notes = { "Snapshot exception: " + ex.Message } };
            }

            // Overlay
            try
            {
                if (_showOverlay)
                    _overlayRenderer.RenderOverlay(snapshot);
            }
            catch (Exception ex)
            {
                Logger.Error($"Overlay render error: {ex.Message}");
            }

            // Profiles panel
            try
            {
                if (Settings.ProfilesUI != null && Settings.ProfilesUI.ShowProfilesTab.Value)
                    _panelRoot.RenderProfilesPanel();
            }
            catch (Exception ex)
            {
                Logger.Error($"Profiles panel error: {ex.Message}");
            }

            // Diagnostics panel
            try
            {
                if (Settings.Diagnostics != null && Settings.Diagnostics.ShowDiagnosticsTab.Value)
                    _panelRoot.RenderDiagnosticsPanel(snapshot, _mappingLoader);
            }
            catch (Exception ex)
            {
                Logger.Error($"Diagnostics panel error: {ex.Message}");
            }
        }

        // ---------------------------------------------
        // Init helpers
        // ---------------------------------------------
        private void TryLoadMappings()
        {
            try
            {
                _mappingLoader.LoadMappings();
            }
            catch (Exception ex)
            {
                Logger.Error($"Hidden mappings load failed: {ex.Message}");
            }
        }

        private void TryLoadProfiles()
        {
            try
            {
                _profilesService.LoadProfiles();
            }
            catch (Exception ex)
            {
                Logger.Error($"Profiles load failed: {ex.Message}");
            }
        }

        private bool ServicesReady()
        {
            return _profilesService != null && _mappingLoader != null &&
                   _panelRoot != null && _overlayRenderer != null;
        }

        private void LazyEnsureServices()
        {
            try
            {
                if (_profilesService == null) _profilesService = new ProfilesService(this.DirectoryFullName);
            }
            catch (Exception ex) { Logger.Error($"Lazy init ProfilesService failed: {ex.Message}"); }

            try
            {
                if (_mappingLoader == null) _mappingLoader = new HiddenMappingLoader(this.DirectoryFullName);
            }
            catch (Exception ex) { Logger.Error($"Lazy init HiddenMappingLoader failed: {ex.Message}"); }

            try
            {
                if (_panelRoot == null) _panelRoot = new PanelRoot(Settings, _profilesService);
            }
            catch (Exception ex) { Logger.Error($"Lazy init PanelRoot failed: {ex.Message}"); }

            try
            {
                if (_overlayRenderer == null) _overlayRenderer = new OverlayRenderer(Settings, _mappingLoader, _profilesService);
            }
            catch (Exception ex) { Logger.Error($"Lazy init OverlayRenderer failed: {ex.Message}"); }

            try
            {
                if (_snapCache == null) _snapCache = new TimeCache<GameSnapshot>(CaptureSnapshot, 100);
            }
            catch (Exception ex) { Logger.Error($"Lazy init Snapshot cache failed: {ex.Message}"); }
        }

        // ---------------------------------------------
        // Snapshot capture (deep diagnostics)
        // ---------------------------------------------
        private GameSnapshot CaptureSnapshot()
        {
            var s = new GameSnapshot();

            try
            {
                var ui = GameController?.Game?.IngameState?.IngameUi;
                if (ui == null)
                {
                    s.Notes.Add("IngameUi is null");
                    return s;
                }

                var rootIndex = Settings.DevTree.RootIndex.Value;
                var root = ui.Children?.ElementAtOrDefault(rootIndex);
                if (root == null)
                {
                    s.Notes.Add($"Root child at index {rootIndex} is null");
                    return s;
                }
                if (!root.IsVisible)
                {
                    s.Notes.Add("Root is not visible");
                    return s;
                }

                s.Root = root;
                s.RootOk = true;

                // Follow configured paths
                s.RevealBtn  = FollowChain(root, Parse(Settings.DevTree.RevealPath.Value));
                s.ConfirmBtn = FollowChain(root, Parse(Settings.DevTree.ConfirmPath.Value));
                s.RerollBtn  = FollowChain(root, Parse(Settings.DevTree.RerollPath.Value));

                s.PathOk.Reveal  = s.RevealBtn  != null;
                s.PathOk.Confirm = s.ConfirmBtn != null;
                s.PathOk.Reroll  = s.RerollBtn  != null;

                // Choices
                var p1 = Parse(Settings.DevTree.Choice1Path.Value);
                var p2 = Parse(Settings.DevTree.Choice2Path.Value);
                var p3 = Parse(Settings.DevTree.Choice3Path.Value);

                s.Choices[0] = FollowChain(root, p1);
                s.Choices[1] = FollowChain(root, p2);
                s.Choices[2] = FollowChain(root, p3);

                for (int i = 0; i < 3; i++)
                {
                    s.ChoiceVisible[i] = s.Choices[i]?.IsVisible == true;
                    if (s.Choices[i] == null) s.Notes.Add($"Choice {i+1} element is null");
                    else if (!s.Choices[i].IsVisible) s.Notes.Add($"Choice {i+1} not visible");
                }

                // Extract text
                for (int i = 0; i < 3; i++)
                {
                    var element = s.Choices[i];
                    var text = Guard.GetElementText(element);
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        s.Notes.Add($"Choice {i+1} text is empty");
                        s.ChoiceTextOk[i] = false;
                        s.ChoiceTexts.Add(string.Empty);
                    }
                    else
                    {
                        s.ChoiceTextOk[i] = true;
                        s.ChoiceTexts.Add(text);
                    }
                }

                // Summaries for quick glance in logs
                s.Notes.Add($"RootOk:{s.RootOk}, Paths(R/C/Rr):{s.PathOk.Reveal}/{s.PathOk.Confirm}/{s.PathOk.Reroll}, " +
                            $"Choices vis:{Bool3(s.ChoiceVisible)} texts:{Bool3(s.ChoiceTextOk)}");

            }
            catch (Exception ex)
            {
                s.Notes.Add("CaptureSnapshot exception: " + ex.Message);
            }

            return s;
        }

        private string Bool3(bool[] arr)
        {
            bool a = arr.ElementAtOrDefault(0);
            bool b = arr.ElementAtOrDefault(1);
            bool c = arr.ElementAtOrDefault(2);
            return $"{(a?'Y':'n')},{(b?'Y':'n')},{(c?'Y':'n')}";
        }

        private int[] Parse(string csv)
        {
            try
            {
                return csv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                          .Select(x => int.Parse(x.Trim())).ToArray();
            }
            catch
            {
                return Array.Empty<int>();
            }
        }

        private ExileCore2.PoEMemory.Element FollowChain(ExileCore2.PoEMemory.Element start, int[] chain)
        {
            var current = start;
            foreach (var index in chain)
            {
                var children = current?.Children;
                if (children == null || index < 0 || index >= children.Count)
                    return null;
                current = children[index];
            }
            return current;
        }
    }

    // ---------------------------------------------
    // Game state snapshot with diagnostics
    // ---------------------------------------------
    public class GameSnapshot
    {
        public ExileCore2.PoEMemory.Element Root;
        public ExileCore2.PoEMemory.Element RevealBtn;
        public ExileCore2.PoEMemory.Element ConfirmBtn;
        public ExileCore2.PoEMemory.Element RerollBtn;
        public readonly ExileCore2.PoEMemory.Element[] Choices = new ExileCore2.PoEMemory.Element[3];

        // Extracted text
        public readonly List<string> ChoiceTexts = new();

        // Diagnostics
        public bool RootOk = false;
        public (bool Reveal, bool Confirm, bool Reroll) PathOk = (false, false, false);
        public readonly bool[] ChoiceVisible = new bool[3];
        public readonly bool[] ChoiceTextOk  = new bool[3];
        public readonly List<string> Notes = new();
    }
}
