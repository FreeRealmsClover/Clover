using Sanctuary.Core.IO;

namespace Sanctuary.Packet.Common;

public class ActionBarData : ISerializableType, IDeserializableType
{
    public int Id;
    public int Slot;

    public void Serialize(PacketWriter writer)
    {
        writer.Write(Id);
        writer.Write(Slot);
    }

    public bool TryRead(ref PacketReader reader)
    {
        if (!reader.TryRead(out Id))
            return false;

        if (!reader.TryRead(out Slot))
            return false;

        return true;
    }
}