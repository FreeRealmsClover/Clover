using Sanctuary.Core.IO;

namespace Sanctuary.Packet.Common;

public class FriendStatus : ISerializableType
{
    /// <summary>
    /// 110 - Online
    /// 111 - Offline
    /// </summary>
    public int Status;

    public int CurrentMiniGameType;
    public int CurrentMiniGameNameId;

    public int ProfileId;
    public int ProfileNameId;
    public int ProfileIconId;
    public int ProfileBackgroundImageId;
    public int ProfileRank;

    public void Serialize(PacketWriter writer)
    {
        writer.Write(Status);

        writer.Write(CurrentMiniGameType);
        writer.Write(CurrentMiniGameNameId);

        writer.Write(ProfileId);
        writer.Write(ProfileNameId);
        writer.Write(ProfileIconId);
        writer.Write(ProfileBackgroundImageId);
        writer.Write(ProfileRank);
    }
}