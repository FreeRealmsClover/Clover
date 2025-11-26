using System;
using System.Buffers.Binary;

using Sanctuary.Core.Cryptography;

namespace Sanctuary.Core.IO;

public class PacketUtils
{
    private const uint Magic = 0xF3D4A5C9;

    public static bool WrapPacket(Span<byte> data, PacketWriter writer, bool encrypt, ICipher? cipher)
    {
        if (encrypt)
        {
            if (cipher is null || !cipher.IsInitialized)
                return false;

            var encryptedDataLength = data.Length + cipher.GetFinalLength(data.Length);

            if (!cipher.Encrypt(data, writer))
                return false;

            writer.Write(encrypt);
            writer.Write(Magic);

            return true;
        }

        if (data.Length < 5)
            return true;

        var magicData = data[^4..];

        if (!BinaryPrimitives.TryReadUInt32LittleEndian(magicData, out uint magic) || magic != Magic)
            return true;

        writer.Write(data);
        writer.Write(encrypt);
        writer.Write(Magic);

        return true;
    }

    public static bool UnwrapPacket(Span<byte> data, out int finalLength, ICipher? cipher)
    {
        if (data.Length < 5)
        {
            finalLength = data.Length;
            return true;
        }

        var magicData = data[^4..];

        if (!BinaryPrimitives.TryReadUInt32LittleEndian(magicData, out uint magic) || magic != Magic)
        {
            finalLength = data.Length;
            return true;
        }

        var isEncrypted = data[^5];

        if (isEncrypted == 0)
        {
            finalLength = data.Length - 5;
            return true;
        }

        if (isEncrypted != 1)
        {
            finalLength = 0;
            return false;
        }

        if (cipher is null || !cipher.IsInitialized)
        {
            finalLength = 0;
            return false;
        }

        var encryptedData = data[..^5];

        if (!cipher.Decrypt(encryptedData, out var decryptedLength))
        {
            finalLength = 0;
            return false;
        }

        finalLength = decryptedLength;

        return true;
    }
}