namespace LostCity.Meta.Core
{
    public static class CaseStartRules
    {
        public static bool CanStart(
            CaseState state,
            bool isPlayable,
            bool hasRequiredContent,
            out CaseStartBlockReason reason)
        {
            if (state == CaseState.Locked)
            {
                reason = CaseStartBlockReason.Locked;
                return false;
            }

            if (!isPlayable)
            {
                reason = CaseStartBlockReason.NotPlayable;
                return false;
            }

            if (!hasRequiredContent)
            {
                reason = CaseStartBlockReason.MissingContent;
                return false;
            }

            reason = CaseStartBlockReason.None;
            return true;
        }
    }
}
