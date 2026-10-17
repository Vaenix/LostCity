using System;
using System.Collections.Generic;
using System.Linq;
using LostCity.CombatSandbox;
using LostCity.Meta.Core;
using LostCity.Meta.Persistence;
using UnityEngine;

namespace LostCity.Meta
{
    public sealed class CaseRegistry : MonoBehaviour
    {
        private SaveGameManager save;
        private Dictionary<string, CaseDefinition> cases = new Dictionary<string, CaseDefinition>();
        private Dictionary<string, ClueDefinition> clues = new Dictionary<string, ClueDefinition>();
        private Dictionary<string, EnemyDefinition> enemies = new Dictionary<string, EnemyDefinition>();
        private Dictionary<string, BossDefinition> bosses = new Dictionary<string, BossDefinition>();
        private ChapterDefinition[] chapters = new ChapterDefinition[0];
        private CaseDefinition[] orderedCases = new CaseDefinition[0];
        private ClueDefinition[] orderedClues = new ClueDefinition[0];
        private EnemyDefinition[] orderedEnemies = new EnemyDefinition[0];
        private BossDefinition[] orderedBosses = new BossDefinition[0];
        private readonly Dictionary<CaseDefinition, ChapterDefinition> membership = new Dictionary<CaseDefinition, ChapterDefinition>();
        private readonly HashSet<CaseDefinition> invalidMembership = new HashSet<CaseDefinition>();
        private readonly Dictionary<ChapterDefinition, CaseProgressionInput> progression = new Dictionary<ChapterDefinition, CaseProgressionInput>();

        public IReadOnlyList<ChapterDefinition> Chapters => chapters.ToArray();
        public IReadOnlyList<CaseDefinition> Cases => orderedCases.ToArray();
        public IReadOnlyList<ClueDefinition> Clues => orderedClues.ToArray();
        public IReadOnlyList<EnemyDefinition> Enemies => orderedEnemies.ToArray();
        public IReadOnlyList<BossDefinition> Bosses => orderedBosses.ToArray();
        public IReadOnlyList<CaseRegistryEntry> Entries => GetEntries();

        public void Initialize(GameContentCatalog catalog, SaveGameManager saveGame)
        {
            save = saveGame;
            membership.Clear();
            invalidMembership.Clear();
            progression.Clear();
            if (catalog == null) Debug.LogError("GameContentCatalog is missing; initializing an empty case registry.", this);
            var sourceCases = catalog == null ? new CaseDefinition[0] : catalog.Cases;
            var sourceChapters = catalog == null ? new ChapterDefinition[0] : catalog.Chapters;
            cases = ContentDefinitionIndex.Build(sourceCases, value => value.CaseId);
            clues = ContentDefinitionIndex.Build(catalog == null ? new ClueDefinition[0] : catalog.Clues, value => value.Id);
            enemies = ContentDefinitionIndex.Build(catalog == null ? new EnemyDefinition[0] : catalog.Enemies, value => value.EnemyId);
            bosses = ContentDefinitionIndex.Build(catalog == null ? new BossDefinition[0] : catalog.Bosses, value => value.BossId);
            orderedClues = catalog == null ? new ClueDefinition[0] : catalog.Clues.Where(value => value != null && GetClue(value.Id) == value).ToArray();
            orderedEnemies = catalog == null ? new EnemyDefinition[0] : catalog.Enemies.Where(value => value != null && GetEnemy(value.EnemyId) == value).ToArray();
            orderedBosses = catalog == null ? new BossDefinition[0] : catalog.Bosses.Where(value => value != null && GetBoss(value.BossId) == value).ToArray();
            var chapterIndex = ContentDefinitionIndex.Build(sourceChapters, value => value.ChapterId);
            chapters = sourceChapters.Where(chapter => chapter != null
                && chapterIndex.TryGetValue(chapter.ChapterId ?? string.Empty, out var valid) && valid == chapter).ToArray();

            var owners = new Dictionary<CaseDefinition, List<ChapterDefinition>>();
            foreach (var chapter in sourceChapters)
            {
                if (chapter == null) continue;
                var chapterCases = chapter.Cases;
                var seen = new HashSet<string>(StringComparer.Ordinal);
                bool validChapter = chapterIndex.TryGetValue(chapter.ChapterId ?? string.Empty, out var indexed) && indexed == chapter;
                foreach (var definition in chapterCases)
                {
                    if (!IsRegistered(definition) || !seen.Add(definition.CaseId)) validChapter = false;
                    if (definition == null) continue;
                    if (!owners.TryGetValue(definition, out var list)) owners.Add(definition, list = new List<ChapterDefinition>());
                    list.Add(chapter);
                }
                if (!validChapter)
                {
                    Debug.LogError($"Chapter '{chapter.name}' has invalid or ambiguous case ordering; referenced cases cannot start.", chapter);
                    foreach (var definition in chapterCases) if (definition != null) invalidMembership.Add(definition);
                    continue;
                }
                progression[chapter] = new CaseProgressionInput(chapterCases.Select(value => value.CaseId).ToArray(),
                    chapter.RequiredCompletedCaseIds);
            }

            foreach (var owner in owners)
            {
                if (owner.Value.Count != 1)
                {
                    invalidMembership.Add(owner.Key);
                    Debug.LogError($"Case '{owner.Key.CaseId}' has multiple chapter references; case cannot start.", owner.Key);
                }
                else if (IsRegistered(owner.Key)) membership[owner.Key] = owner.Value[0];
            }
            var ordered = new List<CaseDefinition>();
            foreach (var chapter in chapters)
                foreach (var definition in chapter.Cases)
                    if (IsRegistered(definition) && !ordered.Contains(definition)) ordered.Add(definition);
            foreach (var definition in sourceCases)
                if (IsRegistered(definition) && !ordered.Contains(definition)) ordered.Add(definition);
            orderedCases = ordered.ToArray();
        }

        public CaseDefinition GetCase(string caseId) => Lookup(cases, caseId);
        public ClueDefinition GetClue(string clueId) => Lookup(clues, clueId);
        public EnemyDefinition GetEnemy(string enemyId) => Lookup(enemies, enemyId);
        public BossDefinition GetBoss(string bossId) => Lookup(bosses, bossId);
        public ChapterDefinition GetChapterForCase(CaseDefinition definition) => definition != null
            && membership.TryGetValue(definition, out var chapter) ? chapter : null;

        public CaseState GetCaseState(CaseDefinition definition)
        {
            if (!IsRegistered(definition)) return CaseState.Locked;
            var completed = Completed();
            if (completed.Contains(definition.CaseId)) return CaseState.Completed;
            if (invalidMembership.Contains(definition)) return CaseState.Locked;
            var chapter = GetChapterForCase(definition);
            if (chapter == null) return CaseState.Available;
            return progression.TryGetValue(chapter, out var input)
                ? CaseProgressionRules.GetCaseState(definition.CaseId, input, completed) : CaseState.Locked;
        }

        public ChapterDefinition GetCurrentChapter()
        {
            var valid = chapters.Where(chapter => progression.ContainsKey(chapter)
                && !chapter.Cases.Any(invalidMembership.Contains)).ToArray();
            int index = CaseProgressionRules.GetCurrentChapterIndex(valid.Select(chapter => progression[chapter]).ToArray(), Completed());
            return index < 0 ? null : valid[index];
        }

        public bool CanStart(CaseDefinition definition, out string reason)
        {
            if (!IsRegistered(definition) || invalidMembership.Contains(definition))
            {
                reason = "案件配置不完整";
                return false;
            }
            bool result = CaseStartRules.CanStart(GetCaseState(definition), definition.IsPlayable,
                HasRequiredContent(definition), out var blocked);
            switch (blocked)
            {
                case CaseStartBlockReason.Locked: reason = "案件尚未解锁"; break;
                case CaseStartBlockReason.NotPlayable: reason = "案件仍在开发中"; break;
                case CaseStartBlockReason.MissingContent: reason = "案件配置不完整"; break;
                default: reason = string.Empty; break;
            }
            return result;
        }

        public IReadOnlyList<CaseRegistryEntry> GetEntries()
        {
            return orderedCases.Select(definition =>
            {
                bool canStart = CanStart(definition, out string reason);
                return new CaseRegistryEntry(definition, GetChapterForCase(definition), GetCaseState(definition), canStart, reason);
            }).ToArray();
        }

        private bool HasRequiredContent(CaseDefinition definition)
        {
            var boss = definition.BossDefinition;
            if (boss == null || GetBoss(boss.BossId) != boss || boss.BossPrefab == null
                || boss.BossPrefab.GetComponent<Damageable>() == null
                || string.IsNullOrWhiteSpace(definition.DeductionQuestion)
                || string.IsNullOrWhiteSpace(definition.CorrectAnswer)) return false;
            var caseClues = definition.Clues;
            if (caseClues == null || caseClues.Length == 0) return false;
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var clue in caseClues)
                if (clue == null || string.IsNullOrWhiteSpace(clue.Id)
                    || GetClue(clue.Id) != clue || !ids.Add(clue.Id)) return false;
            return definition.GetCompletionRewards().Length > 0;
        }

        private bool IsRegistered(CaseDefinition definition) => definition != null
            && GetCase(definition.CaseId) == definition;
        private HashSet<string> Completed() => save == null ? new HashSet<string>(StringComparer.Ordinal)
            : new HashSet<string>(save.CompletedCaseIds, StringComparer.Ordinal);
        private static T Lookup<T>(Dictionary<string, T> index, string id) where T : UnityEngine.Object =>
            id != null && index.TryGetValue(id, out var definition) ? definition : null;
    }
}
