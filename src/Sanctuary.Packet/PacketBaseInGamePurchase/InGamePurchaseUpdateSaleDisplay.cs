using System.Collections.Generic;

using Sanctuary.Core.IO;
using Sanctuary.Packet.Common;

namespace Sanctuary.Packet;

public class InGamePurchaseUpdateSaleDisplay : PacketBaseInGamePurchase, ISerializablePacket
{
    public new const short OpCode = 40;

    public List<SaleDisplayInfo> Sales = new();

    public InGamePurchaseUpdateSaleDisplay() : base(OpCode)
    {
    }

    public byte[] Serialize()
    {
        using var writer = new PacketWriter();

        Write(writer);

        writer.Write(Sales);

        return writer.Buffer;
    }
}