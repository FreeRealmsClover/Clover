using Sanctuary.Core.IO;
using Sanctuary.Packet.Common;

namespace Sanctuary.Packet;

public class ClientUpdatePacketUpdateActionBarSlot : BaseClientUpdatePacket, ISerializablePacket
{
    public new const short OpCode = 25;

    public ActionBarData Data = new();
    public ActionBarSlot Slot = new();

    public ClientUpdatePacketUpdateActionBarSlot() : base(OpCode)
    {
    }

    public byte[] Serialize()
    {
        using var writer = new PacketWriter();

        Write(writer);

        Data.Serialize(writer);
        Slot.Serialize(writer);

        return writer.Buffer;
    }
}