using System;
using System.Numerics;

using Sanctuary.Core.IO;
using Sanctuary.Packet.Common;

namespace Sanctuary.Packet;

public class AbilityPacketClientRequestStartAbility : BaseAbilityPacket, IDeserializable<AbilityPacketClientRequestStartAbility>
{
    public new const short OpCode = 10;

    public ActionBarData Data = new();
    public ulong Guid;
    public int Target;
    public Vector4 Position;

    public AbilityPacketClientRequestStartAbility() : base(OpCode)
    {
    }

    public static bool TryDeserialize(ReadOnlySpan<byte> data, out AbilityPacketClientRequestStartAbility value)
    {
        value = new AbilityPacketClientRequestStartAbility();

        var reader = new PacketReader(data);

        if (!value.TryRead(ref reader))
            return false;

        if (!value.Data.TryRead(ref reader))
            return false;

        if (!reader.TryRead(out value.Target))
            return false;

        if (value.Target == 1)
        {
            if (!reader.TryRead(out value.Position))
                return false;
        }
        else
        {
            if (!reader.TryRead(out value.Guid))
                return false;
        }

        return reader.RemainingLength == 0;
    }
}