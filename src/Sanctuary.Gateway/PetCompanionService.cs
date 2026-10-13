using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

using Sanctuary.Game.Entities;
using Sanctuary.Packet;
using Sanctuary.Packet.Common;

namespace Sanctuary.Gateway;

internal static class PetCompanionService
{
    private static readonly ConcurrentDictionary<ulong, Companion> ActivePets = new();
    private static readonly ConditionalWeakTable<Player, object> OwnerLocks = new();

    public static bool Summon(Player owner, ClientPetData data)
    {
        lock (OwnerLocks.GetValue(owner, _ => new object()))
            return SummonCore(owner, data);
    }

    private static bool SummonCore(Player owner, ClientPetData data)
    {
        if (ActivePets.TryGetValue(owner.Guid, out var current) &&
            current.Id == data.Id && current.Npc.Zone == owner.Zone)
        {
            SendActiveState(owner);
            return true;
        }

        Dismiss(owner);
        if (!owner.Zone.TryCreateNpc(out var npc))
            return false;

        npc.ModelId = data.ModelId;
        npc.Name = data.Name;
        npc.NameId = data.NameId;
        npc.TextureAlias = data.TextureAlias;
        npc.TintAlias = data.TintAlias;
        npc.TintId = data.TintId;
        npc.ImageSetId = data.ImageSetId;
        npc.Scale = 0.75f;
        npc.Disposition = 2;
        npc.IsInteractable = false;
        npc.StandAnimId = -1;
        npc.WalkAnimId = -1;
        npc.RunAnimId = -1;
        npc.Speed = 3f;
        npc.Attachments = PetCustomizationService.Attachments(owner, data);

        var companion = new Companion(data.Id, npc) { EquipmentIds = new Dictionary<int, int>(data.ClothingItems) };
        ActivePets[owner.Guid] = companion;
        owner.ActivePetId = data.Id;
        owner.ActivePetGuid = npc.Guid;
        npc.UpdateEveryTickAction = () => Follow(owner, companion);
        npc.Visible = true;
        npc.UpdatePosition(new Vector4(PetFollowMovement.GetTarget(owner.Position, owner.Rotation), 1),
            owner.Rotation, movementState: 0);
        data.IsActive = true;
        SendActiveState(owner);
        return true;
    }

    public static void Dismiss(Player owner)
    {
        lock (OwnerLocks.GetValue(owner, _ => new object()))
            DismissCore(owner);
    }

    private static void DismissCore(Player owner)
    {
        if (ActivePets.TryRemove(owner.Guid, out var companion))
        {
            companion.Npc.UpdateEveryTickAction = null;
            companion.Npc.Visible = false;
            companion.Npc.Dispose();
        }
        owner.ActivePetId = 0;
        owner.ActivePetGuid = 0;
        foreach (var pet in owner.Pets)
            pet.IsActive = false;
    }

    public static void SendActiveState(Player owner)
    {
        owner.SendTunneled(new PetActivePacket { PetId = owner.ActivePetId, PetGuid = owner.ActivePetGuid });
        owner.SendTunneled(new PetListPacket
        {
            Pets = owner.Pets,
            ActivePetId = owner.ActivePetId,
            ActivePetGuid = owner.ActivePetGuid
        });
    }

    public static void ApplyCustomization(Player owner, ClientPetData data, List<CharacterAttachmentData> attachments)
    {
        lock (OwnerLocks.GetValue(owner, _ => new object()))
        {
            if (!ActivePets.TryGetValue(owner.Guid, out var companion) || companion.Id != data.Id)
                return;
            var npc = companion.Npc;
            var slots = npc.Attachments.Select(attachment => attachment.Slot)
                .Union(attachments.Select(attachment => attachment.Slot)).ToArray();
            npc.Attachments = attachments;
            npc.Name = data.Name;
            npc.NameId = data.NameId;
            foreach (var viewer in npc.VisiblePlayers.Values)
            {
                viewer.SendTunneled(new PlayerUpdatePacketRenamePlayer
                {
                    Guid = npc.Guid,
                    Name = new NameData { FirstName = data.Name }
                });
                foreach (var slot in slots)
                    viewer.SendTunneled(new PlayerUpdatePacketEquipItemChange
                    {
                        Guid = npc.Guid,
                        Id = data.ClothingItems.TryGetValue(slot, out var itemId) ? itemId : companion.EquipmentIds.GetValueOrDefault(slot),
                        Attachment = attachments.FirstOrDefault(attachment => attachment.Slot == slot) ??
                            new CharacterAttachmentData { Slot = slot, ModelName = string.Empty },
                        ProfileId = 0,
                        WieldType = 0
                    });
            }
            companion.EquipmentIds = new Dictionary<int, int>(data.ClothingItems);
        }
    }

    private static bool Follow(Player owner, Companion companion)
    {
        lock (OwnerLocks.GetValue(owner, _ => new object()))
            return FollowCore(owner, companion);
    }

    private static bool FollowCore(Player owner, Companion companion)
    {
        if (!ActivePets.TryGetValue(owner.Guid, out var current) || current != companion)
            return true;
        var npc = companion.Npc;
        if (npc.Zone != owner.Zone)
        {
            Dismiss(owner);
            SendActiveState(owner);
            return true;
        }

        var position = new Vector3(npc.Position.X, npc.Position.Y, npc.Position.Z);
        var step = PetFollowMovement.Advance(position, npc.Rotation,
            PetFollowMovement.GetTarget(owner.Position, owner.Rotation), npc.Zone.TickDeltaSeconds);
        if (step.Position != position || step.State != companion.State)
        {
            if (step.State != companion.State)
            {
                var speed = new PlayerUpdatePacketExpectedSpeed { Guid = npc.Guid, ExpectedSpeed = step.Speed };
                foreach (var viewer in npc.VisiblePlayers.Values)
                    viewer.SendTunneled(speed);
            }
            npc.UpdatePosition(new Vector4(step.Position, 1), step.Rotation, movementState: step.State);
            companion.State = step.State;
        }
        return true;
    }

    private sealed class Companion(int id, Npc npc)
    {
        public int Id { get; } = id;
        public Npc Npc { get; } = npc;
        public byte State;
        public Dictionary<int, int> EquipmentIds = [];
    }
}
