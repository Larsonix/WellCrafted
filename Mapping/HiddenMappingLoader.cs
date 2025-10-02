// ===============================================
// Mapping/HiddenMappingLoader.cs
// External JSON merge + validation + diagnostics
// ===============================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
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
                var seeds   = LoadMappingFile(_seedsPath);
                var user    = LoadMappingFile(_userPath);

                var merged  = HiddenMapping.MergeMappings(builtIn, seeds);
                _currentMappings = HiddenMapping.MergeMappings(merged, user);

                Logger.Info($"Hidden mappings: built-in {builtIn.Count} + seeds {seeds.Count} + user {user.Count} → total {_currentMappings.Count}");
            }
            catch (Exception ex)
            {
                Logger.Error($"LoadMappings failed: {ex.Message}");
                if (_currentMappings == null || _currentMappings.Count == 0)
                    _currentMappings = HiddenMapping.GetBuiltInMappings();
            }
        }

        private static List<HiddenMapEntry> LoadMappingFile(string path)
        {
            try
            {
                if (!File.Exists(path)) return new();

                var json = File.ReadAllText(path);

                // Case-insensitive property names (handles "schema"/"mappings")
                var map = JsonSerializer.Deserialize<HiddenMap>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (map?.Mappings == null || map.Mappings.Count == 0)
                    return new();

                var list = new List<HiddenMapEntry>(map.Mappings.Count);
                foreach (var e in map.Mappings)
                {
                    if (e == null || string.IsNullOrWhiteSpace(e.Match)) continue;

                    list.Add(new HiddenMapEntry
                    {
                        Match  = HiddenMapping.Normalize(e.Match),
                        Hidden = (e.Hidden ?? new List<string>())
                                .Where(h => !string.IsNullOrWhiteSpace(h))
                                .Distinct(StringComparer.OrdinalIgnoreCase)
                                .ToList()
                    });
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
        public void ImportFromBuffer(string buffer)
        {
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
                                         .Select(x => x.Trim())
                                         .Where(x => !string.IsNullOrWhiteSpace(x))
                                         .Distinct(StringComparer.OrdinalIgnoreCase)
                                         .ToList();

                    if (!string.IsNullOrWhiteSpace(match) && hidden.Count > 0)
                    {
                        entries.Add(new HiddenMapEntry
                        {
                            Match  = HiddenMapping.Normalize(match),
                            Hidden = hidden
                        });
                    }
                }

                var userMap = new HiddenMap { Schema = 2, Mappings = entries };
                var outJson = JsonSerializer.Serialize(userMap, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path: _userPath, outJson);

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
                        Match  = e.Match,
                        Hidden = (e.Hidden ?? new List<string>()).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
                    }).ToList()
                };

                var json = JsonSerializer.Serialize(map, new JsonSerializerOptions { WriteIndented = true });
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
