using UnityEngine;

namespace LostCity.CombatSandbox
{
    public readonly struct DamageInfo
    {
        public DamageInfo(float amount, CombatTeam sourceTeam, GameObject source, Vector3 hitPoint)
        {
            Amount = amount;
            SourceTeam = sourceTeam;
            Source = source;
            HitPoint = hitPoint;
        }

        public float Amount { get; }
        public CombatTeam SourceTeam { get; }
        public GameObject Source { get; }
        public Vector3 HitPoint { get; }
    }
}
