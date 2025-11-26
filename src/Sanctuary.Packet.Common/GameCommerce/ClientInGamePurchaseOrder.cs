using System.Collections.Generic;

using Sanctuary.Core.IO;

namespace Sanctuary.Packet.Common.GameCommerce;

public class ClientInGamePurchaseOrder : BaseInGamePurchaseOrder
{
    public List<BaseInGamePurchaseOrderDetail> Details = [];

    public override bool TryRead(ref PacketReader reader)
    {
        if (!base.TryRead(ref reader))
            return false;

        if (!reader.TryReadList(out Details))
            return false;

        return true;
    }
}