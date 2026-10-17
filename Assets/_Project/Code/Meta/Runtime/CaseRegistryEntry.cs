using LostCity.CombatSandbox;
using LostCity.Meta.Core;

namespace LostCity.Meta
{
    public sealed class CaseRegistryEntry
    {
        public CaseDefinition Definition { get; }
        public ChapterDefinition Chapter { get; }
        public CaseState State { get; }
        public bool CanStart { get; }
        public string StartBlockReason { get; }

        public CaseRegistryEntry(CaseDefinition definition, ChapterDefinition chapter,
            CaseState state, bool canStart, string startBlockReason)
        {
            Definition = definition;
            Chapter = chapter;
            State = state;
            CanStart = canStart;
            StartBlockReason = startBlockReason;
        }
    }
}
