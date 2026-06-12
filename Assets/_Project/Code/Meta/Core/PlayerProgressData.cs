using System;

namespace LostCity.Meta.Core
{
    [Serializable]
    public sealed class PlayerProgressData
    {
        public int Level = 1;
        public int CurrentExperience;
        public int ExperienceToNextLevel = 5;
        public float AttackMultiplier = 1f;
        public float MaxHpMultiplier = 1f;
        public float FireRateMultiplier = 1f;
        public float CritChanceBonus;
        public float DodgeChanceBonus;
        public int DroneProjectileBonus;
    }
}
