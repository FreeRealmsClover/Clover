using Sanctuary.Core.IO;

namespace Sanctuary.Packet;

public class ClientUpdatePacketCoinCount : BaseClientUpdatePacket, ISerializablePacket
{
    public new const short OpCode = 19;

    public int Coins;

    public ClientUpdatePacketCoinCount() : base(OpCode)
    {
    }

    public byte[] Serialize()
    {
        using var writer = new PacketWriter();

        Write(writer);

        writer.Write(Coins);

        return writer.Buffer;
    }
}