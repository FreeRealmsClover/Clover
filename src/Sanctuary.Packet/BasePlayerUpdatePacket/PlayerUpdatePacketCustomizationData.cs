using System.Collections.Generic;

using Sanctuary.Core.IO;
using Sanctuary.Packet.Common;

namespace Sanctuary.Packet;

public class PlayerUpdatePacketCustomizationData : BasePlayerUpdatePacket, ISerializablePacket
{
    public new const short OpCode = 65;

    public List<PlayerCustomizationData> Customizations = [];

    public PlayerUpdatePacketCustomizationData() : base(OpCode)
    {
    }

    public byte[] Serialize()
    {
        using var writer = new PacketWriter();

        Write(writer);

        writer.Write(Customizations);

        return writer.Buffer;
    }
}