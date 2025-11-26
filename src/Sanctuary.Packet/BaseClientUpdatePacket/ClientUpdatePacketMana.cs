using Sanctuary.Core.IO;

namespace Sanctuary.Packet;

public class ClientUpdatePacketMana : BaseClientUpdatePacket, ISerializablePacket
{
    public new const short OpCode = 13;

    public int CurrentMana;
    public int MaxMana;

    public bool ShowOverHead;

    public ClientUpdatePacketMana() : base(OpCode)
    {
    }

    public byte[] Serialize()
    {
        using var writer = new PacketWriter();

        Write(writer);

        writer.Write(CurrentMana);
        writer.Write(MaxMana);

        writer.Write(ShowOverHead);

        return writer.Buffer;
    }
}