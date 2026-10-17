using LostCity.CombatSandbox;
using UnityEngine;

namespace LostCity.Meta
{
    public sealed class CaseSessionManager : MonoBehaviour
    {
        private CaseRegistry registry;
        public string ActiveCaseId { get; private set; }
        public CaseDefinition ActiveCase { get; private set; }
        public bool HasActiveCase => ActiveCase != null;

        public void Initialize(CaseRegistry caseRegistry) => registry = caseRegistry;

        public bool BeginCase(CaseDefinition definition)
        {
            if (HasActiveCase || registry == null || !registry.CanStart(definition, out _)) return false;
            ActiveCase = definition;
            ActiveCaseId = definition.CaseId;
            return true;
        }

        public void EndCase()
        {
            ActiveCase = null;
            ActiveCaseId = null;
        }
    }
}
