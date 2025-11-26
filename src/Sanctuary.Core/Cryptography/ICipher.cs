using System;

using Sanctuary.Core.IO;

namespace Sanctuary.Core.Cryptography;

public interface ICipher
{
    bool IsInitialized { get; }

    bool Initialize(byte[] key);

    int GetFinalLength(int length);

    bool Decrypt(Span<byte> data, out int finalLength);
    bool Encrypt(Span<byte> data, PacketWriter writer);
}