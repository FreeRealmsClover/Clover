using System.Collections.Generic;

using Sanctuary.Core.IO;
using Sanctuary.Packet.Common;

namespace Sanctuary.Packet;

public class IgnoreListPacket : BaseIgnorePacket, ISerializablePacket
{
    public new const byte OpCode = 1;

    public List<IgnoreData> Ignores = [];

    public IgnoreListPacket() : base(OpCode)
    {
    }

    public byte[] Serialize()
    {
        using var writer = new PacketWriter();

        Write(writer);

        writer.Write(Ignores);

        return writer.Buffer;
    }
}