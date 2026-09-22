using Sanctuary.Game.Entities;
using Sanctuary.Packet;

namespace Sanctuary.Game;

public interface ITradeManager
{
    bool CanTrade(Player requester, Player target);

    void RequestTrade(Player requester, Player target);

    void HandleClientCommand(Player player, TradeCommand command);

    void OnZoning(Player player);

    void OnDisconnected(Player player);
}
