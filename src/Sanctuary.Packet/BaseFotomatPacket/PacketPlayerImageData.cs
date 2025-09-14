using System;

using Sanctuary.Core.IO;
using Sanctuary.Packet.Common;

namespace Sanctuary.Packet;

public class PacketPlayerImageData : BaseFotomatPacket, ISerializablePacket
{
    public new const byte OpCode = 3;

    public ulong Guid;

    public string? Provider;

    public bool Compressed;

    public FotomatPortrait Portrait = new();

    public byte[] PngPayload = Array.Empty<byte>();

    public PacketPlayerImageData() : base(OpCode)
    {
    }

    public byte[] Serialize()
    {
        using var writer = new PacketWriter();

        Write(writer);

        writer.Write(Guid);

        writer.Write(Provider);

        writer.Write(Compressed);

        Portrait.Serialize(writer);

        writer.WritePayload(PngPayload);

        return writer.Buffer;
    }
}