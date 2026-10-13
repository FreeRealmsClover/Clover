using System;
using System.Linq;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Sanctuary.Core.Helpers;
using Sanctuary.Core.IO;
using Sanctuary.Database;
using Sanctuary.Database.Entities;
using Sanctuary.Game;
using Sanctuary.Packet;
using Sanctuary.Packet.Common;
using Sanctuary.Packet.Common.Attributes;

namespace Sanctuary.Gateway.Handlers;

[PacketHandler]
public static class CoinStoreSellToClientRequestPacketHandler
{
    private static ILogger _logger = null!;
    private static IResourceManager _resourceManager = null!;
    private static IDbContextFactory<DatabaseContext> _dbContextFactory = null!;

    public static void ConfigureServices(IServiceProvider serviceProvider)
    {
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        _logger = loggerFactory.CreateLogger(nameof(CoinStoreSellToClientRequestPacketHandler));

        _resourceManager = serviceProvider.GetRequiredService<IResourceManager>();
        _dbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<DatabaseContext>>();
    }

    public static bool HandlePacket(GatewayConnection connection, ReadOnlySpan<byte> data)
    {
        if (!CoinStoreSellToClientRequestPacket.TryDeserialize(data, out var packet))
        {
            _logger.LogError("Failed to deserialize {packet}.", nameof(CoinStoreSellToClientRequestPacket));
            return false;
        }

        _logger.LogTrace("Received {name} packet. ( {packet} )", nameof(CoinStoreSellToClientRequestPacket), packet);

        var coinStoreTransactionCompletePacket = new CoinStoreTransactionCompletePacket
        {
            TransactionRecord =
            {
                Type = 1 // Buy
            }
        };

        // CloverEconomyGuard: quantity must be positive and sane, otherwise a modified client
        // can send a negative amount and flip the coin/item math.
        if (packet.Quantity <= 0 || packet.Quantity > 9999)
        {
            _logger.LogWarning("Rejected coin store buy with invalid quantity {quantity} from {guid}", packet.Quantity, connection.Player.Guid);

            coinStoreTransactionCompletePacket.Result = 8;

            connection.SendTunneled(coinStoreTransactionCompletePacket);

            return true;
        }

        if (!_resourceManager.ClientItemDefinitions.TryGetValue(packet.ItemRecord.Definition, out var clientItemDefinition))
        {
            _logger.LogWarning("User tried to buy unknown item definition. {guid} {definition}", connection.Player.Guid, packet.ItemRecord.Definition);

            coinStoreTransactionCompletePacket.Result = 3;

            connection.SendTunneled(coinStoreTransactionCompletePacket);

            return true;
        }

        // TODO: Implement other item types
        if (clientItemDefinition.Type != 1 &&
            clientItemDefinition.Type != 3 &&
            clientItemDefinition.Type != 12)
        {
            coinStoreTransactionCompletePacket.Result = 8;

            connection.SendTunneled(coinStoreTransactionCompletePacket);

            return true;
        }

        var cost = connection.Player.MembershipStatus == 0
            ? clientItemDefinition.Cost
            : clientItemDefinition.GetMemberPurchasePrice();

        // CloverEconomyGuard: do the multiply in 64 bits so a huge quantity cannot wrap to a negative cost.
        var totalCostLong = (long)cost * packet.Quantity;

        if (cost < 0 || totalCostLong > int.MaxValue)
        {
            coinStoreTransactionCompletePacket.Result = 7;

            connection.SendTunneled(coinStoreTransactionCompletePacket);

            return true;
        }

        var totalCost = (int)totalCostLong;

        if (connection.Player.Coins < totalCost)
        {
            coinStoreTransactionCompletePacket.Result = 7;

            connection.SendTunneled(coinStoreTransactionCompletePacket);

            return true;
        }

        var tint = packet.ItemRecord.Tint;

        if (!clientItemDefinition.IsTintable)
            tint = clientItemDefinition.Icon.TintId;

        using var dbContext = _dbContextFactory.CreateDbContext();

        var dbQuery = dbContext.Characters
            .Where(x => x.Id == GuidHelper.GetPlayerId(connection.Player.Guid))
            .Select(x => new
            {
                Character = x,
                Item = x.Items.SingleOrDefault(i => i.Definition == clientItemDefinition.Id && i.Tint == tint),
                NextId = x.Items.Select(i => (int?)i.Id).Max() ?? 0
            })
            .SingleOrDefault();

        if (dbQuery is null)
        {
            coinStoreTransactionCompletePacket.Result = 8;

            connection.SendTunneled(coinStoreTransactionCompletePacket);

            return true;
        }

        var dbItem = dbQuery.Item;

        if (dbItem is not null)
        {
            dbItem.Tint = tint;
            dbItem.Count += packet.Quantity;
        }
        else
        {
            dbItem = new DbItem
            {
                Id = dbQuery.NextId + 1,
                Definition = clientItemDefinition.Id,
                Tint = tint,

                Count = packet.Quantity
            };

            dbQuery.Character.Items.Add(dbItem);
        }

        dbQuery.Character.Coins -= totalCost;

        if (dbContext.SaveChanges() <= 0)
        {
            coinStoreTransactionCompletePacket.Result = 8;

            connection.SendTunneled(coinStoreTransactionCompletePacket);

            return true;
        }

        var clientItem = connection.Player.Items.SingleOrDefault(x => x.Definition == clientItemDefinition.Id && x.Tint == tint);

        var addItem = false;

        if (clientItem is not null)
        {
            clientItem.Tint = dbItem.Tint;
            clientItem.Count = dbItem.Count;
        }
        else
        {
            addItem = true;

            clientItem = new ClientItem
            {
                Id = dbItem.Id,
                Tint = dbItem.Tint,
                Count = dbItem.Count,
                Definition = dbItem.Definition
            };

            connection.Player.Items.Add(clientItem);
        }

        connection.Player.Coins = dbQuery.Character.Coins;

        if (addItem)
        {
            using var writer = new PacketWriter();

            clientItem.Serialize(writer);

            clientItemDefinition.Serialize(writer);

            var clientUpdatePacketItemAdd = new ClientUpdatePacketItemAdd();

            clientUpdatePacketItemAdd.Payload = writer.Buffer;

            connection.SendTunneled(clientUpdatePacketItemAdd);
        }
        else
        {
            var clientUpdatePacketItemUpdate = new ClientUpdatePacketItemUpdate
            {
                ItemGuid = clientItem.Id,
                Count = clientItem.Count,
            };

            connection.SendTunneled(clientUpdatePacketItemUpdate);
        }

        var clientUpdatePacketCoinCount = new ClientUpdatePacketCoinCount
        {
            Coins = connection.Player.Coins
        };

        connection.SendTunneled(clientUpdatePacketCoinCount);

        coinStoreTransactionCompletePacket.Result = 1;

        coinStoreTransactionCompletePacket.ItemGuid = clientItem.Id;

        coinStoreTransactionCompletePacket.TransactionRecord.Type = 1; // Buy

        coinStoreTransactionCompletePacket.TransactionRecord.Id = connection.Player.CoinStoreTransactions.Count + 1;

        coinStoreTransactionCompletePacket.TransactionRecord.Timestamp = DateTimeOffset.UtcNow;

        coinStoreTransactionCompletePacket.TransactionRecord.ItemRecord.Definition = clientItem.Definition;
        coinStoreTransactionCompletePacket.TransactionRecord.ItemRecord.Tint = clientItem.Tint;

        coinStoreTransactionCompletePacket.TransactionRecord.Quantity = packet.Quantity;

        global::Sanctuary.Game.Eye.Log("store-buy", connection.Player, new { item = clientItem.Definition, tint = clientItem.Tint, quantity = packet.Quantity, cost = totalCost });

        connection.SendTunneled(coinStoreTransactionCompletePacket);

        connection.Player.CoinStoreTransactions.Add(coinStoreTransactionCompletePacket.TransactionRecord);

        return true;
    }
}
