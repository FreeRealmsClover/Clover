using System.Collections.Generic;

using Sanctuary.Core.IO;
using Sanctuary.Packet.Common;

namespace Sanctuary.Packet;

public class FriendListPacket : BaseFriendPacket, ISerializablePacket
{
    public new const byte OpCode = 1;

    public List<FriendData> Friends = [];

    public FriendListPacket() : base(OpCode)
    {
    }

    public byte[] Serialize()
    {
        using var writer = new PacketWriter();

        Write(writer);

        writer.Write(Friends);

        return writer.Buffer;
    }
}