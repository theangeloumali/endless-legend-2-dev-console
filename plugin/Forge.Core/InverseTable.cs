using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Forge.Core
{
    /// <summary>Undo is only offered where the game has an opposite command; everything else returns false.</summary>
    public static class InverseTable
    {
        private sealed class Pair
        {
            public string Undo;
            public string FromTile;
            public string ToTile;
        }

        private static readonly Dictionary<string, Pair> Pairs = new Dictionary<string, Pair>
        {
            ["EditorOrderCreateArmyAt"] = Make("EditorOrderDestroyArmy", ("TileIndex", "ArmyTileIndex")),
            ["EditorOrderCreateCityAt"] = Make("EditorOrderDestroySettlement", ("CityTileIndex", "SettlementTileIndex")),
            ["EditorOrderCreateCampAt"] = Make("EditorOrderDestroySettlement", ("CampTileIndex", "SettlementTileIndex")),
            ["EditorOrderCreateVillageAt"] = Make("EditorOrderDestroyVillageAt", ("TileIndex", "TileIndex")),
            ["EditorOrderCreateDistrictAt"] = Make("EditorOrderRemoveDistrictAt", ("TileIndex", "TileIndex")),
            ["EditorOrderSpawnObservatoryAt"] = Make("EditorOrderRemoveObservatoryAt", ("TileIndex", "TileIndex")),
            ["EditorOrderSpawnBurrowAt"] = Make("EditorOrderRemoveBurrowAt", ("TileIndex", "TileIndex")),
            ["EditorOrderSpawnDungeonOrSpawnPointAt"] = Make("EditorOrderRemoveDungeonOrSpawnPointAt", ("TileIndex", "TileIndex")),
            ["EditorOrderCreateVillageSpawnLocationAt"] = Make("EditorOrderRemoveVillageSpawnLocationAt", ("TileIndex", "TileIndex")),
            ["EditorOrderCreateCuriosityAt"] = Make("EditorOrderRemoveCollectibleAt", ("TileIndex", "TileIndex")),
            ["EditorOrderPlantForest"] = Make("EditorOrderCutForest", ("DestinationTileIndex", "DestinationTileIndex")),
            ["EditorOrderPathCuriosityAdd"] = Make("EditorOrderPathCuriosityRemove", ("TileIndex", "TileIndex")),
        };

        /// <summary>Stock setters undo to themselves with the value read just before the edit was posted.</summary>
        private static readonly Dictionary<string, string> StockSetters = new Dictionary<string, string>
        {
            ["EditorOrderSetMoneyStock"] = "MoneyStock",
            ["EditorOrderSetInfluenceStock"] = "InfluenceStock",
        };

        /// <summary>The field whose pre-edit value undo needs, or null: callers read only what an order overwrites.</summary>
        public static string PreviousValueField(string orderType) =>
            orderType != null && StockSetters.TryGetValue(orderType, out var field) ? field : null;

        /// <summary>previousValue answers "what was this field before the edit?" for stock setters; null means unknown.</summary>
        public static bool TryInvert(OrderRecord done, Func<string, int?> previousValue, out OrderRecord undo)
        {
            undo = null;
            if (done?.Type == null)
            {
                return false;
            }
            if (StockSetters.TryGetValue(done.Type, out var stockField))
            {
                var before = previousValue?.Invoke(stockField);
                if (before == null)
                {
                    return false;
                }
                undo = done.Clone();
                undo.Scalars[stockField] = new JValue(before.Value);
                return true;
            }
            if (!Pairs.TryGetValue(done.Type, out var pair))
            {
                return false;
            }
            if (!done.Tiles.TryGetValue(pair.FromTile, out var tile))
            {
                return false;
            }
            undo = new OrderRecord { Type = pair.Undo, LocalEmpireFields = new HashSet<string>(done.LocalEmpireFields) };
            undo.Tiles[pair.ToTile] = tile;
            return true;
        }

        private static Pair Make(string undo, (string From, string To) tile) =>
            new Pair { Undo = undo, FromTile = tile.From, ToTile = tile.To };
    }
}
