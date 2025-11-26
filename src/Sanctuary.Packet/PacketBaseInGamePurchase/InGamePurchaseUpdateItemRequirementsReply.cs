using System.Collections.Generic;

using Sanctuary.Core.IO;

namespace Sanctuary.Packet;

public class InGamePurchaseUpdateItemRequirementsReply : PacketBaseInGamePurchase, ISerializablePacket
{
    public new const short OpCode = 39;

    public List<UpdateData> ItemRequirements = [];

    public class UpdateData : ISerializableType
    {
        public int BundleId;

        public bool CanPurchase;

        public void Serialize(PacketWriter writer)
        {
            writer.Write(BundleId);

            writer.Write(CanPurchase);
        }
    }

    public InGamePurchaseUpdateItemRequirementsReply() : base(OpCode)
    {
    }

    public byte[] Serialize()
    {
        using var writer = new PacketWriter();

        Write(writer);

        writer.Write(ItemRequirements);

        return writer.Buffer;
    }
}