using Sanctuary.Core.IO;

namespace Sanctuary.Packet;

public class ChatPacketFromStringId : BaseChatPacket, ISerializablePacket
{
    public new const short OpCode = 4;

    public ulong SpeakerGuid;

    public int StringId;

    public bool IsEmote;
    public bool IsChatLogged;

    public bool HasColor;

    // 0 - White (0xFFFFFF)
    // 1 - Red (0xFF0000)
    // 2 - Yellow (0xFFFF00)
    // 3 - Green (0x00FF00)
    // 4 - Blue (0x0000FF)
    public int ColorId;

    public ulong OwnerGuid;
    public ulong TargetGuid;

    public int ElapsedTime;

    public ChatPacketFromStringId() : base(OpCode)
    {
    }

    public byte[] Serialize()
    {
        using var writer = new PacketWriter();

        base.Write(writer);

        writer.Write(SpeakerGuid);

        writer.Write(StringId);

        writer.Write(IsEmote);
        writer.Write(IsChatLogged);

        writer.Write(HasColor);
        writer.Write(ColorId);

        writer.Write(TargetGuid);
        writer.Write(OwnerGuid);

        writer.Write(ElapsedTime);

        return writer.Buffer;
    }
}