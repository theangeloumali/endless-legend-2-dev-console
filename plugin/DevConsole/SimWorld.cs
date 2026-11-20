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
        private static readonly MethodInfo TileToAxial =
            WorldPositionType == null ? null : AccessTools.Method(WorldPositionType, "TileIndexToAxialCoord", new[] { typeof(int) });

        public static int Turn => Sandbox is object sandbox ? Traverse.Create(sandbox).Field<int>("turn").Value : -1;

        /// <summary>The presentation's map provider: the source the Reveal Map button has always used.</summary>
        public static int MapWidth => MapProperty("MapWidth");

        public static int MapHeight => MapProperty("MapHeight");

        public static bool MapWraps => WorldPositionType != null && Traverse.Create(WorldPositionType).Field("WorldWrap").GetValue() is bool wraps && wraps;

        private static object World => Sandbox is object sandbox ? Traverse.Create(sandbox).Field("World").GetValue() : null;

        public static object ArmyAt(int tile) => AtTile(TryGetArmyAtMethod, tile);

        public static object SettlementAt(int tile) => AtTile(TryGetSettlementAtMethod, tile);

        /// <summary>Burrows and dungeons are not in the world's tile lookups; the sandbox keeps each in one list.</summary>
        public static object BurrowAt(int tile) => RepositoryItemAt("BurrowRepository", "Burrows", tile);

        public static object DungeonAt(int tile) => RepositoryItemAt("DungeonRepository", "Dungeons", tile);

        public sealed class EmpireRow
        {
            public int Index;
            public string Name;
            public object Entity;
        }

        /// <summary>Major empires only: the ones that can be picked as a diplomacy or story target.</summary>
        public static List<EmpireRow> MajorEmpires()
        {
            var empires = Sandbox is object sandbox ? Traverse.Create(sandbox).Field("MajorEmpires").GetValue() as System.Collections.IEnumerable : null;
            return empires?.Cast<object>().Where(empire => empire != null).Select(empire =>
            {
                var index = Traverse.Create(empire).Field("Index").GetValue() is int i ? i : EmpireIndexOf(empire);
                var name = NameOf(empire, null);
                return new EmpireRow { Index = index, Entity = empire, Name = $"#{index}{(name == null ? string.Empty : "  " + name)}{(IsLocal(index) ? "  (you)" : string.Empty)}" };
            }).ToList() ?? new List<EmpireRow>();
        }

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
                var current = PropertyValue(unit, "HealthPoints") is FixedPoint points ? (int)points : 0;
                var fraction = PropertyValue(unit, "HealthRatio") is FixedPoint r ? (float)(int)(r * 1000) / 1000f : 1f;
                var name = DefinitionName(unit, "UnitDefinition", "unit");
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
            var coord = TileToAxial?.Invoke(null, new object[] { tile });
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

        /// <summary>The Name of a definition an entity points at (UnitDefinition, ConstructibleDefinition…).</summary>
        public static string DefinitionName(object entity, string definitionField, string fallback)
        {
            var definition = entity == null ? null : Traverse.Create(entity).Field(definitionField).GetValue();
            return definition == null ? fallback : Traverse.Create(definition).Property("Name").GetValue()?.ToString() ?? fallback;
        }

        private static object RepositoryItemAt(string repository, string list, int tile)
        {
            var items = Sandbox is object sandbox ? Traverse.Create(sandbox).Field(repository).Field(list).GetValue() : null;
            return (items as System.Collections.IEnumerable)?.Cast<object>()
                .FirstOrDefault(item => Traverse.Create(item).Field("TileIndex").GetValue() is int at && at == tile);
        }

        private static int MapProperty(string property)
        {
            var provider = Traverse.Create(typeof(Amplitude.Mercury.Presentation.Presentation)).Property("WorldMapProvider").GetValue();
            return provider == null ? 0 : Traverse.Create(provider).Property<int>(property).Value;
        }

        /// <summary>A FixedPoint or int stock on the local empire (MoneyStock, InfluenceStock); -1 when unreadable.</summary>
        public static int StockOf(string field) => (int?)PropertyValue(LocalEmpire, field) ?? -1;

        /// <summary>Simulation numbers are Property/EditableProperty wrappers around a FixedPoint (stocks, health);
        /// interop snapshots hold the bare FixedPoint. Null when the field is missing or not a number.</summary>
        public static FixedPoint? PropertyValue(object entity, string field)
        {
            var value = entity == null ? null : Traverse.Create(entity).Field(field).GetValue();
            switch (value)
            {
                case null:
                    return null;
                case FixedPoint fixedPoint:
                    return fixedPoint;
                case int whole:
                    return (FixedPoint)whole;
            }
            return Traverse.Create(value).Property("Value").GetValue() is FixedPoint wrapped ? wrapped : (FixedPoint?)null;
        }

        /// <summary>Identifies the running game, so an edit-log entry from a previous game is never undone in this one.</summary>
        public static int Session => Sandbox is object sandbox ? System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(sandbox) : 0;
    }
}
