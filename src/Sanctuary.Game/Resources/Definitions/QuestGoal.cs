using System.Collections.Generic;

namespace Sanctuary.Game.Resources.Definitions;

public enum QuestGoalType
{
    TalkToNpc = 0,
    ReachLocation = 1,
    Collect = 2
}

public sealed class QuestDialogueLine
{
    public int TextId { get; set; }

    public int ResponseTextId { get; set; }
}

public sealed class QuestGoal
{
    /// <summary>Identifies the objective to the client, and must be unique within its quest.</summary>
    public int NameId { get; set; }

    public int DescriptionId { get; set; }

    /// <summary>Spoken when the goal is completed. On the last goal this is the hand-in speech.</summary>
    public int DialogueId { get; set; }

    /// <summary>A multi-line conversation, used in place of <see cref="DialogueId"/> when set.</summary>
    public List<QuestDialogueLine> Dialogue { get; set; } = [];

    public QuestGoalType Type { get; set; } = QuestGoalType.TalkToNpc;

    public ulong TargetGuid { get; set; }

    /// <summary>Additional npcs, for a goal that needs several of them spoken to.</summary>
    public List<ulong> TargetGuids { get; set; } = [];

    public List<int> TargetDialogueIds { get; set; } = [];
    public List<int> TargetResponseIds { get; set; } = [];

    public int RequiredCount { get; set; }

    public string CollectNodeType { get; set; } = string.Empty;

    public float[] ReachPosition { get; set; } = [];

    public float ReachRadius { get; set; }

    public byte CursorId { get; set; } = 17;

    public int InteractRange { get; set; } = 12;

    /// <summary>A talk goal that needs more than one npc spoken to.</summary>
    public bool IsCountedTalk => Type == QuestGoalType.TalkToNpc && RequiredCount > 1;

    /// <summary>Every npc this goal accepts, starting with <see cref="TargetGuid"/>.</summary>
    public IEnumerable<ulong> AllTalkTargetGuids()
    {
        if (TargetGuid != 0)
            yield return TargetGuid;

        foreach (var targetGuid in TargetGuids)
        {
            if (targetGuid != 0 && targetGuid != TargetGuid)
                yield return targetGuid;
        }
    }

    /// <summary>
    /// The lines this npc says for this goal: the shared conversation if one is set, otherwise the
    /// line paired with this npc's position in the target list, otherwise the goal's own dialogue.
    /// </summary>
    public IReadOnlyList<QuestDialogueLine> ConversationFor(ulong npcGuid)
    {
        if (Dialogue.Count > 0)
            return Dialogue;

        var index = 0;

        foreach (var targetGuid in AllTalkTargetGuids())
        {
            if (targetGuid == npcGuid && index < TargetDialogueIds.Count && TargetDialogueIds[index] != 0)
            {
                return
                [
                    new QuestDialogueLine
                    {
                        TextId = TargetDialogueIds[index],
                        ResponseTextId = index < TargetResponseIds.Count ? TargetResponseIds[index] : 0
                    }
                ];
            }

            index++;
        }

        return DialogueId != 0 ? [new QuestDialogueLine { TextId = DialogueId }] : [];
    }
}
