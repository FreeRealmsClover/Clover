namespace Sanctuary.Packet;

public class LobbyGameDefinitionPacketDefinitionsRequest : BaseLobbyGameDefinitionPacket
{
    public new const short OpCode = 1;

    public LobbyGameDefinitionPacketDefinitionsRequest() : base(OpCode)
    {
    }
}