using System.Collections.Generic;

using Sanctuary.Core.IO;

namespace Sanctuary.Packet.Common;

public class ClientPetData : ISerializableType
{
    public Dictionary<int, int> ClothingItems = [];
    public int Id;
    public int ItemDefinitionId;
    public int SpeciesId;
    public int NameId;
    public int ImageSetId;
    public int TintId;
    public string TintAlias = string.Empty;
    public string TextureAlias = string.Empty;
    public ulong Guid;
    public string Name = string.Empty;
    public int ModelId;
    public bool IsActive;
    public int Hunger;
    public int Cleanliness;
    public int Happiness;
    public int Mood;
    public int TrickLevel;
    public int TrickProgress;
    public List<ClientPetTrickData> Tricks = [];
    public List<int> ItemGuids = [];
    public List<ClientPetProfileItemData> ProfileItems = [];
    public int[] FavoriteTricks = [0, 0, 0, 0];
    public int[] UiData = [0, 0, 0, 0, 0, 0, 0, 0];

    public void Serialize(PacketWriter writer)
    {
        writer.Write(Id);
        writer.Write(true);
        writer.Write(SpeciesId);
        writer.Write((float)Hunger);
        writer.Write((float)Cleanliness);
        writer.Write((float)Happiness);
        writer.Write(Mood / 34f);
        writer.Write(false);

        writer.Write(Tricks.Count);
        foreach (var trick in Tricks)
            trick.Serialize(writer);

        writer.Write(ItemGuids.Count);
        foreach (var itemGuid in ItemGuids)
            writer.Write(itemGuid);

        writer.Write(ProfileItems.Count);
        foreach (var profileItem in ProfileItems)
            profileItem.Serialize(writer);

        writer.Write(Name);
        writer.Write(NameId);
        writer.Write(ModelId);
        writer.Write(TintAlias);
        writer.Write(ImageSetId);
        writer.Write(false);
        writer.Write(TintId);
        writer.Write(IsActive);

        for (var i = 0; i < 4; i++)
            writer.Write(i < FavoriteTricks.Length ? FavoriteTricks[i] : 0);

        for (var i = 0; i < 8; i++)
            writer.Write(i < UiData.Length ? UiData[i] : 0);
    }
}

public class ClientPetTrickData
{
    public int TrickId;
    public int NameId;
    public int IconId;
    public int Level = 1;
    public int Progress;
    public int Unknown6;
    public int Unknown7;
    public int Slot;
    public bool IsUnlocked = true;

    public void Serialize(PacketWriter writer)
    {
        writer.Write(TrickId);
        writer.Write(TrickId);
        writer.Write(IconId);
        writer.Write(NameId);
        writer.Write(Level);
        writer.Write(Progress);
        writer.Write(Unknown6);
        writer.Write(Unknown7);
        writer.Write(Slot);
        writer.Write(IsUnlocked);
    }
}

public class ClientPetProfileItemData
{
    public int SlotId;
    public int Unknown;
    public int Unknown2;

    public void Serialize(PacketWriter writer)
    {
        writer.Write(SlotId);
        writer.Write(Unknown);
        writer.Write(Unknown2);
    }
}