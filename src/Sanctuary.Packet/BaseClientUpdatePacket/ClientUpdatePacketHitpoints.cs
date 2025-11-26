using Sanctuary.Core.IO;

namespace Sanctuary.Packet;

public class ClientUpdatePacketHitpoints : BaseClientUpdatePacket, ISerializablePacket
{
    public new const short OpCode = 1;

    public int CurrentHitpoints;
    public int MaxHitpoints;

    public ClientUpdatePacketHitpoints() : base(OpCode)
    {
    }

    public byte[] Serialize()
    {
        using var writer = new PacketWriter();

        Write(writer);

        writer.Write(CurrentHitpoints);
        writer.Write(MaxHitpoints);

        return writer.Buffer;
    }
}