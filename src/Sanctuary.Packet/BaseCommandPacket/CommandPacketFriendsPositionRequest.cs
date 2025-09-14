namespace Sanctuary.Packet;

public class CommandPacketFriendsPositionRequest : BaseCommandPacket
{
    public new const short OpCode = 21;

    public CommandPacketFriendsPositionRequest() : base(OpCode)
    {
    }
}