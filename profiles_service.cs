// ===============================================
// Profiles/ProfilesService.cs
// Active profile management + persistence (+ create/rename/delete)
// ===============================================

using System;
using System.Collections.Generic;
using System.Linq;
using WellCrafted.Diagnostics;

namespace WellCrafted.Profiles
{
    public class ProfilesService
    {
        private readonly ProfilesStore _store;
        private ProfilesFile _profilesFile;

        public event Action ProfilesChanged;

        public ProfilesService(string directoryPath)
        {
            _store = new ProfilesStore(directoryPath);
            _profilesFile = new ProfilesFile();
            EnsureDefaultExists();
        }

        public void LoadProfiles()
        {
            _profilesFile = _store.LoadProfiles();
            EnsureDefaultExists();
            ProfilesChanged?.Invoke();
        }

        public void SaveProfiles()
        {
            EnsureDefaultExists();
            _store.SaveProfiles(_profilesFile);
            ProfilesChanged?.Invoke();
        }

        public string ActiveProfile
        {
            get => string.IsNullOrWhiteSpace(_profilesFile.Active) ? "Default" : _profilesFile.Active;
            set
            {
                if (_profilesFile.Profiles.ContainsKey(value))
                {
                    _profilesFile.Active = value;
                    SaveProfiles();
                }
            }
        }

        public WeightProfile GetActiveProfile()
        {
            var name = ActiveProfile;
            return _profilesFile.Profiles.TryGetValue(name, out var p)
                ? p
                : GetDefaultProfile();
        }

        public IEnumerable<string> GetProfileNames()
            => _profilesFile.Profiles.Keys.OrderBy(k => k);

        public void UpdateWeight(string category, string normalizedKey, int value, bool save = false)
        {
            var profile = GetActiveProfile();
            var dict = category?.ToLowerInvariant() switch
            {
                "default"    => profile.VisibleDefault,
                "desecrated" => profile.VisibleDesecrated,
                "hidden"     => profile.Hidden,
                _            => null
            };
            if (dict == null) return;

            dict[normalizedKey] = value;
            if (save) SaveProfiles();
        }

        // ── NEW: CRUD used by ProfilesPanel ────────────────────────────────────

        public bool CreateProfile(string name)
        {
            name = (name ?? "").Trim();
            if (string.IsNullOrEmpty(name)) return false;
            if (_profilesFile.Profiles.ContainsKey(name)) return false;

            _profilesFile.Profiles[name] = GetDefaultProfile();
            SaveProfiles();
            return true;
        }

        public bool DeleteProfile(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            if (name.Equals("Default", StringComparison.OrdinalIgnoreCase)) return false;
            if (!_profilesFile.Profiles.ContainsKey(name)) return false;

            _profilesFile.Profiles.Remove(name);
            if (ActiveProfile.Equals(name, StringComparison.OrdinalIgnoreCase))
                _profilesFile.Active = "Default";

            SaveProfiles();
            return true;
        }

        public bool RenameProfile(string oldName, string newName)
        {
            oldName = (oldName ?? "").Trim();
            newName = (newName ?? "").Trim();

            if (string.IsNullOrEmpty(oldName) || string.IsNullOrEmpty(newName)) return false;
            if (!_profilesFile.Profiles.ContainsKey(oldName)) return false;
            if (_profilesFile.Profiles.ContainsKey(newName)) return false;

            var payload = _profilesFile.Profiles[oldName];
            _profilesFile.Profiles.Remove(oldName);
            _profilesFile.Profiles[newName] = payload;

            if (ActiveProfile.Equals(oldName, StringComparison.OrdinalIgnoreCase))
                _profilesFile.Active = newName;

            SaveProfiles();
            return true;
        }

        // ── Internals ─────────────────────────────────────────────────────────

        private static WeightProfile GetDefaultProfile()
        {
            return new WeightProfile
            {
                MultDefault    = 0f,
                MultDesecrated = 0f,
                MultHidden     = 0f
            };
        }

        private void EnsureDefaultExists()
        {
            if (!_profilesFile.Profiles.ContainsKey("Default"))
            {
                _profilesFile.Profiles["Default"] = GetDefaultProfile();
                _profilesFile.Active = "Default";
            }
        }
    }
}
