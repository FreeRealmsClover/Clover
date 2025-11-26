using System.Collections.Generic;

using Sanctuary.Core.IO;
using Sanctuary.Packet.Common;

namespace Sanctuary.Packet;

public class PlayerUpdatePacketCustomizationChange : BasePlayerUpdatePacket, ISerializablePacket
{
    public new const short OpCode = 39;

    public ulong Guid;

    public bool Preview;

    public List<PlayerCustomizationData> Customizations = [];

    public PlayerUpdatePacketCustomizationChange() : base(OpCode)
    {
    }

    public byte[] Serialize()
    {
        using var writer = new PacketWriter();

        Write(writer);

        writer.Write(Guid);

        writer.Write(Preview);

        writer.Write(Customizations);

        return writer.Buffer;
    }
}