using System;

using Sanctuary.Core.IO;

namespace Sanctuary.Core.Cryptography;

public sealed class CipherRC4 : ICipher
{
    private RC4? _encryptRC4;
    private RC4? _decryptRC4;

    public bool IsInitialized { get; private set; }

    public bool Initialize(byte[] key)
    {
        _encryptRC4 = new RC4(key);
        _decryptRC4 = new RC4(key);

        IsInitialized = true;

        return true;
    }

    public int GetFinalLength(int length)
    {
        return length;
    }

    public bool Decrypt(Span<byte> data, out int finalLength)
    {
        if (_decryptRC4 is null)
        {
            finalLength = 0;
            return false;
        }

        _decryptRC4.Apply(data, data);

        finalLength = data.Length;

        return true;
    }

    public bool Encrypt(Span<byte> data, PacketWriter writer)
    {
        if (_encryptRC4 is null)
            return false;

        _encryptRC4.Apply(data, data);

        writer.Write(data);

        return true;
    }
}