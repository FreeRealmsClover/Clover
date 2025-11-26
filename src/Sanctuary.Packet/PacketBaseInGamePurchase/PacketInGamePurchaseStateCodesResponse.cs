using System.Collections.Generic;

using Sanctuary.Core.IO;
using Sanctuary.Packet.Common.GameCommerce;

namespace Sanctuary.Packet;

public class PacketInGamePurchaseStateCodesResponse : PacketBaseInGamePurchase, ISerializablePacket
{
    public new const short OpCode = 19;

    public int ErrorCode;

    public List<StateCode> States = [];

    public PacketInGamePurchaseStateCodesResponse() : base(OpCode)
    {
    }

    public byte[] Serialize()
    {
        using var writer = new PacketWriter();

        Write(writer);

        writer.Write(ErrorCode);

        if (ErrorCode == 1)
            writer.Write(States);

        return writer.Buffer;
    }
}