using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Sanctuary.Core.Extensions;
using Sanctuary.Core.IO;
using Sanctuary.Database;
using Sanctuary.Database.Entities;
using Sanctuary.Game.Entities;
using Sanctuary.Game.Interactions;
using Sanctuary.Game.Resources.Definitions;
using Sanctuary.Game.Zones;
using Sanctuary.Packet;
using Sanctuary.Packet.Common;

namespace Sanctuary.Game.Quests;

public sealed class QuestManager : IQuestManager
{
    private readonly IResourceManager _resourceManager;
    private readonly IDbContextFactory<DatabaseContext> _dbContextFactory;
    private readonly ILogger<QuestManager> _logger;

    public QuestManager(IResourceManager resourceManager, IDbContextFactory<DatabaseContext> dbContextFactory, ILogger<QuestManager> logger)
    {
        _resourceManager = resourceManager;
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    public bool IsQuestNpc(ulong npcGuid)
        => _resourceManager.Quests.IsQuestNpc(npcGuid);

    private IEnumerable<(int QuestId, QuestDefinition Quest, int GoalIndex, QuestGoal Goal)> ActiveGoals(Player player)
    {
        foreach (var (questId, completed) in player.Quests)
        {
            if (completed || !_resourceManager.Quests.TryGet(questId, out var quest))
                continue;

            var goals = quest.Goals;
            int done = player.QuestGoalProgress.TryGetValue(questId, out var progress) ? progress : 0;
            if (done >= goals.Count)
                continue;

            yield return (questId, quest, done, goals[done]);
        }
    }

    public List<NpcInteractionOption> GetInteractionOptions(Player player, Npc npc)
    {
        var options = new List<NpcInteractionOption>();
        var quests = _resourceManager.Quests;

        // CloverQuestRecovery: every objective is done but the turn-in was never answered
        // (restart, relog, disconnect). Let the player hand it in again at the final npc.
        foreach (var (questId, completed) in player.Quests)
        {
            if (completed || !quests.TryGet(questId, out var readyQuest) || readyQuest.Goals.Count == 0)
                continue;

            var progress = player.QuestGoalProgress.TryGetValue(questId, out var p) ? p : 0;

            if (progress < readyQuest.Goals.Count)
                continue;

            if (GoalTargetGuid(readyQuest, readyQuest.Goals.Count - 1) != npc.Guid && readyQuest.TargetGuid != npc.Guid)
                continue;

            var quest = readyQuest;

            options.Add(new NpcInteractionOption
            {
                IconId = ContextIcons.QuestTurnIn,
                ButtonTextId = quest.TitleId,
                Invoke = interactingPlayer =>
                {
                    // Any queued turn-in answer is stale by now; start clean so the next
                    // answer completes this quest.
                    interactingPlayer.PendingQuestEndActions.Clear();
                    TurnIn(interactingPlayer, quest);
                }
            });
        }

        foreach (var (questId, completed) in player.Quests)
        {
            if (completed || !quests.TryGet(questId, out var activeQuest))
                continue;

            if (!AdvancesHere(player, activeQuest, npc, out _))
                continue;

            var quest = activeQuest;

            options.Add(new NpcInteractionOption
            {
                IconId = ContextIcons.QuestTurnIn,
                ButtonTextId = quest.TitleId,
                Invoke = interactingPlayer => AdvanceAtNpc(interactingPlayer, quest, npc)
            });
        }

        {
            foreach (var questId in quests.QuestsOfferedBy(npc.Guid))
            {
                if (!quests.TryGet(questId, out var offerableQuest) || !offerableQuest.IsOfferableFor(player.Quests))
                    continue;

                var quest = offerableQuest;

                options.Add(new NpcInteractionOption
                {
                    IconId = ContextIcons.QuestOffer,
                    ButtonTextId = quest.TitleId,
                    Invoke = interactingPlayer => Offer(interactingPlayer, quest)
                });
            }
        }

        return options;
    }

    private bool AdvancesHere(Player player, QuestDefinition quest, Npc npc, out int goalIndex)
    {
        goalIndex = player.QuestGoalProgress.TryGetValue(quest.QuestId, out var progress) ? progress : 0;

        var goals = quest.Goals;

        if (goalIndex >= goals.Count)
            return false;

        if (goals[goalIndex].Type == QuestGoalType.Collect)
            return false;

        if (goals[goalIndex].IsCountedTalk)
            return goals[goalIndex].AllTalkTargetGuids().Contains(npc.Guid);

        return GoalTargetGuid(quest, goalIndex) == npc.Guid;
    }

    private void AdvanceAtNpc(Player player, QuestDefinition quest, Npc npc)
    {
        if (!AdvancesHere(player, quest, npc, out var goalIndex))
            return;

        if (quest.Goals[goalIndex].IsCountedTalk)
            TryCreditCountedTalk(player, quest, goalIndex, npc);
        else
            CompleteGoal(player, quest, goalIndex, npc.Guid);
    }

    public void OnNpcInteract(Player player, Npc npc)
    {
        var options = GetInteractionOptions(player, npc);

        // The first option wins: the menu that would let the player choose between several is
        // not ported yet. Options are built turn-ins first, which is what a player walking up
        // to an npc expects.
        if (options.Count > 0)
        {
            options[0].Invoke(player);
            return;
        }

        // CloverQuestRecovery: nothing quest related to do with this npc right now. Run its
        // normal interaction, or close the client side interaction so it can be clicked again.
        if (npc.InteractAction is not null)
            npc.OnInteract(player);
        else
            player.SendTunneled(new CommandPacketEndDialog());
    }

    private const int CollectPickupEffect = 5386;

    public void OnCollectionNodeGathered(Player player, CollectionNode node)
    {
        var nodeType = node.TypeDefinition.Key;

        foreach (var (questId, quest, goalIndex, goal) in ActiveGoals(player))
        {
            if (goal.Type != QuestGoalType.Collect || goal.CollectNodeType != nodeType)
                continue;

            var required = goal.RequiredCount;
            if (required <= 0)
                return;

            var count = (player.QuestCollectProgress.TryGetValue(questId, out var c) ? c : 0) + 1;

            if (count >= required)
            {
                player.QuestCollectProgress.Remove(questId);
                CompleteGoal(player, quest, goalIndex);
                return;
            }

            player.QuestCollectProgress[questId] = count;

            player.SendTunneled(new QuestObjectiveUpdatePacket
            {
                QuestId = questId,
                ObjectiveId = goal.NameId,
                CurrentCount = count,
                CompletedPercentage = (float)count / required
            });

            PersistCollectCount(player, questId, count);
            RefreshObjectiveTarget(player);
            return;
        }
    }
    private const float DefaultReachRadius = 12f;

    public void OnPlayerMoved(Player player)
    {
        foreach (var (questId, quest, done, goal) in ActiveGoals(player))
        {
            if (goal.Type != QuestGoalType.ReachLocation || goal.ReachPosition.Length < 3)
                continue;

            var radius = goal.ReachRadius > 0 ? goal.ReachRadius : DefaultReachRadius;
            var target = new Vector3(goal.ReachPosition[0], goal.ReachPosition[1], goal.ReachPosition[2]);

            if (!player.Position.IsInCircle(target, radius))
                continue;

            CompleteGoal(player, quest, done);
        }
    }

    private void UpdateCharacterQuest(Player player, int questId, Action<DbCharacterQuest> update)
    {
        using var db = _dbContextFactory.CreateDbContext();
        var dbQuest = db.CharacterQuests.FirstOrDefault(x => x.QuestId == questId && x.CharacterId == player.CharacterId);
        if (dbQuest is null)
            return;

        update(dbQuest);
        db.SaveChanges();
    }

    private void PersistCollectCount(Player player, int questId, int count)
        => UpdateCharacterQuest(player, questId, q => q.GoalCount = count);

    private void PersistActiveQuest(Player player, int questId)
    {
        using var db = _dbContextFactory.CreateDbContext();

        var dbCharacter = db.Characters.FirstOrDefault(c => c.Id == player.CharacterId);
        if (dbCharacter is null)
            return;

        dbCharacter.ActiveQuestId = questId != 0 ? questId : null;
        db.SaveChanges();
    }

    public void AcceptQuest(Player player, int questId)
    {
        if (!_resourceManager.Quests.TryGet(questId, out var quest) || !quest.IsOfferableFor(player.Quests))
            return;

        player.Quests[questId] = false;
        player.QuestGoalProgress.Remove(questId);
        player.QuestCollectProgress.Remove(questId);
        ClearTalkProgress(player, quest);
        player.ActiveQuestId = questId;
        player.LastQuestAcceptedAt = DateTime.UtcNow;

        global::Sanctuary.Game.Eye.Log("quest-accept", player, new { quest = questId });

        using (var db = _dbContextFactory.CreateDbContext())
        {
            db.CharacterQuests.Add(new DbCharacterQuest
            {
                QuestId = questId,
                CharacterId = player.CharacterId,
                Completed = false
            });

            var dbCharacter = db.Characters.FirstOrDefault(c => c.Id == player.CharacterId);
            if (dbCharacter is not null)
                dbCharacter.ActiveQuestId = questId;

            db.SaveChanges();
        }

        SendActiveState(player, quest);

        RefreshQuestNotifications(player, quest);

        player.SendTunneled(new CommandPacketQuestDialogComplete());
    }

    public void CompleteQuest(Player player, int questId)
    {
        if (!_resourceManager.Quests.TryGet(questId, out var quest))
            return;

        if (player.Quests.TryGetValue(questId, out var done) && done)
            return;

        player.Quests[questId] = true;
        player.QuestCollectProgress.Remove(questId);
        ClearTalkProgress(player, quest);
        UpdateCharacterQuest(player, questId, q => q.Completed = true);

        player.SendTunneled(new QuestCompletePacket { QuestId = questId });

        player.SendTunneled(new CompletedQuestCountUpdatePacket
        {
            Count = player.Quests.Values.Count(done => done)
        });

        SendJournalQuestStates(player);

        global::Sanctuary.Game.Eye.Log("quest-complete", player, new { quest = questId, rewardCoins = quest.RewardCoins, rewardItems = quest.RewardItems });

        GrantReward(player, quest);

        RefreshQuestNotifications(player, quest);

        if (quest.NextQuestId != 0 && _resourceManager.Quests.TryGet(quest.NextQuestId, out var next))
            RefreshQuestNotification(player, next.GiverGuid);

        RefreshObjectiveTarget(player);
    }

    public void AbandonQuest(Player player, int questId)
    {
        if ((DateTime.UtcNow - player.LastQuestAcceptedAt).TotalSeconds < 3)
            return;

        if (!(player.Quests.TryGetValue(questId, out var completed) && !completed))
        {
            var active = player.Quests.Where(entry => !entry.Value).Select(entry => entry.Key).ToList();
            if (active.Count != 1)
                return;

            questId = active[0];
        }

        if (!_resourceManager.Quests.TryGet(questId, out var quest))
            return;

        player.Quests.Remove(questId);
        player.QuestCollectProgress.Remove(questId);
        ClearTalkProgress(player, quest);
        QuestDialogue.Clear(player);

        using (var db = _dbContextFactory.CreateDbContext())
        {
            var dbQuest = db.CharacterQuests.FirstOrDefault(x => x.QuestId == questId && x.CharacterId == player.CharacterId);
            if (dbQuest is not null)
            {
                db.CharacterQuests.Remove(dbQuest);
                db.SaveChanges();
            }
        }

        global::Sanctuary.Game.Eye.Log("quest-abandon", player, new { quest = questId });

        player.SendTunneled(new QuestAbandonedPacket { QuestId = questId });

        RefreshQuestNotifications(player, quest);

        RefreshObjectiveTarget(player);
    }

    public void SetActiveQuest(Player player, int questId)
    {
        if (!_resourceManager.Quests.TryGet(questId, out var quest))
            return;

        if (player.Quests.TryGetValue(questId, out var completed) && !completed)
        {
            player.ActiveQuestId = questId;
            PersistActiveQuest(player, questId);

            int done = player.QuestGoalProgress.TryGetValue(questId, out var progress) ? progress : 0;
            var goals = quest.Goals;

            if (done < goals.Count)
                SendObjectiveActivated(player, questId, goals[done]);

            SendObjectiveForGoal(player, quest, done);
        }
    }

    public void RestoreJournal(Player player)
    {
        foreach (var (questId, completed) in player.Quests)
        {
            if (!completed && _resourceManager.Quests.TryGet(questId, out var quest))
                SendActiveState(player, quest, sendObjectiveTarget: false, suppressStartBanner: true);
        }

        RefreshObjectiveTarget(player);

        player.SendTunneled(new CompletedQuestCountUpdatePacket
        {
            Count = player.Quests.Values.Count(done => done)
        });

        SendJournalQuestStates(player);
    }

    private void SendJournalQuestStates(Player player)
    {
        var states = new Dictionary<int, int>();
        foreach (var (questId, completed) in player.Quests)
            if (completed)
                states[questId] = 1;

        if (states.Count > 0)
            player.SendTunneled(new AdventurersJournalQuestUpdatePacket { QuestStates = states });
    }

    private void RefreshQuestNotifications(Player player, QuestDefinition quest)
    {
        RefreshQuestNotification(player, quest.GiverGuid);
        RefreshQuestNotification(player, quest.TargetGuid);

        foreach (var excludedId in quest.ExcludesQuestIds)
        {
            if (!_resourceManager.Quests.TryGet(excludedId, out var excludedQuest))
                continue;

            RefreshQuestNotification(player, excludedQuest.GiverGuid);
            RefreshQuestNotification(player, excludedQuest.TargetGuid);
        }
    }

    public void RefreshQuestNotification(Player player, ulong npcGuid)
    {
        if (npcGuid == 0 || !player.Zone.TryGetNpc(npcGuid, out var npc))
            return;

        var imageId = player.GetNotificationImageId(npc);

        // The marker over an npc's head is part of the packet that adds the npc, so changing it
        // means removing the npc from this client and adding it again.
        player.SendTunneled(new PlayerUpdatePacketRemovePlayer { Guid = npc.Guid });

        var addNpcPacket = npc.GetAddNpcPacket();
        addNpcPacket.NotificationImageSetId = imageId;
        player.SendTunneled(addNpcPacket);

        if (npc.CursorId != 0)
        {
            var relevance = new PlayerUpdatePacketNpcRelevance();

            relevance.Entries.Add(new PlayerUpdatePacketNpcRelevance.Entry
            {
                Guid = npc.Guid,
                HasCursor = true,
                CursorId = npc.CursorId,
                Unknown2 = imageId != 0
            });

            player.SendTunneled(relevance);
        }

        if (imageId == 0)
        {
            player.SendTunneled(new PlayerUpdatePacketRemoveNotifications { Guids = { npc.Guid } });
            return;
        }

        player.SendTunneled(new PlayerUpdatePacketAddNotifications
        {
            Notifications =
            {
                new NotificationInfo
                {
                    Guid = npc.Guid,
                    Combat = false,
                    ImageId = imageId,
                    NameId = npc.NameId,
                    SubTextId = npc.SubTextNameId
                }
            }
        });
    }

    private void Offer(Player player, QuestDefinition quest)
    {
        var offer = new QuestInfoPacket
        {
            QuestId = quest.QuestId,
            TitleId = quest.GiverDialogueId,
            DescriptionId = quest.DescriptionId,
            HelperTextId = quest.TitleId,
            IconId = quest.IconId,
            Unknown6 = quest.ObjectiveDescriptionId,
            Unknown7 = false,
            NpcGuid = quest.GiverGuid,
            Unknown10 = 0,
            Unknown11 = false,
            Unknown12 = false
        };

        FillRewardBundle(offer.RewardBundle, quest);

        player.SendTunneled(offer);
    }

    private void FillRewardBundle(RewardBundleBase bundle, QuestDefinition quest)
    {
        bundle.Success = false;
        bundle.Unknown1 = quest.RewardCoins;
        bundle.RewardKind = quest.RewardExperience;
        bundle.Unknown3 = 0;
        bundle.Multiplier = 1f;
        bundle.IconId = -1;
        bundle.NameId = -1;

        foreach (var entry in BuildRewardEntries(quest))
            bundle.Entries.Add(entry);
    }

    private List<RewardBundleEntryBase> BuildRewardEntries(QuestDefinition quest)
    {
        var entries = new List<RewardBundleEntryBase>();
        foreach (var definitionId in quest.RewardItems)
        {
            if (_resourceManager.ClientItemDefinitions.TryGetValue(definitionId, out var itemDef))
            {
                entries.Add(new RewardBundleEntryItem
                {
                    IconId = itemDef.Icon.Id,
                    NameId = itemDef.NameId,
                    Quantity = 1
                });
            }
        }

        return entries;
    }

    private bool TryCreditCountedTalk(Player player, QuestDefinition quest, int goalIndex, Npc npc)
    {
        var goal = quest.Goals[goalIndex];

        if (!goal.AllTalkTargetGuids().Contains(npc.Guid))
            return false;

        bool alreadyCredited = !player.TalkedQuestNpcs.Add(npc.Guid);

        if (!alreadyCredited)
            RefreshQuestNotification(player, npc.Guid);

        int required = goal.RequiredCount;
        int count = player.QuestCollectProgress.TryGetValue(quest.QuestId, out var c) ? c : 0;

        if (!alreadyCredited)
            count++;

        if (!alreadyCredited && count >= required)
        {
            player.QuestCollectProgress.Remove(quest.QuestId);
            ClearTalkProgress(player, goal);
            CompleteGoal(player, quest, goalIndex, npc.Guid);
            return true;
        }

        if (!alreadyCredited)
        {
            player.QuestCollectProgress[quest.QuestId] = count;

            player.SendTunneled(new QuestObjectiveUpdatePacket
            {
                QuestId = quest.QuestId,
                ObjectiveId = goal.NameId,
                CurrentCount = count,
                CompletedPercentage = (float)count / required
            });

            PersistCollectCount(player, quest.QuestId, count);
        }

        QuestDialogue.Begin(player, goal.ConversationFor(npc.Guid), npc.Guid);

        RefreshObjectiveTarget(player);
        return true;
    }

    private static void ClearTalkProgress(Player player, QuestGoal goal)
    {
        foreach (var guid in goal.AllTalkTargetGuids())
            player.TalkedQuestNpcs.Remove(guid);
    }

    private static void ClearTalkProgress(Player player, QuestDefinition quest)
    {
        foreach (var goal in quest.Goals)
            if (goal.IsCountedTalk)
                ClearTalkProgress(player, goal);
    }

    private static ulong NearestUntalkedTarget(Player player, QuestGoal goal)
    {
        ulong nearest = 0;
        var best = float.MaxValue;

        foreach (var guid in goal.AllTalkTargetGuids())
        {
            if (player.TalkedQuestNpcs.Contains(guid))
                continue;

            if (!player.Zone.TryGetNpc(guid, out var npc))
                continue;

            var dx = npc.Position.X - player.Position.X;
            var dz = npc.Position.Z - player.Position.Z;
            var distance = dx * dx + dz * dz;

            if (distance < best)
            {
                best = distance;
                nearest = guid;
            }
        }

        return nearest;
    }
    private void CompleteGoal(Player player, QuestDefinition quest, int goalIndex, ulong spokenBy = 0)
    {
        var goals = quest.Goals;

        bool isFinalGoal = goalIndex + 1 >= goals.Count;

        player.SendTunneled(new QuestObjectiveCompletePacket
        {
            QuestId = quest.QuestId,
            ObjectiveId = goals[goalIndex].NameId,
            Percent = 1f,
            Silent = isFinalGoal
        });

        int done = goalIndex + 1;
        player.QuestGoalProgress[quest.QuestId] = done;

        UpdateCharacterQuest(player, quest.QuestId, q =>
        {
            q.GoalProgress = done;
            q.GoalCount = 0;
        });

        if (done >= goals.Count)
        {
            TurnIn(player, quest);
            return;
        }

        player.SendTunneled(new QuestObjectiveAddedPacket
        {
            QuestId = quest.QuestId,
            ObjectiveNameId = goals[done].NameId,
            ObjectiveDescriptionId = goals[done].NameId,
            ObjectiveField2 = goals[done].DescriptionId != 0 ? goals[done].DescriptionId : goals[done].NameId
        });
        SendObjectiveActivated(player, quest.QuestId, goals[done]);
        SendObjectiveForGoal(player, quest, done);

        var completedGoal = goals[goalIndex];
        if (completedGoal.Type == QuestGoalType.TalkToNpc)
        {
            var speaker = spokenBy != 0 ? spokenBy : GoalTargetGuid(quest, goalIndex);

            QuestDialogue.Begin(player, completedGoal.ConversationFor(speaker), speaker);
        }
    }

    private const int YouGotItTextId = 103085;

    private const int GreenCheckImageId = 300;

    private const int GreenButtonImageSet = 17;

    private void TurnIn(Player player, QuestDefinition quest)
    {
        var end = new QuestEndPacket
        {
            NpcGuid = GoalTargetGuid(quest, quest.Goals.Count - 1),
            QuestId = quest.QuestId,
            TitleId = quest.TurnInDialogueId,
            DescriptionId = quest.TitleId
        };

        FillRewardBundle(end.RewardBundle, quest);

        player.SendTunneled(end);

        player.PendingQuestEndActions.Enqueue(() => CompleteQuest(player, quest.QuestId));
    }

    private static void SendQuestAdd(Player player, QuestDefinition quest, int helperTextId, float completedPercentage = 0f, bool suppressStartBanner = false)
    {
        player.SendTunneled(new QuestAddPacket
        {
            QuestId = quest.QuestId,
            TitleId = quest.TitleId,
            DescriptionId = quest.ObjectiveDescriptionId,
            HelperTextId = helperTextId,
            MembersOnly = false,
            TimeStarted = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            ProfileId = 0,
            CompletedPercentage = completedPercentage,
            IconId = quest.IconId,
            SystemQuest = false,
            SuppressStartBanner = suppressStartBanner
        });
    }

    private void SendActiveState(Player player, QuestDefinition quest, bool sendObjectiveTarget = true, bool suppressStartBanner = false)
    {
        int alreadyDone = player.QuestGoalProgress.TryGetValue(quest.QuestId, out var p) ? p : 0;
        SendQuestAdd(player, quest, quest.ObjectiveDescriptionId, (float)alreadyDone / quest.Goals.Count, suppressStartBanner);

        var goals = quest.Goals;
        int done = player.QuestGoalProgress.TryGetValue(quest.QuestId, out var progress) ? progress : 0;
        int lastVisible = System.Math.Min(done, goals.Count - 1);

        for (int i = 0; i <= lastVisible; i++)
        {
            player.SendTunneled(new QuestObjectiveAddedPacket
            {
                QuestId = quest.QuestId,
                ObjectiveNameId = goals[i].NameId,
                ObjectiveDescriptionId = goals[i].NameId,
                ObjectiveField2 = goals[i].DescriptionId != 0 ? goals[i].DescriptionId : goals[i].NameId
            });
        }

        for (int i = 0; i < done && i < goals.Count; i++)
        {
            player.SendTunneled(new QuestObjectiveCompletePacket
            {
                QuestId = quest.QuestId,
                ObjectiveId = goals[i].NameId,
                Percent = 1f,
                Silent = true
            });
        }

        if (done < goals.Count)
        {
            var activeGoal = goals[done];
            SendObjectiveActivated(player, quest.QuestId, activeGoal);

            if (activeGoal.Type == QuestGoalType.Collect
                && player.QuestCollectProgress.TryGetValue(quest.QuestId, out var collected) && collected > 0)
            {
                int req = activeGoal.RequiredCount;
                player.SendTunneled(new QuestObjectiveUpdatePacket
                {
                    QuestId = quest.QuestId,
                    ObjectiveId = activeGoal.NameId,
                    CurrentCount = collected,
                    CompletedPercentage = req > 0 ? (float)collected / req : 0f
                });
            }
        }

        if (sendObjectiveTarget)
            SendObjectiveForGoal(player, quest, done);
    }

    private static void SendObjectiveActivated(Player player, int questId, QuestGoal goal)
    {
        player.SendTunneled(new QuestObjectiveActivatedPacket
        {
            QuestId = questId,
            ObjectiveId = goal.NameId,
            RequiredCount = goal.RequiredCount,
            Unknown2 = false
        });
    }

    private static ulong GoalTargetGuid(QuestDefinition quest, int goalIndex)
    {
        var goals = quest.Goals;
        if (goalIndex >= 0 && goalIndex < goals.Count && goals[goalIndex].TargetGuid != 0)
            return goals[goalIndex].TargetGuid;
        return quest.TargetGuid;
    }

    private ulong ResolveGoalTargetGuid(Player player, QuestDefinition quest, int goalIndex)
    {
        var goals = quest.Goals;

        if (goalIndex >= 0 && goalIndex < goals.Count
            && goals[goalIndex].Type == QuestGoalType.Collect)
        {
            var nearest = NearestCollectionNode(player, goals[goalIndex].CollectNodeType);
            if (nearest is not null)
                return nearest.Guid;
        }

        if (goalIndex >= 0 && goalIndex < goals.Count && goals[goalIndex].IsCountedTalk)
        {
            var untalked = NearestUntalkedTarget(player, goals[goalIndex]);
            if (untalked != 0)
                return untalked;
        }

        return GoalTargetGuid(quest, goalIndex);
    }

    private static CollectionNode? NearestCollectionNode(Player player, string nodeType)
    {
        if (string.IsNullOrEmpty(nodeType))
            return null;

        CollectionNode? nearest = null;
        var best = float.MaxValue;

        foreach (var npc in player.Zone.Npcs)
        {
            if (npc is not CollectionNode node || node.TypeDefinition.Key != nodeType)
                continue;

            var dx = node.Position.X - player.Position.X;
            var dz = node.Position.Z - player.Position.Z;
            var distance = dx * dx + dz * dz;

            if (distance < best)
            {
                best = distance;
                nearest = node;
            }
        }

        return nearest;
    }
    private void SendObjectiveForGoal(Player player, QuestDefinition quest, int goalIndex)
    {
        var goals = quest.Goals;

        if (goalIndex >= 0 && goalIndex < goals.Count
            && goals[goalIndex].Type == QuestGoalType.ReachLocation
            && goals[goalIndex].ReachPosition.Length >= 3)
        {
            var rp = goals[goalIndex].ReachPosition;
            var reachPos = new Vector4(rp[0], rp[1], rp[2], 1f);
            var reachZoneId = player.Zone is StartingZone reachZone
                ? reachZone.GetZoneAreaId(reachPos)
                : player.Zone.Id;

            player.SendTunneled(new ObjectiveTargetUpdatePacket
            {
                Active = true,
                LocationX = reachPos.X,
                LocationZ = reachPos.Z,
                ZoneId = reachZoneId,
                Guid = 0,
                NameId = goals[goalIndex].NameId,
                PositionX = reachPos.X,
                PositionY = reachPos.Y,
                PositionZ = reachPos.Z,
                PositionW = 1f
            });
            return;
        }

        SendObjectiveTarget(player, ResolveGoalTargetGuid(player, quest, goalIndex));
    }

    private void SendObjectiveTarget(Player player, ulong targetGuid)
    {
        if (targetGuid == 0 || !player.Zone.TryGetNpc(targetGuid, out var target))
            return;

        var pos = target.Position;
        var zoneAreaId = player.Zone is StartingZone startingZone
            ? startingZone.GetZoneAreaId(pos)
            : player.Zone.Id;

        player.SendTunneled(new ObjectiveTargetUpdatePacket
        {
            Active = true,
            LocationX = pos.X,
            LocationZ = pos.Z,
            ZoneId = zoneAreaId,
            Guid = targetGuid,
            NameId = target.NameId,
            PositionX = pos.X,
            PositionY = pos.Y,
            PositionZ = pos.Z,
            PositionW = 1f
        });
    }

    public void RefreshObjectiveTarget(Player player)
    {
        if (TryGetTrackedGoal(player, out var quest, out var goalIndex))
            SendObjectiveForGoal(player, quest, goalIndex);
        else
            player.SendTunneled(new ObjectiveTargetUpdatePacket { Active = false });
    }

    public bool TryGetActiveObjectiveTarget(Player player, out Vector3 targetPosition)
    {
        if (TryGetTrackedGoal(player, out var quest, out var goalIndex))
        {
            var goals = quest.Goals;

            var onGoal = goalIndex >= 0 && goalIndex < goals.Count;

            if (onGoal && goals[goalIndex].Type == QuestGoalType.ReachLocation
                && goals[goalIndex].ReachPosition.Length >= 3)
            {
                var rp = goals[goalIndex].ReachPosition;
                targetPosition = new Vector3(rp[0], rp[1], rp[2]);
                return true;
            }

            var guid = ResolveGoalTargetGuid(player, quest, goalIndex);
            if (guid != 0 && player.Zone.TryGetNpc(guid, out var target))
            {
                targetPosition = new Vector3(target.Position.X, target.Position.Y, target.Position.Z);
                return true;
            }
        }

        targetPosition = default;
        return false;
    }

    private bool TryGetTrackedGoal(Player player, out QuestDefinition quest, out int goalIndex)
    {
        if (player.ActiveQuestId != 0
            && player.Quests.TryGetValue(player.ActiveQuestId, out var activeCompleted) && !activeCompleted
            && TryGetTrackableGoal(player, player.ActiveQuestId, out quest, out goalIndex))
        {
            return true;
        }

        foreach (var (questId, completed) in player.Quests)
        {
            if (completed)
                continue;
            if (TryGetTrackableGoal(player, questId, out quest, out goalIndex))
                return true;
        }

        quest = null!;
        goalIndex = -1;
        return false;
    }

    private bool TryGetTrackableGoal(Player player, int questId, out QuestDefinition quest, out int goalIndex)
    {
        quest = null!;
        goalIndex = -1;
        if (!_resourceManager.Quests.TryGet(questId, out var q))
            return false;

        int done = player.QuestGoalProgress.TryGetValue(questId, out var progress) ? progress : 0;
        var goals = q.Goals;

        if (done >= 0 && done < goals.Count
            && goals[done].Type == QuestGoalType.ReachLocation
            && goals[done].ReachPosition.Length >= 3)
        {
            quest = q;
            goalIndex = done;
            return true;
        }

        ulong guid = ResolveGoalTargetGuid(player, q, done);
        if (guid != 0 && player.Zone.TryGetNpc(guid, out _))
        {
            quest = q;
            goalIndex = done;
            return true;
        }
        return false;
    }

    private void GrantReward(Player player, QuestDefinition quest)
    {
        var coins = quest.RewardCoins;
        if (coins > 0)
        {
            int newTotal;
            using (var db = _dbContextFactory.CreateDbContext())
            {
                var dbCharacter = db.Characters.FirstOrDefault(c => c.Id == player.CharacterId);
                if (dbCharacter is null)
                    return;

                dbCharacter.Coins += coins;
                db.SaveChanges();
                newTotal = dbCharacter.Coins;
            }

            player.Coins = newTotal;
            player.SendTunneled(new ClientUpdatePacketCoinCount { Coins = newTotal });
        }

        // Experience is recorded in the quest data, but this server has no experience system to
        // grant it to yet. Coins and items below are granted normally.
        var experience = quest.RewardExperience;

        var grantedCollection = quest.RewardCollectionId != 0 &&
            _resourceManager.Collections.TryGetValue(quest.RewardCollectionId, out var rewardedCollection)
                ? rewardedCollection
                : null;

        if (coins > 0 || experience > 0 || grantedCollection is not null)
        {
            var celebration = new QuestRewardBundlePacket();

            // Success also gates whether each entry's tail is written; only the actual
            // collection id needs one here, so only flip it when there is a collection to report.
            celebration.RewardBundle.Success = grantedCollection is not null;
            celebration.RewardBundle.Unknown1 = coins;
            celebration.RewardBundle.RewardKind = experience;
            celebration.RewardBundle.Unknown3 = 0;
            celebration.RewardBundle.Multiplier = 1f;
            celebration.RewardBundle.IconId = -1;
            celebration.RewardBundle.NameId = -1;

            player.SendTunneled(celebration);
        }

        foreach (var itemDefinitionId in quest.RewardItems)
        {
            GrantItem(player, itemDefinitionId);

            player.SendTunneled(new RewardNonBundledItemPacket { ItemDefinitionId = itemDefinitionId, Quantity = 1 });
        }
    }

    private void GrantItem(Player player, int definitionId)
    {
        if (!_resourceManager.ClientItemDefinitions.TryGetValue(definitionId, out var itemDef))
            return;

        int tint = itemDef.IsTintable ? 0 : itemDef.Icon.TintId;

        int itemId, count;
        using (var db = _dbContextFactory.CreateDbContext())
        {
            var row = db.Characters
                .Where(c => c.Id == player.CharacterId)
                .Select(c => new
                {
                    Character = c,
                    Item = c.Items.FirstOrDefault(i => i.Definition == definitionId && i.Tint == tint),
                    NextId = c.Items.Max(i => (int?)i.Id) ?? 0
                })
                .FirstOrDefault();

            if (row is null)
                return;

            if (row.Item is not null)
            {
                row.Item.Count += 1;
                itemId = row.Item.Id;
                count = row.Item.Count;
            }
            else
            {
                var dbItem = new DbItem { Id = row.NextId + 1, Definition = definitionId, Tint = tint, Count = 1 };
                row.Character.Items.Add(dbItem);
                itemId = dbItem.Id;
                count = 1;
            }

            db.SaveChanges();
        }

        var clientItem = player.Items.FirstOrDefault(x => x.Definition == definitionId && x.Tint == tint);
        if (clientItem is not null)
        {
            clientItem.Count = count;
            player.SendTunneled(new ClientUpdatePacketItemUpdate { ItemGuid = clientItem.Id, Count = clientItem.Count });
        }
        else
        {
            clientItem = new ClientItem { Id = itemId, Tint = tint, Count = count, Definition = definitionId };
            player.Items.Add(clientItem);

            using var writer = new PacketWriter();
            clientItem.Serialize(writer);
            itemDef.Serialize(writer);
            player.SendTunneled(new ClientUpdatePacketItemAdd { Payload = writer.Buffer });
        }
    }
}
