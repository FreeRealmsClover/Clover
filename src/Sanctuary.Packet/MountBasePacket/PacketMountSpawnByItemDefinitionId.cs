using System;

using Sanctuary.Core.IO;

namespace Sanctuary.Packet;

public class PacketMountSpawnByItemDefinitionId : MountBasePacket, IDeserializable<PacketMountSpawnByItemDefinitionId>
{
    public new const byte OpCode = 8;

    public int ItemDefinitionId;

    public bool Unknown;

    public PacketMountSpawnByItemDefinitionId() : base(OpCode)
    {
    }

    public static bool TryDeserialize(ReadOnlySpan<byte> data, out PacketMountSpawnByItemDefinitionId value)
    {
        value = new PacketMountSpawnByItemDefinitionId();

        var reader = new PacketReader(data);

        if (!value.TryRead(ref reader))
            return false;

        if (!reader.TryRead(out value.ItemDefinitionId))
            return false;

        if (!reader.TryRead(out value.Unknown))
            return false;

        return reader.RemainingLength == 0;
    }
}