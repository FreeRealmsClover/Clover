using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Sanctuary.Core.Helpers;
using Sanctuary.Database;
using Sanctuary.Database.Entities;
using Sanctuary.Game;
using Sanctuary.Game.Entities;
using Sanctuary.Packet;
using Sanctuary.Packet.Common;

namespace Sanctuary.Gateway;

public static class PetCustomizationService
{
    private static IDbContextFactory<DatabaseContext> _database = null!;
    private static IResourceManager _resources = null!;

    public static void ConfigureServices(IServiceProvider provider)
    {
        _database = provider.GetRequiredService<IDbContextFactory<DatabaseContext>>();
        _resources = provider.GetRequiredService<IResourceManager>();
    }

    public static void Load(Player player, IEnumerable<DbItem> items)
    {
        foreach (var item in items)
        {
            var pet = player.Pets.FirstOrDefault(pet => pet.Id == item.Id);
            if (pet is null) continue;
            if (!string.IsNullOrWhiteSpace(item.PetName)) { pet.Name = item.PetName; pet.NameId = 0; }
            if (!string.IsNullOrWhiteSpace(item.PetClothing))
            {
                try
                {
                    var saved = JsonSerializer.Deserialize<Dictionary<int, int>>(item.PetClothing);
                    foreach (var entry in saved ?? [])
                    {
                        var owned = player.Items.FirstOrDefault(owned => owned.Id == entry.Value && owned.Count > 0);
                        if (owned is not null && _resources.ClientItemDefinitions.TryGetValue(owned.Definition, out var definition) &&
                            definition.Slot == entry.Key && PetClothingRules.CanWear(pet, definition))
                            pet.ClothingItems[entry.Key] = entry.Value;
                    }
                }
                catch (JsonException) { /* Ignore malformed stored outfits; ownership is still intact. */ }
            }
            PetClothingRules.UpdatePanel(pet);
        }
    }

    public static ClientPetData? Resolve(Player player, int petId = 0, ulong guid = 0)
    {
        if (guid != 0)
        {
            if (player.ActivePetGuid == guid && player.ActivePetId > 0)
                return player.Pets.FirstOrDefault(pet => pet.Id == player.ActivePetId);
            // The client also sends an owned pet-list ID widened to UInt64 in name dialogs.
            return player.Pets.FirstOrDefault(pet => pet.Guid == guid || (ulong)pet.Id == guid);
        }
        return player.Pets.FirstOrDefault(pet => pet.Id == (petId > 0 ? petId : player.ActivePetId));
    }

    public static CheckNameResponse ValidateName(Player player, ulong guid, string name)
    {
        if (Resolve(player, guid: guid) is null) return CheckNameResponse.MissingPet;
        name = name.Trim();
        if (name.Length is < 3 or > 32) return CheckNameResponse.IncorrectLength;
        if (name.Any(c => !char.IsLetter(c) && c is not (' ' or '-' or '\''))) return CheckNameResponse.IllegalCharacters;
        if (_resources.NameFilter.Any(token => name.Contains(token, StringComparison.OrdinalIgnoreCase)))
            return CheckNameResponse.Profane;
        return CheckNameResponse.Available;
    }

    public static bool Rename(Player player, ClientPetData pet, string name)
    {
        name = name.Trim();
        if (ValidateName(player, pet.Guid, name) != CheckNameResponse.Available) return false;
        if (pet.Name == name && pet.NameId == 0) { SendName(player, pet); return true; }
        using var db = _database.CreateDbContext();
        var characterId = GuidHelper.GetPlayerId(player.Guid);
        if (db.Items.Where(item => item.CharacterId == characterId && item.Id == pet.Id &&
            item.Definition == pet.ItemDefinitionId && item.Count > 0)
            .ExecuteUpdate(update => update.SetProperty(item => item.PetName, name)) != 1)
            return false;
        pet.Name = name;
        pet.NameId = 0;
        PetCompanionService.ApplyCustomization(player, pet, Attachments(player, pet));
        SendName(player, pet);
        return true;
    }

    public static bool Equip(Player player, ClientPetData pet, int slot, int itemId)
    {
        var outfit = new Dictionary<int, int>(pet.ClothingItems);
        if (itemId == 0)
        {
            if (slot is not (1 or 3 or 5 or 6)) return false;
            outfit.Remove(slot);
        }
        else
        {
            var item = player.Items.FirstOrDefault(item => item.Id == itemId && item.Count > 0);
            if (item is null || !_resources.ClientItemDefinitions.TryGetValue(item.Definition, out var definition) ||
                !PetClothingRules.CanWear(pet, definition) || slot != 0 && slot != definition.Slot)
                return false;
            slot = definition.Slot;
            outfit[slot] = itemId;
        }

        var previousItemId = pet.ClothingItems.GetValueOrDefault(slot);
        var changed = false;
        if (!outfit.OrderBy(pair => pair.Key).SequenceEqual(pet.ClothingItems.OrderBy(pair => pair.Key)))
        {
            var json = JsonSerializer.Serialize(outfit);
            using var db = _database.CreateDbContext();
            var characterId = GuidHelper.GetPlayerId(player.Guid);
            if (db.Items.Where(item => item.CharacterId == characterId && item.Id == pet.Id &&
                item.Definition == pet.ItemDefinitionId && item.Count > 0)
                .ExecuteUpdate(update => update.SetProperty(item => item.PetClothing, json)) != 1)
                return false;
            pet.ClothingItems = outfit;
            changed = true;
            PetClothingRules.UpdatePanel(pet);
            PetCompanionService.ApplyCustomization(player, pet, Attachments(player, pet));
        }
        SendClothing(player, pet, slot, previousItemId: previousItemId);
        if (changed) PetCompanionService.SendActiveState(player);
        return true;
    }

    internal static List<CharacterAttachmentData> Attachments(Player player, ClientPetData pet)
    {
        var result = new List<CharacterAttachmentData>();
        foreach (var entry in pet.ClothingItems)
        {
            var item = player.Items.FirstOrDefault(item => item.Id == entry.Value && item.Count > 0);
            if (item is not null && _resources.ClientItemDefinitions.TryGetValue(item.Definition, out var definition) &&
                definition.Slot == entry.Key && PetClothingRules.CanWear(pet, definition))
                result.Add(PetClothingRules.Attachment(item, definition));
        }
        return result;
    }

    public static void SendWardrobe(Player player)
    {
        var pet = Resolve(player);
        if (pet is null) return;
        foreach (var slot in new[] { 1, 3, 5, 6 }) SendClothing(player, pet, slot);
    }

    private static void SendClothing(Player player, ClientPetData pet, int slot, int previousItemId = 0)
    {
        var selected = pet.ClothingItems.GetValueOrDefault(slot);
        player.SendTunneled(new PetEquipPacket
        {
            SlotId = slot,
            ItemRecordId = selected == 0 ? previousItemId : selected,
            IsEquipped = selected > 0
        });
    }

    private static void SendName(Player player, ClientPetData pet)
    {
        player.SendTunneled(new PetSetNamePacket { PetGuid = pet.IsActive ? player.ActivePetGuid : pet.Guid, Name = pet.Name });
        PetCompanionService.SendActiveState(player);
    }
}
