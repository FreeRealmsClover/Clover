using System.Linq;

using Sanctuary.Core.Helpers;
using Sanctuary.Game;
using Sanctuary.Game.Entities;
using Sanctuary.Gateway.Admin;
using Sanctuary.Packet.Common;

namespace Sanctuary.Gateway;

internal static class PetPurchaseHelper
{
    public static bool IsSupported(ClientItemDefinition definition)
        => definition.Type == 2 && PetDefinitionLookup.TryGet(definition, out _);

    // Ownership stays in the existing item table. One item record represents one pet.
    public static void LoadPets(Player player, IResourceManager resourceManager)
    {
        var existing = player.Pets.ToDictionary(pet => pet.Id);
        player.Pets.Clear();
        foreach (var item in player.Items.OrderBy(item => item.Id))
        {
            if (item.Count <= 0 ||
                !resourceManager.ClientItemDefinitions.TryGetValue(item.Definition, out var definition) ||
                definition.Type != 2 || !PetDefinitionLookup.TryGet(definition, out var metadata))
                continue;

            if (existing.TryGetValue(item.Id, out var cached))
            {
                cached.IsActive = item.Id == player.ActivePetId;
                PetClothingRules.UpdatePanel(cached);
                player.Pets.Add(cached);
                continue;
            }
            var pet = new ClientPetData
            {
                Id = item.Id,
                Guid = 0x7000000000000000UL | (GuidHelper.GetPlayerId(player.Guid) << 16) | (ushort)item.Id,
                ItemDefinitionId = definition.Id,
                SpeciesId = metadata.SpeciesId,
                Name = metadata.Name,
                NameId = metadata.NameId,
                ModelId = metadata.ModelId,
                ImageSetId = metadata.ImageSetId,
                TintId = item.Tint > 0 ? item.Tint : metadata.TintId,
                TintAlias = metadata.TintAlias,
                TextureAlias = definition.TextureAlias,
                Hunger = 100,
                Cleanliness = 100,
                Happiness = 100,
                Mood = 100,
                IsActive = item.Id == player.ActivePetId
            };
            PetClothingRules.UpdatePanel(pet);
            player.Pets.Add(pet);
        }
    }
}
