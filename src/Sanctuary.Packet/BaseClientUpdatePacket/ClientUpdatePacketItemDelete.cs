using Sanctuary.Core.IO;

namespace Sanctuary.Packet;

public class ClientUpdatePacketItemDelete : BaseClientUpdatePacket, ISerializablePacket
{
    public new const short OpCode = 4;

    public int ItemGuid;

    public ClientUpdatePacketItemDelete() : base(OpCode)
    {
    }

    public byte[] Serialize()
    {
        using var writer = new PacketWriter();

        Write(writer);

        writer.Write(ItemGuid);

        return writer.Buffer;
    }
}