using System;
using System.Collections.Generic;

namespace LostCity.Meta.Core
{
    public static class SaveDataSanitizer
    {
        public static SaveGameData Sanitize(SaveGameData data)
        {
            data ??= SaveGameData.CreateDefault();
            data.Version = SaveGameData.CurrentVersion;
            data.CompletedCaseIds = SanitizeIds(data.CompletedCaseIds);
            data.PermanentUnlockIds = SanitizeIds(data.PermanentUnlockIds);
            data.DiscoveredClueIds = SanitizeIds(data.DiscoveredClueIds);
            data.EncounteredEnemyIds = SanitizeIds(data.EncounteredEnemyIds);
            data.EncounteredBossIds = SanitizeIds(data.EncounteredBossIds);
            data.PlayerProgress ??= new PlayerProgressData();
            data.PlayerProgress.Level = Math.Max(1, data.PlayerProgress.Level);
            data.PlayerProgress.CurrentExperience = Math.Max(0, data.PlayerProgress.CurrentExperience);
            data.PlayerProgress.ExperienceToNextLevel = Math.Max(1, data.PlayerProgress.ExperienceToNextLevel);
            data.PlayerProgress.AttackMultiplier = PositiveOrOne(data.PlayerProgress.AttackMultiplier);
            data.PlayerProgress.MaxHpMultiplier = PositiveOrOne(data.PlayerProgress.MaxHpMultiplier);
            data.PlayerProgress.FireRateMultiplier = PositiveOrOne(data.PlayerProgress.FireRateMultiplier);
            data.PlayerProgress.CritChanceBonus = NonNegativeOrZero(data.PlayerProgress.CritChanceBonus);
            data.PlayerProgress.DodgeChanceBonus = NonNegativeOrZero(data.PlayerProgress.DodgeChanceBonus);
            data.PlayerProgress.DroneProjectileBonus = Math.Max(0, data.PlayerProgress.DroneProjectileBonus);
            return data;
        }

        public static bool AddUnique(List<string> values, string id)
        {
            if (values == null || string.IsNullOrWhiteSpace(id) || values.Contains(id))
            {
                return false;
            }

            values.Add(id);
            return true;
        }

        private static List<string> SanitizeIds(List<string> source)
        {
            List<string> result = new List<string>();
            if (source == null)
            {
                return result;
            }

            for (int i = 0; i < source.Count; i++)
            {
                AddUnique(result, source[i]?.Trim());
            }

            return result;
        }

        private static float PositiveOrOne(float value)
        {
            return IsFinite(value) && value > 0f ? value : 1f;
        }

        private static float NonNegativeOrZero(float value)
        {
            return IsFinite(value) && value >= 0f ? value : 0f;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
