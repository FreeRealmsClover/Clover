using Sanctuary.Game.Entities;
using Sanctuary.Packet.Common;

namespace Sanctuary.Game.Interactions;

/// <summary>
/// Starts an ordinary player-to-player inventory trade.
/// </summary>
public sealed class TradeInteraction(ITradeManager tradeManager) : IInteraction
{
    public int Id => Data.Id;

    public static InteractionData Data = new()
    {
        Id = IInteraction.UniqueId++,
        IconId = 43188,
        ButtonText = 3299
    };

    public void OnInteract(Player player, IEntity other)
    {
        if (other is Player target)
            tradeManager.RequestTrade(player, target);
    }
}
