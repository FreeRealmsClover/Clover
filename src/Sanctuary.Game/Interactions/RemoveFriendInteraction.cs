using System.Linq;

using Microsoft.EntityFrameworkCore;

using Sanctuary.Database;
using Sanctuary.Game.Entities;
using Sanctuary.Packet;
using Sanctuary.Packet.Common;

namespace Sanctuary.Game.Interactions;

public class RemoveFriendInteraction : IInteraction
{
    private readonly IDbContextFactory<DatabaseContext> _dbContextFactory;

    public int Id => Data.Id;

    public static InteractionData Data = new()
    {
        Id = IInteraction.UniqueId++,
        IconId = 135,
        ButtonText = 3371
    };

    public RemoveFriendInteraction(IDbContextFactory<DatabaseContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public void OnInteract(Player player, IEntity other)
    {
        if (other is not Player otherPlayer)
            return;

        using var dbContext = _dbContextFactory.CreateDbContext();

        var dbFriendsToRemove = dbContext.Friends
            .Where(x => (x.CharacterGuid == otherPlayer.Guid &&
                        x.FriendCharacterGuid == player.Guid) ||
                        (x.FriendCharacterGuid == otherPlayer.Guid &&
                        x.CharacterGuid == player.Guid));

        if (dbFriendsToRemove.ExecuteDelete() <= 0)
            return;

        player.Friends.RemoveAll(x => x.Guid == otherPlayer.Guid);

        player.SendTunneled(new FriendRemovePacket
        {
            Guid = otherPlayer.Guid
        });

        otherPlayer.Friends.RemoveAll(x => x.Guid == player.Guid);

        otherPlayer.SendTunneled(new FriendRemovePacket
        {
            Guid = player.Guid
        });
    }
}