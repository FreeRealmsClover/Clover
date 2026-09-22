using System.Collections.Generic;

namespace Sanctuary.Game.Resources.Definitions;

public sealed class QuestDefinition
{
    public int QuestId { get; set; }

    public int TitleId { get; set; }
    public int DescriptionId { get; set; }
    public int GiverDialogueId { get; set; }

    public int ObjectiveDescriptionId { get; set; }

    public int IconId { get; set; }

    public List<QuestGoal> Goals { get; set; } = [];

    /// <summary>The npc that offers the quest.</summary>
    public ulong GiverGuid { get; set; }

    /// <summary>The npc a goal falls back to when it names no target of its own.</summary>
    public ulong TargetGuid { get; set; }

    public int RewardCoins { get; set; }
    public int RewardExperience { get; set; }
    public List<int> RewardItems { get; set; } = [];
    public int RewardCollectionId { get; set; }

    public int PrerequisiteQuestId { get; set; }
    public int NextQuestId { get; set; }
    public List<int> ExcludesQuestIds { get; set; } = [];

    public int NotificationAvailable { get; set; } = 2;
    public int NotificationActive { get; set; } = 6;

    /// <summary>The speech shown when the quest is handed in, taken from the last goal.</summary>
    public int TurnInDialogueId => Goals.Count > 0 ? Goals[^1].DialogueId : 0;

    /// <summary>
    /// Whether this quest can be offered, given the quests a player has already taken,
    /// keyed by quest id with <see langword="true"/> for completed.
    /// </summary>
    public bool IsOfferableFor(IReadOnlyDictionary<int, bool> playerQuests)
    {
        if (playerQuests.ContainsKey(QuestId))
            return false;

        // Exclusions are checked before the prerequisite, not after it. A quest can have both,
        // and picking one branch of a choice has to rule out the other whatever unlocked it.
        foreach (var excludedQuestId in ExcludesQuestIds)
        {
            if (playerQuests.ContainsKey(excludedQuestId))
                return false;
        }

        if (PrerequisiteQuestId != 0)
            return playerQuests.TryGetValue(PrerequisiteQuestId, out var prerequisiteCompleted) && prerequisiteCompleted;

        return true;
    }
}
