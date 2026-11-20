using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Amplitude;
using Amplitude.Mercury.Interop;
using Amplitude.Mercury.Simulation;
using Forge.Core;
using HarmonyLib;

namespace DevConsole
{
    /// <summary>
    /// Map-level reads for Forge: what stands on a tile, where an entity is, the map's size and layout. Every
    /// member is resolved by name and degrades to "unknown" (null or -1) on a build where it moved.
    /// </summary>
    internal static partial class Sim
    {
        private static readonly Type WorldPositionType = AccessTools.TypeByName("Amplitude.Mercury.WorldPosition");
        private static readonly MethodInfo TryGetArmyAtMethod = TileLookup("TryGetArmyAt");
        private static readonly MethodInfo TryGetSettlementAtMethod = TileLookup("TryGetSettlementAt");

        public static int Turn => Sandbox is object sandbox ? Traverse.Create(sandbox).Field<int>("turn").Value : -1;

        public static int MapWidth => StaticInt("WorldWidth");

        public static int MapHeight => StaticInt("WorldHeight");

        public static bool MapWraps => WorldPositionType != null && Traverse.Create(WorldPositionType).Field("WorldWrap").GetValue() is bool wraps && wraps;

        private static object World => Sandbox is object sandbox ? Traverse.Create(sandbox).Field("World").GetValue() : null;

        public static object ArmyAt(int tile) => AtTile(TryGetArmyAtMethod, tile);

        public static object SettlementAt(int tile) => AtTile(TryGetSettlementAtMethod, tile);

        public static int EmpireIndexOf(object entity)
        {
            if (entity == null)
            {
                return -1;
            }
            var value = Traverse.Create(entity).Property("EmpireIndex").GetValue();
            return value is int index ? index : -1;
        }

        /// <summary>WorldPosition is stored differently across entity types; try the shapes seen in this build.</summary>
        public static int TileOf(object entity)
        {
            var position = entity == null ? null : Traverse.Create(entity).Field("WorldPosition").GetValue();
            switch (position)
            {
                case null:
                    return -1;
                case int tile:
                    return tile;
            }
            var traverse = Traverse.Create(position);
            foreach (var name in new[] { "TileIndex", "Index" })
            {
                if (traverse.Field(name).GetValue() is int field)
                {
                    return field;
                }
                if (traverse.Property(name).GetValue() is int property)
                {
                    return property;
                }
            }
            var convert = WorldPositionType == null ? null : AccessTools.Method(WorldPositionType, "GetTileIndexFromWorldPosition", new[] { position.GetType() });
            return convert?.Invoke(null, new[] { position }) is int converted ? converted : -1;
        }

        /// <summary>The local empire's capital tile, or -1 before the capital is founded.</summary>
        public static int CapitalTile()
        {
            var capital = LocalEmpire == null ? null : Traverse.Create(LocalEmpire).Field("Capital").GetValue();
            if (capital == null)
            {
                return -1;
            }
            var entity = Traverse.Create(capital).Property("Entity").GetValue() ?? capital;  // a reference wrapper or the settlement
            return TileOf(entity);
        }

        public static int MoneyStock => StockOf("MoneyStock");

        public static int InfluenceStock => StockOf("InfluenceStock");

        public sealed class UnitRow
        {
            public string Name;
            public SimulationEntityGUID Guid;
            public int Health;
            public int MaxHealth;
        }

        public static List<UnitRow> UnitsOf(object army) =>
            Collection(army, "Units").Select(unit =>
            {
                var traverse = Traverse.Create(unit);
                var health = traverse.Field("HealthPoints").GetValue();
                var ratio = traverse.Field("HealthRatio").GetValue();
                var current = health is FixedPoint points ? (int)points : 0;
                var fraction = ratio is FixedPoint r ? (float)(int)(r * 1000) / 1000f : 1f;
                var definition = traverse.Field("UnitDefinition").GetValue();
                var name = definition == null ? "unit" : Traverse.Create(definition).Property("Name").GetValue()?.ToString() ?? "unit";
                return new UnitRow
                {
                    Name = Catalog.Units.FirstOrDefault(entry => entry.Name == name)?.Display ?? name,
                    Guid = GuidOf(unit),
                    Health = current,
                    MaxHealth = fraction > 0f ? (int)Math.Round(current / fraction) : current,
                };
            }).ToList();

        /// <summary>The game's own axial coordinate for a tile; recipes use it to tell which of the four offset
        /// layouts this map follows instead of assuming one.</summary>
        public static (int Q, int R)? GameAxial(int tile)
        {
            var method = WorldPositionType == null ? null : AccessTools.Method(WorldPositionType, "TileIndexToAxialCoord", new[] { typeof(int) });
            var coord = method?.Invoke(null, new object[] { tile });
            if (coord == null)
            {
                return null;
            }
            var ints = coord.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance)
                            .Where(field => field.FieldType == typeof(int)).Select(field => (int)field.GetValue(coord)).ToArray();
            return ints.Length >= 2 ? (ints[0], ints[1]) : ((int, int)?)null;
        }

        /// <summary>Picks the layout whose axial maths agrees with the game's on probe tiles; null when none does.</summary>
        public static HexGrid? Grid()
        {
            int width = MapWidth, height = MapHeight;
            if (width <= 2 || height <= 3)
            {
                return null;
            }
            var probes = new[] { 2 * width, 2 * width + 1, 3 * width + 2, width + 3 };
            foreach (HexLayout layout in Enum.GetValues(typeof(HexLayout)))
            {
                var grid = new HexGrid(width, height, layout, MapWraps);
                if (probes.All(tile => GameAxial(tile) is (int q, int r) && grid.ToAxial(tile % width, tile / width) == (q, r)))
                {
                    return grid;
                }
            }
            return null;
        }

        private static MethodInfo TileLookup(string name)
        {
            var world = Type("World");
            return world?.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .FirstOrDefault(method => method.Name == name
                                                  && method.GetParameters().Length == 2
                                                  && method.GetParameters()[0].ParameterType == typeof(int)
                                                  && method.GetParameters()[1].IsOut);
        }

        private static object AtTile(MethodInfo lookup, int tile)
        {
            var world = World;
            if (lookup == null || world == null || tile < 0)
            {
                return null;
            }
            var arguments = new object[] { tile, null };
            return lookup.Invoke(world, arguments) is bool found && found ? arguments[1] : null;
        }

        private static int StaticInt(string field) =>
            WorldPositionType != null && Traverse.Create(WorldPositionType).Field(field).GetValue() is int value ? value : 0;

        private static int StockOf(string field)
        {
            var value = LocalEmpire == null ? null : Traverse.Create(LocalEmpire).Field(field).GetValue();
            switch (value)
            {
                case FixedPoint stock:
                    return (int)stock;
                case int whole:
                    return whole;
                default:
                    return -1;
            }
        }
    }
}
