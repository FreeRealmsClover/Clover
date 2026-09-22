using System;

using Sanctuary.Core.IO;
using Sanctuary.Packet.Common;

namespace Sanctuary.Packet;

public static class BaseTradePacket
{
    public const short OpCode = 59;

    public enum SubOpCode : byte
    {
        Invite = 2,
        InviteReply = 3,
        StartSession = 4,
        EndSession = 5,
        Accept = 6,
        Cancel = 7,
        Lock = 8,
        OfferItem = 9,
        CoinCount = 10,
        UpdateCoinCount = 11,
        UpdateAddItem = 12,
        UpdateItemCount = 13,
        UpdateAccept = 14,
        Message = 15,
        Rejected = 16,
        UpdateLock = 17,
        Confirm = 18,
        ItemRemoved = 19
    }

    public static bool TryDeserialize(ReadOnlySpan<byte> data, out TradeCommand? command)
    {
        command = null;

        var reader = new PacketReader(data);

        if (!reader.TryRead(out short opCode) || opCode != OpCode)
            return false;

        if (!reader.TryRead(out byte subOpCode))
            return false;

        switch ((SubOpCode)subOpCode)
        {
            case SubOpCode.InviteReply:
                if (!reader.TryRead(out int inviteSessionId) ||
                    !TryReadBoolean(ref reader, out bool accepted))
                    return false;

                command = new TradeInviteReplyCommand(inviteSessionId, accepted);
                break;

            case SubOpCode.Accept:
                if (!reader.TryRead(out int acceptSessionId) ||
                    !reader.TryRead(out int acceptRevision))
                    return false;

                command = new TradeAcceptCommand(acceptSessionId, acceptRevision);
                break;

            case SubOpCode.Cancel:
                if (!reader.TryRead(out int cancelSessionId))
                    return false;

                command = new TradeCancelCommand(cancelSessionId);
                break;

            case SubOpCode.Lock:
                if (!reader.TryRead(out int lockSessionId) ||
                    !reader.TryRead(out int lockRevision) ||
                    !TryReadBoolean(ref reader, out bool locked))
                    return false;

                command = new TradeLockCommand(lockSessionId, lockRevision, locked);
                break;

            case SubOpCode.OfferItem:
                if (!reader.TryRead(out int offerSessionId) ||
                    !reader.TryRead(out int itemGuid) ||
                    !reader.TryRead(out int resultingCount))
                    return false;

                command = new TradeOfferItemCommand(offerSessionId, itemGuid, resultingCount);
                break;

            case SubOpCode.CoinCount:
                if (!reader.TryRead(out int coinSessionId) ||
                    !reader.TryRead(out int resultingCoins))
                    return false;

                command = new TradeCoinCountCommand(coinSessionId, resultingCoins);
                break;

            default:
                return false;
        }

        if (reader.RemainingLength != 0)
        {
            command = null;
            return false;
        }

        return true;
    }

    public static byte[] CreateInvite(NameData inviter, ulong inviterGuid, int sessionId)
    {
        ArgumentNullException.ThrowIfNull(inviter);

        return Serialize(SubOpCode.Invite, writer =>
        {
            inviter.Serialize(writer);
            writer.Write(inviterGuid);
            writer.Write(sessionId);
        });
    }

    public static byte[] CreateStartSession(ulong partnerGuid, NameData partner, int sessionId)
    {
        ArgumentNullException.ThrowIfNull(partner);

        return Serialize(SubOpCode.StartSession, writer =>
        {
            writer.Write(partnerGuid);
            partner.Serialize(writer);
            writer.Write(sessionId);
        });
    }

    public static byte[] CreateEndSession(int sessionId)
    {
        return Serialize(SubOpCode.EndSession, writer => writer.Write(sessionId));
    }

    public static byte[] CreateUpdateCoinCount(int sessionId, int revision, bool isSelf, int coins)
    {
        return Serialize(SubOpCode.UpdateCoinCount, writer =>
        {
            writer.Write(sessionId);
            writer.Write(revision);
            writer.Write(isSelf);
            writer.Write(coins);
        });
    }

    public static byte[] CreateUpdateAddItem(int sessionId, int revision, bool isSelf, ItemInstance item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return Serialize(SubOpCode.UpdateAddItem, writer =>
        {
            writer.Write(sessionId);
            writer.Write(revision);
            writer.Write(isSelf);
            item.Serialize(writer);
        });
    }

    public static byte[] CreateUpdateItemCount(int sessionId, int revision, bool isSelf, int itemId, int count)
    {
        return Serialize(SubOpCode.UpdateItemCount, writer =>
        {
            writer.Write(sessionId);
            writer.Write(revision);
            writer.Write(isSelf);
            writer.Write(itemId);
            writer.Write(count);
        });
    }

    public static byte[] CreateUpdateAccept(int sessionId)
    {
        return Serialize(SubOpCode.UpdateAccept, writer => writer.Write(sessionId));
    }

    public static byte[] CreateMessage(byte code)
    {
        return Serialize(SubOpCode.Message, writer => writer.Write(code));
    }

    public static byte[] CreateRejected(NameData name)
    {
        ArgumentNullException.ThrowIfNull(name);

        return Serialize(SubOpCode.Rejected, name.Serialize);
    }

    public static byte[] CreateUpdateLock(int sessionId, bool locked)
    {
        return Serialize(SubOpCode.UpdateLock, writer =>
        {
            writer.Write(sessionId);
            writer.Write(locked);
        });
    }

    public static byte[] CreateConfirm(int sessionId)
    {
        return Serialize(SubOpCode.Confirm, writer => writer.Write(sessionId));
    }

    public static byte[] CreateItemRemoved(int sessionId, int revision, int itemId)
    {
        return Serialize(SubOpCode.ItemRemoved, writer =>
        {
            writer.Write(sessionId);
            writer.Write(revision);
            writer.Write(itemId);
        });
    }

    private static bool TryReadBoolean(ref PacketReader reader, out bool value)
    {
        value = false;

        if (!reader.TryRead(out byte byteValue) || byteValue > 1)
            return false;

        value = byteValue == 1;
        return true;
    }

    private static byte[] Serialize(SubOpCode subOpCode, Action<PacketWriter> writePayload)
    {
        using var writer = new PacketWriter();

        writer.Write(OpCode);
        writer.Write((byte)subOpCode);
        writePayload(writer);

        return writer.Buffer;
    }
}

public abstract record TradeCommand(int SessionId);

public sealed record TradeInviteReplyCommand(int SessionId, bool Accepted) : TradeCommand(SessionId);

public sealed record TradeAcceptCommand(int SessionId, int Revision) : TradeCommand(SessionId);

public sealed record TradeCancelCommand(int SessionId) : TradeCommand(SessionId);

public sealed record TradeLockCommand(int SessionId, int Revision, bool Locked) : TradeCommand(SessionId);

public sealed record TradeOfferItemCommand(int SessionId, int ItemGuid, int ResultingCount) : TradeCommand(SessionId);

public sealed record TradeCoinCountCommand(int SessionId, int ResultingCoins) : TradeCommand(SessionId);
