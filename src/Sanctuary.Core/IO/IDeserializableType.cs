namespace Sanctuary.Core.IO;

public interface IDeserializableType
{
    bool TryRead(ref PacketReader reader);
}