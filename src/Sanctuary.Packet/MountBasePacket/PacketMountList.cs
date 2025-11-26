using System.Collections.Generic;

using Sanctuary.Core.IO;
using Sanctuary.Packet.Common;

namespace Sanctuary.Packet;

public class PacketMountList : MountBasePacket, ISerializablePacket
{
    public new const byte OpCode = 5;

    public List<PacketMountInfo> Mounts = new List<PacketMountInfo>();

    public PacketMountList() : base(OpCode)
    {
    }

    public byte[] Serialize()
    {
        using var writer = new PacketWriter();

        Write(writer);

        writer.Write(Mounts);

        return writer.Buffer;
    }
}