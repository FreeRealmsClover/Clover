using Sanctuary.Core.IO;

namespace Sanctuary.Packet;

public class PlayerUpdatePacketSlotCompositeEffectOverride : BasePlayerUpdatePacket, ISerializablePacket
{
    public new const short OpCode = 31;

    public ulong Guid;

    public int Slot;
    public int CompositeEffect;

    public PlayerUpdatePacketSlotCompositeEffectOverride() : base(OpCode)
    {
    }

    public byte[] Serialize()
    {
        using var writer = new PacketWriter();

        Write(writer);

        writer.Write(Guid);

        writer.Write(Slot);
        writer.Write(CompositeEffect);

        return writer.Buffer;
    }
}