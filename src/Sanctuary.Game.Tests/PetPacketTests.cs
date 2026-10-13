using Microsoft.VisualStudio.TestTools.UnitTesting;

using Sanctuary.Core.IO;
using Sanctuary.Packet;

namespace Sanctuary.Game.Tests;

[TestClass]
public sealed class PetPacketTests
{
    [TestMethod]
    public void EquipResponseUsesOriginalClientFieldOrder()
    {
        var packet = new PetEquipPacket { IsEquipped = true, ItemRecordId = 13, SlotId = 3 };
        CollectionAssert.AreEqual(new byte[] { 53, 0, 11, 1, 13, 0, 0, 0, 3, 0, 0, 0 }, packet.Serialize());
    }

    [TestMethod]
    public void UnequipResponseRetainsPreviousRecordAndSlot()
    {
        var packet = new PetEquipPacket { IsEquipped = false, ItemRecordId = 14, SlotId = 6 };
        CollectionAssert.AreEqual(new byte[] { 53, 0, 11, 0, 14, 0, 0, 0, 6, 0, 0, 0 }, packet.Serialize());
    }

    [TestMethod]
    public void NameOnlyRequestSelectsActivePet()
    {
        using var writer = new PacketWriter();
        writer.Write((short)53);
        writer.Write((byte)6);
        writer.Write("Captain Beau");
        Assert.IsTrue(PetRenameRequest.TryDeserialize(writer.Buffer, out var request));
        Assert.AreEqual(0, request.PetId);
        Assert.AreEqual(0UL, request.PetGuid);
        Assert.AreEqual("Captain Beau", request.Name);
    }

    [TestMethod]
    public void RenameResponseMatchesClientGuidStringAndAcceptanceLayout()
    {
        var packet = new PetSetNamePacket { PetGuid = 83, Name = "Beau", IsAccepted = true };
        Assert.IsTrue(PetRenameRequest.TryDeserialize(packet.Serialize(), out var request));
        Assert.AreEqual(83UL, request.PetGuid);
        Assert.AreEqual("Beau", request.Name);
    }

    [TestMethod]
    public void ClothingNoneRequestRetainsSlotAndClearsRecord()
    {
        Assert.IsTrue(PetClothingRequest.TryDeserialize(new byte[] { 53, 0, 11, 1, 3, 0, 0, 0, 0, 0, 0, 0 }, out var request));
        Assert.AreEqual(3, request.Slot);
        Assert.AreEqual(0, request.ItemId);
    }

    [TestMethod]
    public void TruncatedRenameCannotBecomeAValidRequest()
    {
        Assert.IsFalse(PetRenameRequest.TryDeserialize(new byte[] { 53, 0, 6, 9, 0, 0, 0, 65 }, out _));
    }
}
