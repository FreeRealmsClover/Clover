using System;

using Sanctuary.Core.IO;

namespace Sanctuary.Packet;

public class ClientUpdatePacketItemAdd : BaseClientUpdatePacket, ISerializablePacket
{
    public new const short OpCode = 2;

    public byte[] Payload = Array.Empty<byte>();

    public ClientUpdatePacketItemAdd() : base(OpCode)
    {
    }

    public byte[] Serialize()
    {
        using var writer = new PacketWriter();

        Write(writer);

        writer.WritePayload(Payload);

        return writer.Buffer;
    }
}