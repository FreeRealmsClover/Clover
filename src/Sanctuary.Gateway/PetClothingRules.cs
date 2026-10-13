using System;
using System.Collections.Generic;
using System.Linq;

using Sanctuary.Packet.Common;

namespace Sanctuary.Gateway;

internal static class PetClothingRules
{
    private static readonly HashSet<int> CatModels = [50, 540, 983, 984, 1013, 1014, 1015, 1031, 1032, 1033, 3928, 3929, 3930, 3931];
    public static bool IsWearable(ClientItemDefinition definition)
        => definition.Type == 3 && definition.CategoryId == 6 && definition.Slot is 1 or 3 or 5 or 6;

    public static bool CanWear(ClientPetData pet, ClientItemDefinition definition)
        => IsWearable(definition) &&
            (pet.SpeciesId == 652 && definition.ModelName?.Contains("pet_dog_", StringComparison.OrdinalIgnoreCase) == true ||
             CatModels.Contains(pet.ModelId) && definition.ModelName?.Contains("pet_cat_", StringComparison.OrdinalIgnoreCase) == true);

    public static void UpdatePanel(ClientPetData pet)
    {
        if (pet.SpeciesId == 652)
            pet.ProfileItems = [new() { SlotId=31, Unknown=31, Unknown2=5 }, new() { SlotId=73, Unknown=73, Unknown2=3 },
                new() { SlotId=144, Unknown=144 }, new() { SlotId=145, Unknown=145 }, new() { SlotId=146, Unknown=146 }];
        else if (CatModels.Contains(pet.ModelId))
            pet.ProfileItems = [new() { SlotId = 61, Unknown = 61, Unknown2 = 5 }];
        else
            pet.ProfileItems = [];

        pet.ItemGuids = pet.ProfileItems.Count == 0 ? [] :
            new[] { 1, 3, 5, 6 }.Select(slot => pet.ClothingItems.GetValueOrDefault(slot)).Append(0).ToList();
    }

    public static CharacterAttachmentData Attachment(ClientItem item, ClientItemDefinition definition)
        => new()
        {
            ModelName = definition.ModelName,
            TextureAlias = definition.TextureAlias,
            TintAlias = definition.TintAlias,
            TintId = item.Tint == 0 ? definition.Icon.TintId : item.Tint,
            CompositeEffectId = definition.CompositeEffectId,
            Slot = definition.Slot
        };
}
