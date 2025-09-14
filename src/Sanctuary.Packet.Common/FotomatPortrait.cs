using System.Collections.Generic;

using Sanctuary.Core.IO;

namespace Sanctuary.Packet.Common;

public class FotomatPortrait : ISerializableType
{
    public int Unknown;
    public int Unknown2;

    public ulong Guid;

    public int ModelId;

    public List<CharacterAttachmentData> Attachments = [];

    public string? Head;
    public string? Hair;
    public string? SkinTone;
    public string? FacePaint;
    public string? ModelCustomization;

    public int HairColor;
    public int EyeColor;
    public int HeadId;
    public int HairId;
    public int SkinToneId;
    public int FacePaintId;

    public string? Provider;

    public void Serialize(PacketWriter writer)
    {
        writer.Write(Unknown);
        writer.Write(Unknown2);

        writer.Write(Guid);

        writer.Write(ModelId);

        writer.Write(Attachments);

        writer.Write(Head);
        writer.Write(Hair);

        writer.Write(HairColor);
        writer.Write(EyeColor);

        writer.Write(SkinTone);
        writer.Write(FacePaint);
        writer.Write(ModelCustomization);

        writer.Write(HeadId);
        writer.Write(HairId);
        writer.Write(SkinToneId);
        writer.Write(FacePaintId);

        writer.Write(Provider);
    }
}