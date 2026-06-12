namespace LostCity.Meta.Core
{
    public sealed class CaseProgressionInput
    {
        public CaseProgressionInput(string[] orderedCaseIds, string[] requiredCompletedCaseIds)
        {
            OrderedCaseIds = orderedCaseIds ?? new string[0];
            RequiredCompletedCaseIds = requiredCompletedCaseIds ?? new string[0];
        }

        public string[] OrderedCaseIds { get; }
        public string[] RequiredCompletedCaseIds { get; }
    }
}
