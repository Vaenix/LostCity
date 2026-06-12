using System;
using System.Collections.Generic;

namespace LostCity.Meta.Core
{
    [Serializable]
    public sealed class SaveGameData
    {
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;
        public List<string> CompletedCaseIds = new List<string>();
        public List<string> PermanentUnlockIds = new List<string>();
        public List<string> DiscoveredClueIds = new List<string>();
        public List<string> EncounteredEnemyIds = new List<string>();
        public List<string> EncounteredBossIds = new List<string>();
        public PlayerProgressData PlayerProgress = new PlayerProgressData();

        public static SaveGameData CreateDefault()
        {
            return new SaveGameData();
        }
    }
}
