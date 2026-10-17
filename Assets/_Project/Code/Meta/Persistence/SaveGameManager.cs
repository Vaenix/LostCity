using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LostCity.Meta.Core;
using UnityEngine;

namespace LostCity.Meta.Persistence
{
    public sealed class SaveGameManager : MonoBehaviour
    {
        private SaveGameData data = SaveGameData.CreateDefault();
        private bool initialized;

        public string SavePath { get; private set; }
        public bool IsReadOnly { get; private set; }
        public bool HasUnsavedChanges { get; private set; }
        public string LastError { get; private set; } = string.Empty;
        public event Action DataChanged;
        public IReadOnlyList<string> CompletedCaseIds => data.CompletedCaseIds.ToArray();
        public IReadOnlyList<string> PermanentUnlockIds => data.PermanentUnlockIds.ToArray();
        public IReadOnlyList<string> DiscoveredClueIds => data.DiscoveredClueIds.ToArray();
        public IReadOnlyList<string> EncounteredEnemyIds => data.EncounteredEnemyIds.ToArray();
        public IReadOnlyList<string> EncounteredBossIds => data.EncounteredBossIds.ToArray();

        public void Initialize(string explicitPath = null)
        {
            if (initialized) return;
            SavePath = explicitPath ?? Path.Combine(Application.persistentDataPath, "lost-city-save.json");
            initialized = true;
            LoadFromDisk();
        }

        public bool IsCaseCompleted(string caseId) => data.CompletedCaseIds.Contains(caseId);
        public PlayerProgressData GetPlayerProgress() => Copy(data.PlayerProgress);
        public SaveGameData GetSnapshot() => JsonUtility.FromJson<SaveGameData>(JsonUtility.ToJson(data));
        public bool CompleteCase(string caseId) => AddId(data.CompletedCaseIds, caseId);
        public bool DiscoverClue(string clueId) => AddId(data.DiscoveredClueIds, clueId);
        public bool EncounterEnemy(string enemyId) => AddId(data.EncounteredEnemyIds, enemyId);
        public bool EncounterBoss(string bossId) => AddId(data.EncounteredBossIds, bossId);
        public bool AddPermanentUnlock(string unlockId) => AddId(data.PermanentUnlockIds, unlockId);

        public void SetPlayerProgress(PlayerProgressData progress)
        {
            SetPlayerProgressAndUnlocks(progress, null);
        }

        public void SetPlayerProgressAndUnlocks(PlayerProgressData progress, IEnumerable<string> unlockIds)
        {
            EnsureInitialized();
            if (IsReadOnly) return;
            var unlocks = unlockIds == null ? new List<string>() : new List<string>(unlockIds);
            var candidate = SaveGameData.CreateDefault();
            candidate.PlayerProgress = Copy(progress ?? new PlayerProgressData());
            SaveDataSanitizer.Sanitize(candidate);
            bool changed = JsonUtility.ToJson(candidate.PlayerProgress) != JsonUtility.ToJson(data.PlayerProgress);
            foreach (string id in unlocks) changed |= SaveDataSanitizer.AddUnique(data.PermanentUnlockIds, id?.Trim());
            if (!changed) return;
            data.PlayerProgress = candidate.PlayerProgress;
            Changed();
        }

        public void SaveNow()
        {
            EnsureInitialized();
            if (IsReadOnly) return;
            string temporaryPath = SavePath + ".tmp";
            bool ownsTemporaryFile = false;
            try
            {
                // Recheck before writing: another process or manager may have upgraded the file.
                if (File.Exists(SavePath))
                {
                    var existing = Parse(File.ReadAllText(SavePath));
                    if (RejectFutureVersion(existing)) return;
                }

                SaveDataSanitizer.Sanitize(data);
                string parent = Path.GetDirectoryName(Path.GetFullPath(SavePath));
                Directory.CreateDirectory(parent);
                byte[] json = new UTF8Encoding(false).GetBytes(JsonUtility.ToJson(data, true));
                using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    ownsTemporaryFile = true;
                    stream.Write(json, 0, json.Length);
                    stream.Flush(true);
                }

                if (File.Exists(SavePath)) File.Replace(temporaryPath, SavePath, SavePath + ".bak");
                else File.Move(temporaryPath, SavePath);
                HasUnsavedChanges = false;
                LastError = string.Empty;
            }
            catch (Exception exception) when (IsStorageException(exception) || exception is ArgumentException)
            {
                HasUnsavedChanges = true;
                Report("Save write failed", exception);
            }
            finally
            {
                try { if (ownsTemporaryFile && File.Exists(temporaryPath)) File.Delete(temporaryPath); }
                catch (Exception exception) when (IsStorageException(exception))
                {
                    Report("Save temporary cleanup failed", exception);
                }
            }
        }

        public void Reload()
        {
            EnsureInitialized();
            if (HasUnsavedChanges && !IsReadOnly)
            {
                SaveNow();
                if (HasUnsavedChanges) return;
            }
            LoadFromDisk();
            DataChanged?.Invoke();
        }

        private void LoadFromDisk()
        {
            IsReadOnly = false;
            LastError = string.Empty;
            try
            {
                // File.Exists suppresses IO errors. Opening directly distinguishes missing from unreadable.
                string json;
                try { json = File.ReadAllText(SavePath); }
                catch (FileNotFoundException) { CreateNewSave(); return; }
                catch (DirectoryNotFoundException) { CreateNewSave(); return; }

                SaveGameData loaded;
                try { loaded = Parse(json); }
                catch (ArgumentException exception)
                {
                    string backup = SavePath + ".corrupt-" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffffff")
                        + "-" + Guid.NewGuid().ToString("N") + ".json";
                    File.Move(SavePath, backup);
                    Report("Corrupt save preserved at " + backup, exception);
                    CreateNewSave();
                    return;
                }

                if (RejectFutureVersion(loaded)) return;
                data = Migrate(loaded);
                HasUnsavedChanges = false;
            }
            catch (Exception exception) when (IsStorageException(exception) || exception is ArgumentException)
            {
                // Never replace a file we could not read or safely back up.
                IsReadOnly = true;
                Report("Save load failed; storage is read-only", exception);
            }
        }

        private void CreateNewSave()
        {
            data = SaveGameData.CreateDefault();
            HasUnsavedChanges = true;
            SaveNow();
        }

        private bool RejectFutureVersion(SaveGameData loaded)
        {
            if (loaded.Version <= SaveGameData.CurrentVersion) return false;
            IsReadOnly = true;
            LastError = $"Unsupported future save version {loaded.Version} at {SavePath}; file left untouched.";
            Debug.LogError(LastError, this);
            return true;
        }

        private static SaveGameData Migrate(SaveGameData loaded)
        {
            // Version 0/missing fields use v1 defaults; future schemas must add explicit migrations here.
            return SaveDataSanitizer.Sanitize(loaded);
        }

        private static SaveGameData Parse(string json)
        {
            string trimmed = json.Trim();
            if (!trimmed.StartsWith("{", StringComparison.Ordinal) || !trimmed.EndsWith("}", StringComparison.Ordinal))
                throw new ArgumentException("Expected a JSON save object.");
            return JsonUtility.FromJson<SaveGameData>(trimmed)
                ?? throw new ArgumentException("JSON did not contain a save object.");
        }

        private bool AddId(List<string> values, string id)
        {
            EnsureInitialized();
            if (IsReadOnly || !SaveDataSanitizer.AddUnique(values, id?.Trim())) return false;
            Changed();
            return true;
        }

        private void Changed()
        {
            HasUnsavedChanges = true;
            SaveNow();
            DataChanged?.Invoke();
        }

        private void EnsureInitialized()
        {
            if (!initialized) throw new InvalidOperationException("Initialize SaveGameManager before using persistence APIs.");
        }

        private void Report(string context, Exception exception)
        {
            LastError = $"{context} ({SavePath}): {exception.GetType().Name}: {exception.Message}";
            Debug.LogError(LastError, this);
        }

        private static bool IsStorageException(Exception exception) => exception is IOException
            || exception is UnauthorizedAccessException || exception is NotSupportedException
            || exception is System.Security.SecurityException;

        private static PlayerProgressData Copy(PlayerProgressData progress)
        {
            return new PlayerProgressData
            {
                Level = progress.Level,
                CurrentExperience = progress.CurrentExperience,
                ExperienceToNextLevel = progress.ExperienceToNextLevel,
                PendingUpgradeChoices = progress.PendingUpgradeChoices,
                AttackMultiplier = progress.AttackMultiplier,
                MaxHpMultiplier = progress.MaxHpMultiplier,
                FireRateMultiplier = progress.FireRateMultiplier,
                CritChanceBonus = progress.CritChanceBonus,
                DodgeChanceBonus = progress.DodgeChanceBonus,
                DroneProjectileBonus = progress.DroneProjectileBonus
            };
        }
    }
}
