using System.Collections.Generic;

using Sanctuary.Core.IO;

namespace Sanctuary.Packet;

public class PacketInGamePurchaseStoreBundleContentResponse : PacketBaseInGamePurchase, ISerializablePacket
{
    public new const short OpCode = 28;

    public List<ResponseData> Responses = [];

    public class ResponseData : ISerializableType
    {
        public int StoreId;
        public int BundleId;

        public void Serialize(PacketWriter writer)
        {
            writer.Write(StoreId);
            writer.Write(BundleId);
        }
    }

    public PacketInGamePurchaseStoreBundleContentResponse() : base(OpCode)
    {
    }

    public byte[] Serialize()
    {
        using var writer = new PacketWriter();

        Write(writer);

        writer.Write(Responses);

        return writer.Buffer;
    }
}