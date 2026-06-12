using System.Collections.Generic;
using NUnit.Framework;

namespace LostCity.Meta.Core.Tests
{
    public sealed class CaseProgressionRulesTests
    {
        [Test]
        public void FirstCaseInUnlockedChapter_IsAvailable()
        {
            CaseProgressionInput input = Chapter("room_304", "placeholder");

            CaseState state = CaseProgressionRules.GetCaseState(
                "room_304",
                input,
                new HashSet<string>());

            Assert.That(state, Is.EqualTo(CaseState.Available));
        }

        [Test]
        public void LaterCase_IsLockedUntilPreviousCaseCompletes()
        {
            CaseProgressionInput input = Chapter("room_304", "placeholder");

            Assert.That(
                CaseProgressionRules.GetCaseState(
                    "placeholder",
                    input,
                    new HashSet<string>()),
                Is.EqualTo(CaseState.Locked));

            Assert.That(
                CaseProgressionRules.GetCaseState(
                    "placeholder",
                    input,
                    new HashSet<string> { "room_304" }),
                Is.EqualTo(CaseState.Available));
        }

        [Test]
        public void CompletedCase_RemainsCompleted()
        {
            CaseProgressionInput input = Chapter("room_304", "placeholder");

            CaseState state = CaseProgressionRules.GetCaseState(
                "room_304",
                input,
                new HashSet<string> { "room_304" });

            Assert.That(state, Is.EqualTo(CaseState.Completed));
        }

        [Test]
        public void ChapterPrerequisites_LockThenUnlockEveryCase()
        {
            CaseProgressionInput input = new CaseProgressionInput(
                new[] { "case_a" },
                new[] { "required_case" });

            Assert.That(
                CaseProgressionRules.GetCaseState(
                    "case_a",
                    input,
                    new HashSet<string>()),
                Is.EqualTo(CaseState.Locked));

            Assert.That(
                CaseProgressionRules.GetCaseState(
                    "case_a",
                    input,
                    new HashSet<string> { "required_case" }),
                Is.EqualTo(CaseState.Available));
        }

        [Test]
        public void CurrentChapter_IsFirstUnlockedChapterWithIncompleteCases()
        {
            CaseProgressionInput[] chapters =
            {
                new CaseProgressionInput(new[] { "room_304" }, new string[0]),
                new CaseProgressionInput(new[] { "future_case" }, new[] { "room_304" })
            };

            Assert.That(
                CaseProgressionRules.GetCurrentChapterIndex(
                    chapters,
                    new HashSet<string>()),
                Is.EqualTo(0));

            Assert.That(
                CaseProgressionRules.GetCurrentChapterIndex(
                    chapters,
                    new HashSet<string> { "room_304" }),
                Is.EqualTo(1));
        }

        [Test]
        public void AllCompletedChapters_ReturnLatestUnlockedChapter()
        {
            CaseProgressionInput[] chapters =
            {
                Chapter("case_a"),
                new CaseProgressionInput(new[] { "case_b" }, new[] { "case_a" })
            };

            int index = CaseProgressionRules.GetCurrentChapterIndex(
                chapters,
                new HashSet<string> { "case_a", "case_b" });

            Assert.That(index, Is.EqualTo(1));
        }

        [Test]
        public void NullOrEmptyChapter_LocksCase()
        {
            Assert.That(
                CaseProgressionRules.GetCaseState(
                    "case_a",
                    null,
                    new HashSet<string>()),
                Is.EqualTo(CaseState.Locked));

            Assert.That(
                CaseProgressionRules.GetCaseState(
                    "case_a",
                    Chapter(),
                    new HashSet<string>()),
                Is.EqualTo(CaseState.Locked));
        }

        [Test]
        public void CurrentChapter_NullOrEmptyListHasNoSelection()
        {
            Assert.That(
                CaseProgressionRules.GetCurrentChapterIndex(null, new HashSet<string>()),
                Is.EqualTo(-1));

            Assert.That(
                CaseProgressionRules.GetCurrentChapterIndex(
                    new CaseProgressionInput[0],
                    new HashSet<string>()),
                Is.EqualTo(-1));
        }

        [Test]
        public void CurrentChapter_SkipsNullAndEmptyChapters()
        {
            CaseProgressionInput[] chapters =
            {
                null,
                Chapter(),
                Chapter("case_a")
            };

            int index = CaseProgressionRules.GetCurrentChapterIndex(
                chapters,
                new HashSet<string>());

            Assert.That(index, Is.EqualTo(2));
        }

        [Test]
        public void CaseOutsideChapter_IsLockedEvenWhenCompletionSetContainsIt()
        {
            CaseState state = CaseProgressionRules.GetCaseState(
                "legacy_case",
                Chapter("room_304"),
                new HashSet<string> { "legacy_case" });

            Assert.That(state, Is.EqualTo(CaseState.Locked));
        }

        [Test]
        public void MetadataOnlyAvailableCase_CannotStart()
        {
            bool canStart = CaseStartRules.CanStart(
                CaseState.Available,
                isPlayable: false,
                hasRequiredContent: false,
                out CaseStartBlockReason reason);

            Assert.That(canStart, Is.False);
            Assert.That(reason, Is.EqualTo(CaseStartBlockReason.NotPlayable));
        }

        [Test]
        public void MissingRequiredContent_BlocksPlayableCase()
        {
            bool canStart = CaseStartRules.CanStart(
                CaseState.Available,
                isPlayable: true,
                hasRequiredContent: false,
                out CaseStartBlockReason reason);

            Assert.That(canStart, Is.False);
            Assert.That(reason, Is.EqualTo(CaseStartBlockReason.MissingContent));
        }

        [Test]
        public void LockedState_TakesPriorityOverOtherStartFailures()
        {
            bool canStart = CaseStartRules.CanStart(
                CaseState.Locked,
                isPlayable: false,
                hasRequiredContent: false,
                out CaseStartBlockReason reason);

            Assert.That(canStart, Is.False);
            Assert.That(reason, Is.EqualTo(CaseStartBlockReason.Locked));
        }

        [Test]
        public void CompletedPlayableCase_WithRequiredContentCanReplay()
        {
            bool canStart = CaseStartRules.CanStart(
                CaseState.Completed,
                isPlayable: true,
                hasRequiredContent: true,
                out CaseStartBlockReason reason);

            Assert.That(canStart, Is.True);
            Assert.That(reason, Is.EqualTo(CaseStartBlockReason.None));
        }

        private static CaseProgressionInput Chapter(params string[] caseIds)
        {
            return new CaseProgressionInput(caseIds, new string[0]);
        }
    }
}
