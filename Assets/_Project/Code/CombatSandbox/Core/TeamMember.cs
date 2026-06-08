using UnityEngine;

namespace LostCity.CombatSandbox
{
    public sealed class TeamMember : MonoBehaviour
    {
        [SerializeField] private CombatTeam team = CombatTeam.Neutral;

        public CombatTeam Team => team;

        public bool IsHostileTo(TeamMember other)
        {
            return other != null && team != CombatTeam.Neutral && other.team != CombatTeam.Neutral && team != other.team;
        }
    }
}
