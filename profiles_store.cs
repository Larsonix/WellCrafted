// ===============================================
// Profiles/ProfilesStore.cs
// Load/save JSON, schema versioning, migrations
// ===============================================

using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using WellCrafted.Diagnostics;

namespace WellCrafted.Profiles
{
    public class ProfilesStore
    {
        private readonly string _profilesPath;
        
        private static readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        public ProfilesStore(string directoryPath)
        {
            _profilesPath = GetProfilesPath(directoryPath);
        }

        public ProfilesFile LoadProfiles()
        {
            try
            {
                if (!File.Exists(_profilesPath))
                {
                    Logger.Info($"No profiles file found, creating default at: {_profilesPath}");
                    return CreateDefaultProfiles();
                }

                var json = File.ReadAllText(_profilesPath);
                var profilesFile = JsonSerializer.Deserialize<ProfilesFile>(json, JsonOpts);

                if (profilesFile == null)
                {
                    Logger.Warning("Failed to deserialize profiles file, using defaults");
                    return CreateDefaultProfiles();
                }

                // Handle schema migration
                if (profilesFile.Schema < 2)
                {
                    Logger.Info($"Migrating profiles from schema {profilesFile.Schema} to 2");
                    profilesFile = MigrateProfilesSchema(json, profilesFile);
                }

                // Normalize all keys
                NormalizeProfileKeys(profilesFile);

                Logger.Info($"Loaded {profilesFile.Profiles.Count} profiles successfully");
                return profilesFile;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error loading profiles: {ex.Message}");
                return CreateDefaultProfiles();
            }
        }

        public void SaveProfiles(ProfilesFile profilesFile)
        {
            try
            {
                profilesFile.Schema = 2;

                // Create backup if file exists
                CreateBackup();

                var json = JsonSerializer.Serialize(profilesFile, JsonOpts);
                File.WriteAllText(_profilesPath, json);

                Logger.Info("Profiles saved successfully");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error saving profiles: {ex.Message}");
            }
        }

        private ProfilesFile CreateDefaultProfiles()
        {
            var profilesFile = new ProfilesFile { Schema = 2 };
            
            var defaultProfile = new WeightProfile
            {
                MultDefault = 0.0f,
                MultDesecrated = 0.0f,
                MultHidden = 0.0f
            };

            // Add some default weights with normalized keys
            defaultProfile.Hidden[Normalize("Map Item Drop Chance")] = 3f;
            defaultProfile.VisibleDefault[Normalize("pack size")] = 2f;
            defaultProfile.VisibleDefault[Normalize("quantity")] = 2f;
            defaultProfile.VisibleDefault[Normalize("rarity")] = 0f;

            profilesFile.Profiles["Default"] = defaultProfile;
            profilesFile.Active = "Default";

            return profilesFile;
        }

        private ProfilesFile MigrateProfilesSchema(string json, ProfilesFile profilesFile)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("Profiles", out var profilesElement))
                {
                    foreach (var name in profilesFile.Profiles.Keys.ToList())
                    {
                        var migrated = new WeightProfile
                        {
                            MultDefault = 0.0f,
                            MultDesecrated = 0.0f,
                            MultHidden = 0.0f
                        };

                        if (profilesElement.TryGetProperty(name, out var profileElement))
                        {
                            MigrateWeightDictionary(profileElement, "Visible", migrated.VisibleDefault);
                            MigrateWeightDictionary(profileElement, "Hidden", migrated.Hidden);
                        }

                        profilesFile.Profiles[name] = migrated;
                    }
                }

                profilesFile.Schema = 2;
                return profilesFile;
            }
            catch (Exception ex)
            {
                Logger.Warning($"Schema migration failed: {ex.Message}");
                return profilesFile;
            }
        }

        private void MigrateWeightDictionary(JsonElement profileElement, string propertyName, System.Collections.Generic.Dictionary<string, float> target)
        {
            if (profileElement.TryGetProperty(propertyName, out var weightElement) && 
                weightElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in weightElement.EnumerateObject())
                {
                    float value = 0f;
                    if (prop.Value.ValueKind == JsonValueKind.Number)
                    {
                        if (prop.Value.TryGetInt32(out var intVal))
                            value = intVal;
                        else if (prop.Value.TryGetDouble(out var doubleVal))
                            value = (float)doubleVal;
                    }

                    target[Normalize(prop.Name)] = value;
                }
            }
        }

        private void NormalizeProfileKeys(ProfilesFile profilesFile)
        {
            foreach (var profile in profilesFile.Profiles.Values)
            {
                profile.VisibleDefault = NormalizeDictionary(profile.VisibleDefault);
                profile.VisibleDesecrated = NormalizeDictionary(profile.VisibleDesecrated);
                profile.Hidden = NormalizeDictionary(profile.Hidden);
            }
        }

        private System.Collections.Generic.Dictionary<string, float> NormalizeDictionary(System.Collections.Generic.Dictionary<string, float> source)
        {
            var result = new System.Collections.Generic.Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in source)
            {
                result[Normalize(kvp.Key)] = kvp.Value;
            }
            return result;
        }

        private void CreateBackup()
        {
            try
            {
                if (!File.Exists(_profilesPath)) return;

                var dir = Path.GetDirectoryName(_profilesPath);
                var backupDir = Path.Combine(dir ?? "", "Backups");
                Directory.CreateDirectory(backupDir);

                var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
                var backupFile = Path.Combine(backupDir, $"WellCraftedProfiles_{timestamp}.json");
                File.Copy(_profilesPath, backupFile, true);

                // Keep only the 3 most recent backups
                var backups = new DirectoryInfo(backupDir)
                    .GetFiles("WellCraftedProfiles_*.json")
                    .OrderByDescending(f => f.CreationTimeUtc)
                    .Skip(3);

                foreach (var backup in backups)
                {
                    try { backup.Delete(); } catch { }
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"Backup creation failed: {ex.Message}");
            }
        }

        private string GetProfilesPath(string directoryPath)
        {
            try
            {
                if (!string.IsNullOrEmpty(directoryPath) && Directory.Exists(directoryPath))
                    return Path.Combine(directoryPath, "WellCraftedProfiles.json");
            }
            catch { }

            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(baseDir, "Plugins", "Source", "WellCrafted", "WellCraftedProfiles.json");
        }

        private string Normalize(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            var result = input.ToLowerInvariant().Trim();
            result = result.Replace('–', '-').Replace('—', '-');
            
            // Remove numeric ranges and values
            result = System.Text.RegularExpressions.Regex.Replace(
                result,
                @"\(\s*\d+(?:[.,]\d+)?\s*[-–—]\s*\d+(?:[.,]\d+)?\s*%?\s*\)",
                string.Empty
            );
            
            // Remove bracketed values and numbers
            result = System.Text.RegularExpressions.Regex.Replace(
                result,
                @"\[\s*#\s*\]|\[\s*\d+\s*\]|#",
                string.Empty
            );
            
            // Remove standalone numbers and percentages
            result = System.Text.RegularExpressions.Regex.Replace(
                result,
                @"[-+]?\d+(?:[.,]\d+)?%?",
                string.Empty
            );
            
            // Keep only letters, numbers, spaces, and hyphens
            result = System.Text.RegularExpressions.Regex.Replace(
                result,
                @"[^a-z0-9 \-]",
                " "
            );
            
            // Collapse multiple spaces
            result = System.Text.RegularExpressions.Regex.Replace(result, @"\s{2,}", " ").Trim();

            return result;
        }
    }
}