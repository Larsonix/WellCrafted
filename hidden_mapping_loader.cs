// ===============================================
// Mapping/HiddenMappingLoader.cs
// External JSON merge + validation + diagnostics
// (legacy helpers included)
// ===============================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WellCrafted.Diagnostics;

namespace WellCrafted.Mapping
{
    public class HiddenMappingLoader
    {
        private readonly string _seedsPath;
        private readonly string _userPath;

        private List<HiddenMapEntry> _currentMappings = new();

        public HiddenMappingLoader(string pluginDir)
        {
            var dataDir = Path.Combine(pluginDir ?? ".", "data");
            Directory.CreateDirectory(dataDir);

            _seedsPath = Path.Combine(dataDir, "HiddenMap.seeds.json");
            _userPath  = Path.Combine(dataDir, "HiddenMap.user.json");
        }

        public void LoadMappings()
        {
            try
            {
                var builtIn = HiddenMapping.GetBuiltInMappings();
                var seeds = LoadMappingFile(_seedsPath);
                var user  = LoadMappingFile(_userPath);

                var merged = HiddenMapping.MergeMappings(builtIn, seeds);
                _currentMappings = HiddenMapping.MergeMappings(merged, user);

                Logger.Info($"Hidden mappings: built-in {builtIn.Count} + seeds {seeds.Count} + user {user.Count} → total {_currentMappings.Count}");
            }
            catch (Exception ex)
            {
                Logger.Error($"LoadMappings failed: {ex.Message}");
                _currentMappings = HiddenMapping.GetBuiltInMappings();
            }
        }

        private static List<HiddenMapEntry> LoadMappingFile(string path)
        {
            try
            {
                if (!File.Exists(path)) return new();

                var json = File.ReadAllText(path);
                var doc = System.Text.Json.JsonDocument.Parse(json);
                var root = doc.RootElement;

                int schema = root.TryGetProperty("Schema", out var s) ? s.GetInt32() : 2;
                if (schema != 2)
                    Logger.Warning($"Hidden map '{Path.GetFileName(path)}' has unsupported schema {schema}, continuing best-effort.");

                var list = new List<HiddenMapEntry>();
                if (root.TryGetProperty("Mappings", out var mappings))
                {
                    foreach (var el in mappings.EnumerateArray())
                    {
                        var match = el.TryGetProperty("Match", out var m) ? m.GetString() ?? "" : "";
                        var hidden = new List<string>();
                        if (el.TryGetProperty("Hidden", out var hArr))
                            foreach (var h in hArr.EnumerateArray())
                                hidden.Add(h.GetString() ?? "");

                        if (!string.IsNullOrWhiteSpace(match))
                        {
                            list.Add(new HiddenMapEntry
                            {
                                Match = HiddenMapping.Normalize(match),
                                Hidden = hidden.Distinct().ToList()
                            });
                        }
                    }
                }

                return list;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed reading hidden map file '{path}': {ex.Message}");
                return new();
            }
        }

        // Query API
        public List<string> GetHiddenModsFor(string visible)
            => HiddenMapping.GetHiddenModsFor(visible, _currentMappings);

        public List<string> GetAllKnownVisibleMods()
            => HiddenMapping.GetAllKnownVisibleMods(_currentMappings);

        public List<string> GetAllKnownHiddenMods()
            => HiddenMapping.GetAllKnownHiddenMods(_currentMappings);

        // Diagnostics helpers (used by panel_root)
        public void ImportFromBuffer(string buffer) { /* same as previous version */  // keep your existing one if modified
            try
            {
                var lines = (buffer ?? string.Empty).Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                var entries = new List<HiddenMapEntry>();

                foreach (var line in lines)
                {
                    var parts = line.Split(new[] { "=>" }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2) continue;

                    var match = parts[0].Trim();
                    var hidden = parts[1].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                                         .Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x))
                                         .Distinct().ToList();

                    if (!string.IsNullOrWhiteSpace(match) && hidden.Count > 0)
                    {
                        entries.Add(new HiddenMapEntry
                        {
                            Match = HiddenMapping.Normalize(match),
                            Hidden = hidden
                        });
                    }
                }

                var userMap = new HiddenMap { Schema = 2, Mappings = entries };
                var json = System.Text.Json.JsonSerializer.Serialize(userMap, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_userPath, json);
                Logger.Info($"Imported {entries.Count} entries to '{_userPath}'. Reloading...");
                LoadMappings();
            }
            catch (Exception ex)
            {
                Logger.Error($"ImportFromBuffer failed: {ex.Message}");
            }
        }

        public void ExportCurrentMappings()
        {
            try
            {
                var map = new HiddenMap
                {
                    Schema = 2,
                    Mappings = _currentMappings.Select(e => new HiddenMapEntry
                    {
                        Match = e.Match,
                        Hidden = e.Hidden.Distinct().ToList()
                    }).ToList()
                };

                var json = System.Text.Json.JsonSerializer.Serialize(map, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_userPath, json);
                Logger.Info($"Exported {_currentMappings.Count} mappings to '{_userPath}'.");
            }
            catch (Exception ex)
            {
                Logger.Error($"ExportCurrentMappings failed: {ex.Message}");
            }
        }

        public string GetSeedsPath() => _seedsPath;
        public string GetUserPath()  => _userPath;
    }
}
