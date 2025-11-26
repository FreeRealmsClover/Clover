using Sanctuary.Core.IO;

namespace Sanctuary.Packet;

public class PacketMembershipSubscriptionInfo : ISerializablePacket
{
    public const short OpCode = 189;

    public bool IsMember;
    public bool Unknown;

    public byte[] Serialize()
    {
        using var writer = new PacketWriter();

        writer.Write(OpCode);

        writer.Write(IsMember);
        writer.Write(Unknown);

        return writer.Buffer;
    }
}