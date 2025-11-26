using Sanctuary.Core.IO;

namespace Sanctuary.Packet;

public class ClientUpdatePacketItemUpdate : BaseClientUpdatePacket, ISerializablePacket
{
    public new const short OpCode = 3;

    public int ItemGuid;

    public int Count;

    public int ActionBarId = -1;

    public int ConsumedCount;

    public int AbilityCount;

    public long RentalExpirationTime;

    public ClientUpdatePacketItemUpdate() : base(OpCode)
    {
    }

    public byte[] Serialize()
    {
        using var writer = new PacketWriter();

        Write(writer);

        writer.Write(ItemGuid);

        writer.Write(Count);

        writer.Write(ActionBarId);

        writer.Write(ConsumedCount);

        writer.Write(AbilityCount);

        writer.Write(RentalExpirationTime);

        return writer.Buffer;
    }
}