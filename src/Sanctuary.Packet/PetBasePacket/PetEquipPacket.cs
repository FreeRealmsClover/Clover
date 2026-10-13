using Sanctuary.Core.IO;

namespace Sanctuary.Packet;

public class PetEquipPacket : ISerializablePacket
{
    public const byte OpCode = 11;
    public int SlotId;
    public int ItemRecordId;
    public bool IsEquipped = true;
    public byte[] Serialize()
    {
        using var writer = new PacketWriter();
        writer.Write(PetListPacket.BaseOpCode);
        writer.Write(OpCode);
        writer.Write(IsEquipped);
        writer.Write(ItemRecordId);
        writer.Write(SlotId);
        return writer.Buffer;
    }
}
