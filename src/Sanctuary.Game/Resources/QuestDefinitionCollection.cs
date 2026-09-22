using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using Sanctuary.Core.Collections;
using Sanctuary.Game.Resources.Definitions;

namespace Sanctuary.Game.Resources;

public sealed class QuestDefinitionCollection : ObservableConcurrentDictionary<int, QuestDefinition>
{
    private static readonly IReadOnlyList<int> NoQuests = [];

    private readonly ILogger _logger;

    private Dictionary<ulong, IReadOnlyList<int>> _byGiver = [];
    private Dictionary<ulong, IReadOnlyList<int>> _byTarget = [];

    public QuestDefinitionCollection(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>The quests this npc can offer.</summary>
    public IReadOnlyList<int> QuestsOfferedBy(ulong npcGuid) => _byGiver.TryGetValue(npcGuid, out var questIds) ? questIds : NoQuests;

    /// <summary>The quests this npc can advance or complete.</summary>
    public IReadOnlyList<int> QuestsTargeting(ulong npcGuid) => _byTarget.TryGetValue(npcGuid, out var questIds) ? questIds : NoQuests;

    public bool IsQuestNpc(ulong npcGuid) => _byGiver.ContainsKey(npcGuid) || _byTarget.ContainsKey(npcGuid);

    public bool Load(string filePath)
    {
        if (!File.Exists(filePath))
        {
            // Quest content is optional. Without it the server runs normally and offers no quests.
            _logger.LogWarning("Failed to find file \"{file}\". No quests will be loaded.", filePath);
            return true;
        }

        try
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

            var entries = JsonSerializer.Deserialize<List<QuestDefinition>>(stream, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });

            if (entries is null)
            {
                _logger.LogError("No quests found in file \"{file}\".", filePath);
                return false;
            }

            var loaded = new Dictionary<int, QuestDefinition>();

            foreach (var entry in entries)
            {
                if (!IsValid(entry, filePath))
                    return false;

                if (!loaded.TryAdd(entry.QuestId, entry))
                {
                    _logger.LogError("Duplicate quest {id} in \"{file}\".", entry.QuestId, filePath);
                    return false;
                }
            }

            foreach (var entry in loaded.Values)
            {
                if (!ReferencedQuestsExist(entry, loaded, filePath))
                    return false;
            }

            Index(loaded.Values);

            Clear();

            foreach (var entry in loaded)
                TryAdd(entry.Key, entry.Value);

            _logger.LogInformation("Loaded {count} quests from \"{file}\".", Count, filePath);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse file \"{file}\".", filePath);
            return false;
        }
    }

    /// <summary>
    /// Rejects quest data the server cannot act on. A quest whose goal names no reachable target,
    /// or repeats an objective id, fails silently at runtime, so it is refused here instead.
    /// </summary>
    private bool IsValid(QuestDefinition quest, string filePath)
    {
        if (quest.QuestId <= 0)
        {
            _logger.LogError("Quest has no id in \"{file}\".", filePath);
            return false;
        }

        if (quest.GiverGuid == 0)
        {
            _logger.LogError("Quest {id} has no giver, so it can never be offered. \"{file}\"", quest.QuestId, filePath);
            return false;
        }

        if (quest.Goals.Count == 0)
        {
            _logger.LogError("Quest {id} has no goals. \"{file}\"", quest.QuestId, filePath);
            return false;
        }

        var goalNameIds = new HashSet<int>();

        foreach (var goal in quest.Goals)
        {
            if (goal.NameId <= 0)
            {
                _logger.LogError("Quest {id} has a goal without a NameId, which the client tracks objectives by. \"{file}\"", quest.QuestId, filePath);
                return false;
            }

            if (!goalNameIds.Add(goal.NameId))
            {
                _logger.LogError("Quest {id} reuses goal NameId {nameId}; objectives would overwrite each other. \"{file}\"", quest.QuestId, goal.NameId, filePath);
                return false;
            }

            if (!IsGoalValid(quest, goal, filePath))
                return false;
        }

        return true;
    }

    private bool IsGoalValid(QuestDefinition quest, QuestGoal goal, string filePath)
    {
        switch (goal.Type)
        {
            case QuestGoalType.TalkToNpc:
                if (goal.TargetGuid == 0 && goal.TargetGuids.Count == 0 && quest.TargetGuid == 0)
                {
                    _logger.LogError("Quest {id} has a talk goal ({nameId}) with no npc to talk to. \"{file}\"", quest.QuestId, goal.NameId, filePath);
                    return false;
                }

                return true;

            case QuestGoalType.ReachLocation:
                if (goal.ReachPosition.Length < 3)
                {
                    _logger.LogError("Quest {id} has a travel goal ({nameId}) without an x, y and z position. \"{file}\"", quest.QuestId, goal.NameId, filePath);
                    return false;
                }

                return true;

            case QuestGoalType.Collect:
                if (string.IsNullOrWhiteSpace(goal.CollectNodeType))
                {
                    _logger.LogError("Quest {id} has a gather goal ({nameId}) with no CollectNodeType, so it can never be credited. \"{file}\"", quest.QuestId, goal.NameId, filePath);
                    return false;
                }

                if (goal.RequiredCount <= 0)
                {
                    _logger.LogError("Quest {id} has a gather goal ({nameId}) with no RequiredCount, so it can never be completed. \"{file}\"", quest.QuestId, goal.NameId, filePath);
                    return false;
                }

                return true;

            default:
                _logger.LogError("Quest {id} has a goal ({nameId}) of unknown type {type}. \"{file}\"", quest.QuestId, goal.NameId, goal.Type, filePath);
                return false;
        }
    }

    private bool ReferencedQuestsExist(QuestDefinition quest, Dictionary<int, QuestDefinition> loaded, string filePath)
    {
        if (quest.PrerequisiteQuestId != 0 && !loaded.ContainsKey(quest.PrerequisiteQuestId))
        {
            _logger.LogError("Quest {id} requires unknown quest {other}, so it could never be offered. \"{file}\"", quest.QuestId, quest.PrerequisiteQuestId, filePath);
            return false;
        }

        if (quest.NextQuestId != 0 && !loaded.ContainsKey(quest.NextQuestId))
        {
            _logger.LogError("Quest {id} leads to unknown quest {other}. \"{file}\"", quest.QuestId, quest.NextQuestId, filePath);
            return false;
        }

        foreach (var excludedQuestId in quest.ExcludesQuestIds)
        {
            if (!loaded.ContainsKey(excludedQuestId))
            {
                _logger.LogError("Quest {id} excludes unknown quest {other}. \"{file}\"", quest.QuestId, excludedQuestId, filePath);
                return false;
            }
        }

        return true;
    }

    private void Index(IEnumerable<QuestDefinition> quests)
    {
        var byGiver = new Dictionary<ulong, List<int>>();
        var byTarget = new Dictionary<ulong, List<int>>();

        static void Add(Dictionary<ulong, List<int>> index, ulong npcGuid, int questId)
        {
            if (npcGuid == 0)
                return;

            if (!index.TryGetValue(npcGuid, out var questIds))
                index[npcGuid] = questIds = [];

            if (!questIds.Contains(questId))
                questIds.Add(questId);
        }

        foreach (var quest in quests)
        {
            Add(byGiver, quest.GiverGuid, quest.QuestId);
            Add(byTarget, quest.TargetGuid, quest.QuestId);

            foreach (var goal in quest.Goals)
            {
                Add(byTarget, goal.TargetGuid, quest.QuestId);

                foreach (var targetGuid in goal.AllTalkTargetGuids())
                    Add(byTarget, targetGuid, quest.QuestId);
            }
        }

        _byGiver = new Dictionary<ulong, IReadOnlyList<int>>(byGiver.Count);
        _byTarget = new Dictionary<ulong, IReadOnlyList<int>>(byTarget.Count);

        foreach (var entry in byGiver)
            _byGiver[entry.Key] = entry.Value;

        foreach (var entry in byTarget)
            _byTarget[entry.Key] = entry.Value;
    }
}
