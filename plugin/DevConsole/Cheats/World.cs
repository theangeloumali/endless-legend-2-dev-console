using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Mercury.Interop;
using Amplitude.Mercury.Presentation;
using Amplitude.Mercury.Simulation;
using HarmonyLib;

namespace DevConsole.Cheats
{
    /// <summary>
    /// Map editing. The tile under the cursor comes from the presentation layer, which is public, so everything
    /// here targets whatever the player is pointing at rather than asking for a tile number.
    /// </summary>
    internal static class World
    {
        /// <summary>Tile index the mouse is over; -1 when the cursor is off the map.</summary>
        public static int HoveredTile
        {
            get
            {
                var cursor = Presentation.PresentationCursorController;
                return cursor != null && cursor.CurrentHighlightedPositionValidity ? cursor.CurrentHighlightedPosition : -1;
            }
        }

        public static bool HasTile => HoveredTile >= 0;

        public sealed class ArmyEntry
        {
            public string Name;
            public SimulationEntityGUID Guid;

            public override string ToString() => Name;
        }

        public static List<ArmyEntry> Armies() =>
            Sim.Armies().Select(army => new ArmyEntry { Name = Sim.NameOf(army, "army"), Guid = Sim.GuidOf(army) }).ToList();

        public static void SpawnArmy(string unitDefinition, int tileIndex) =>
            Orders.Post(new EditorOrderCreateArmyAt
            {
                EmpireIndex = Sim.LocalEmpireIndex,
                UnitDefinitionName = new StaticString(unitDefinition),
                TileIndex = tileIndex,
            }, $"spawn {unitDefinition} at tile {tileIndex}");

        public static void CreateCity(int tileIndex) =>
            Orders.Post(new EditorOrderCreateCityAt { EmpireIndex = Sim.LocalEmpireIndex, CityTileIndex = tileIndex },
                        $"create city at tile {tileIndex}");

        public static void CreateCamp(int tileIndex) =>
            Orders.Post(new EditorOrderCreateCampAt { EmpireIndex = Sim.LocalEmpireIndex, CampTileIndex = tileIndex },
                        $"create camp at tile {tileIndex}");

        public static void Teleport(ArmyEntry army, int tileIndex) =>
            Orders.Post(new EditorOrderTeleportArmy { ArmyGUID = army.Guid, TileIndex = tileIndex, EmpireIndex = Sim.LocalEmpireIndex },
                        $"teleport {army.Name} to tile {tileIndex}");

        public static void SetSpeed(ArmyEntry army, int speed) =>
            Orders.Post(new EditorOrderSetGodSpeed { ArmyGUID = army.Guid, Speed = speed }, $"{army.Name} speed = {speed}");

        public static void CollectCuriosities() =>
            Orders.Post(new EditorOrderCollectAllCuriosities { MajorEmpireIndex = Sim.LocalEmpireIndex }, "collect all curiosities");

        /// <summary>Reveal is a single order carrying every tile index; the world size tells us how many there are.</summary>
        public static void RevealMap()
        {
            var tiles = TileCount;
            if (tiles <= 0)
            {
                return;
            }
            Orders.Post(new EditorOrderSetExploration
            {
                EmpireIndex = Sim.LocalEmpireIndex,
                TileIndexes = Enumerable.Range(0, tiles).ToArray(),
                Set = true,
            }, $"reveal {tiles} tiles");
        }

        private static int TileCount
        {
            get
            {
                var provider = Traverse.Create(typeof(Presentation)).Property("WorldMapProvider").GetValue();
                if (provider == null)
                {
                    return 0;
                }
                var traverse = Traverse.Create(provider);
                return traverse.Property<int>("MapWidth").Value * traverse.Property<int>("MapHeight").Value;
            }
        }
    }
}
