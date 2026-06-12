using System.Collections.Generic;
using NUnit.Framework;

namespace LostCity.Meta.Core.Tests
{
    public sealed class SaveGameDataTests
    {
        [Test]
        public void CreateDefault_InitializesEveryRequiredCollectionAndPlayerValue()
        {
            SaveGameData data = SaveGameData.CreateDefault();

            Assert.That(data.Version, Is.EqualTo(SaveGameData.CurrentVersion));
            Assert.That(data.CompletedCaseIds, Is.Empty);
            Assert.That(data.PermanentUnlockIds, Is.Empty);
            Assert.That(data.DiscoveredClueIds, Is.Empty);
            Assert.That(data.EncounteredEnemyIds, Is.Empty);
            Assert.That(data.EncounteredBossIds, Is.Empty);
            Assert.That(data.PlayerProgress.Level, Is.EqualTo(1));
            Assert.That(data.PlayerProgress.CurrentExperience, Is.Zero);
            Assert.That(data.PlayerProgress.ExperienceToNextLevel, Is.EqualTo(5));
            Assert.That(data.PlayerProgress.AttackMultiplier, Is.EqualTo(1f));
            Assert.That(data.PlayerProgress.MaxHpMultiplier, Is.EqualTo(1f));
            Assert.That(data.PlayerProgress.FireRateMultiplier, Is.EqualTo(1f));
            Assert.That(data.PlayerProgress.CritChanceBonus, Is.Zero);
            Assert.That(data.PlayerProgress.DodgeChanceBonus, Is.Zero);
            Assert.That(data.PlayerProgress.DroneProjectileBonus, Is.Zero);
        }

        [Test]
        public void Sanitize_NullDataCreatesDefaultSave()
        {
            SaveGameData data = SaveDataSanitizer.Sanitize(null);

            Assert.That(data, Is.Not.Null);
            Assert.That(data.Version, Is.EqualTo(SaveGameData.CurrentVersion));
            Assert.That(data.CompletedCaseIds, Is.Not.Null);
            Assert.That(data.PermanentUnlockIds, Is.Not.Null);
            Assert.That(data.DiscoveredClueIds, Is.Not.Null);
            Assert.That(data.EncounteredEnemyIds, Is.Not.Null);
            Assert.That(data.EncounteredBossIds, Is.Not.Null);
            Assert.That(data.PlayerProgress, Is.Not.Null);
        }

        [Test]
        public void Sanitize_RepairsNullCollectionsAndPlayerProgress()
        {
            SaveGameData data = SaveGameData.CreateDefault();
            data.CompletedCaseIds = null;
            data.PermanentUnlockIds = null;
            data.DiscoveredClueIds = null;
            data.EncounteredEnemyIds = null;
            data.EncounteredBossIds = null;
            data.PlayerProgress = null;

            SaveDataSanitizer.Sanitize(data);

            Assert.That(data.CompletedCaseIds, Is.Empty);
            Assert.That(data.PermanentUnlockIds, Is.Empty);
            Assert.That(data.DiscoveredClueIds, Is.Empty);
            Assert.That(data.EncounteredEnemyIds, Is.Empty);
            Assert.That(data.EncounteredBossIds, Is.Empty);
            Assert.That(data.PlayerProgress.Level, Is.EqualTo(1));
        }

        [Test]
        public void Sanitize_RemovesBlankAndDuplicateIdsFromEveryCollection()
        {
            SaveGameData data = SaveGameData.CreateDefault();
            data.CompletedCaseIds = CorruptIds("room_304");
            data.PermanentUnlockIds = CorruptIds("unlock_drone");
            data.DiscoveredClueIds = CorruptIds("clue_chart");
            data.EncounteredEnemyIds = CorruptIds("enemy_fragment");
            data.EncounteredBossIds = CorruptIds("boss_warden");

            SaveDataSanitizer.Sanitize(data);

            Assert.That(data.CompletedCaseIds, Is.EqualTo(new[] { "room_304" }));
            Assert.That(data.PermanentUnlockIds, Is.EqualTo(new[] { "unlock_drone" }));
            Assert.That(data.DiscoveredClueIds, Is.EqualTo(new[] { "clue_chart" }));
            Assert.That(data.EncounteredEnemyIds, Is.EqualTo(new[] { "enemy_fragment" }));
            Assert.That(data.EncounteredBossIds, Is.EqualTo(new[] { "boss_warden" }));
        }

        [Test]
        public void Sanitize_RepairsInvalidPlayerProgress()
        {
            SaveGameData data = SaveGameData.CreateDefault();
            data.PlayerProgress.Level = 0;
            data.PlayerProgress.CurrentExperience = -1;
            data.PlayerProgress.ExperienceToNextLevel = 0;
            data.PlayerProgress.AttackMultiplier = -2f;
            data.PlayerProgress.MaxHpMultiplier = float.NaN;
            data.PlayerProgress.FireRateMultiplier = float.PositiveInfinity;
            data.PlayerProgress.CritChanceBonus = -0.5f;
            data.PlayerProgress.DodgeChanceBonus = float.NaN;
            data.PlayerProgress.DroneProjectileBonus = -3;

            SaveDataSanitizer.Sanitize(data);

            Assert.That(data.PlayerProgress.Level, Is.EqualTo(1));
            Assert.That(data.PlayerProgress.CurrentExperience, Is.Zero);
            Assert.That(data.PlayerProgress.ExperienceToNextLevel, Is.EqualTo(1));
            Assert.That(data.PlayerProgress.AttackMultiplier, Is.EqualTo(1f));
            Assert.That(data.PlayerProgress.MaxHpMultiplier, Is.EqualTo(1f));
            Assert.That(data.PlayerProgress.FireRateMultiplier, Is.EqualTo(1f));
            Assert.That(data.PlayerProgress.CritChanceBonus, Is.Zero);
            Assert.That(data.PlayerProgress.DodgeChanceBonus, Is.Zero);
            Assert.That(data.PlayerProgress.DroneProjectileBonus, Is.Zero);
        }

        [Test]
        public void Sanitize_UpgradesOldVersionWithoutReplacingValidProgress()
        {
            SaveGameData data = SaveGameData.CreateDefault();
            data.Version = 0;
            data.CompletedCaseIds.Add("room_304");
            data.PlayerProgress.Level = 4;
            data.PlayerProgress.CurrentExperience = 3;
            data.PlayerProgress.AttackMultiplier = 1.25f;

            SaveDataSanitizer.Sanitize(data);

            Assert.That(data.Version, Is.EqualTo(SaveGameData.CurrentVersion));
            Assert.That(data.CompletedCaseIds, Is.EqualTo(new[] { "room_304" }));
            Assert.That(data.PlayerProgress.Level, Is.EqualTo(4));
            Assert.That(data.PlayerProgress.CurrentExperience, Is.EqualTo(3));
            Assert.That(data.PlayerProgress.AttackMultiplier, Is.EqualTo(1.25f));
        }

        [Test]
        public void AddUnique_ReturnsFalseForBlankOrExistingId()
        {
            List<string> ids = new List<string>();

            Assert.That(SaveDataSanitizer.AddUnique(ids, "room_304"), Is.True);
            Assert.That(SaveDataSanitizer.AddUnique(ids, "room_304"), Is.False);
            Assert.That(SaveDataSanitizer.AddUnique(ids, ""), Is.False);
            Assert.That(SaveDataSanitizer.AddUnique(ids, " "), Is.False);
            Assert.That(ids, Is.EqualTo(new[] { "room_304" }));
        }

        private static List<string> CorruptIds(string validId)
        {
            return new List<string> { validId, "", " ", null, $" {validId} ", validId };
        }
    }
}
