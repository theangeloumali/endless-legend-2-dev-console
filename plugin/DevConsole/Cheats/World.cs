using System;
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
        /// <summary>Tile index the mouse is over right now; -1 when the cursor is off the map — which includes
        /// every moment the mouse is over the console window.</summary>
        public static int HoveredTile
        {
            get
            {
                var cursor = Presentation.PresentationCursorController;
                return cursor != null && cursor.CurrentHighlightedPositionValidity ? cursor.CurrentHighlightedPosition : -1;
            }
        }

        /// <summary>The tile actions actually use. Reading the live hover at click time never worked: moving the
        /// mouse onto a button takes it off the map, so the target has to be latched while the map is hovered.
        /// Locking freezes it so panning or brushing the map cannot move the target out from under you.</summary>
        public static int TargetTile { get; private set; } = -1;

        public static bool Locked { get; set; }

        public static bool HasTile => TargetTile >= 0;

        /// <summary>The armed tool, if any: press a plot button to arm it, then click the map as often as you
        /// like. It stays armed until you cancel, which is the whole point — placing ten districts should be ten
        /// map clicks, not ten trips back to the window.</summary>
        public static string ArmedLabel { get; private set; }

        private static Action<int> armed;

        public static bool IsArmed => armed != null;

        public static void Arm(string label, Action<int> action)
        {
            ArmedLabel = label;
            armed = action;
            Orders.Log.LogInfo($"armed: {label} — click the map to place, right-click or Escape to cancel");
        }

        public static void Disarm()
        {
            if (armed == null)
            {
                return;
            }
            Orders.Log.LogInfo($"disarmed: {ArmedLabel}");
            ArmedLabel = null;
            armed = null;
        }

        /// <summary>Fired by a map click. Stays armed so the next click places another. The live hover wins, but
        /// falls back to the latched tile: the cursor controller reports nothing in some camera and UI states, and
        /// silently doing nothing was impossible to diagnose.</summary>
        public static void PlaceAt(int hovered)
        {
            if (armed == null)
            {
                return;
            }
            var tile = hovered >= 0 ? hovered : TargetTile;
            if (tile < 0)
            {
                Orders.Log.LogWarning($"{ArmedLabel}: click ignored — no tile resolved (hover={hovered}, latched={TargetTile})");
                return;
            }
            Orders.Log.LogInfo($"placing {ArmedLabel} at tile {tile} (hover={hovered}, latched={TargetTile})");
            armed(tile);
        }

        /// <summary>Called once per frame; the last valid hover wins unless the target is locked.</summary>
        public static void Track()
        {
            if (Locked)
            {
                return;
            }
            var hovered = HoveredTile;
            if (hovered >= 0)
            {
                TargetTile = hovered;
            }
        }

        public sealed class ArmyEntry
        {
            public string Name;
            public SimulationEntityGUID Guid;
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

        public static void CreateDistrict(string definitionName, int tileIndex) =>
            Orders.Post(new EditorOrderCreateDistrictAt { DistrictDefinitionName = new StaticString(definitionName), TileIndex = tileIndex },
                        $"district {definitionName} at tile {tileIndex}");

        public static void CreateWonder(string definitionName, int tileIndex) =>
            Orders.Post(new EditorOrderCreateArtificialWonderAt { ArtificialWonderDefinitionName = new StaticString(definitionName), TileIndex = tileIndex },
                        $"wonder {definitionName} at tile {tileIndex}");

        public static void PlantForest(int tileIndex) =>
            Orders.Post(new EditorOrderPlantForest { DestinationTileIndex = tileIndex }, $"plant forest at tile {tileIndex}");

        public static void CutForest(int tileIndex) =>
            Orders.Post(new EditorOrderCutForest { DestinationTileIndex = tileIndex, EmpireIndex = Sim.LocalEmpireIndex },
                        $"cut forest at tile {tileIndex}");

        public static void ClearMountain(int tileIndex) =>
            Orders.Post(new EditorOrderClearMountain { DestinationTileIndex = tileIndex }, $"clear mountain at tile {tileIndex}");

        public static void BuildBridge(int tileIndex) =>
            Orders.Post(new EditorOrderBuildBridge { DestinationTileIndex = tileIndex, EmpireIndex = Sim.LocalEmpireIndex },
                        $"bridge at tile {tileIndex}");

        public static void BuildDam(int tileIndex) =>
            Orders.Post(new EditorOrderBuildDam { DestinationTileIndex = tileIndex, EmpireIndex = Sim.LocalEmpireIndex },
                        $"dam at tile {tileIndex}");

        public static void RaiseSandRuin(int tileIndex) =>
            Orders.Post(new EditorOrderRaiseSandRuinAt { EmpireIndex = Sim.LocalEmpireIndex, TileIndex = tileIndex },
                        $"raise sand ruin at tile {tileIndex}");

        public static void CreateVillage(int tileIndex) =>
            Orders.Post(new EditorOrderCreateVillageAt { TileIndex = tileIndex }, $"village at tile {tileIndex}");

        public static void DestroyVillage(int tileIndex) =>
            Orders.Post(new EditorOrderDestroyVillageAt { TileIndex = tileIndex }, $"destroy village at tile {tileIndex}");

        public static void PacifyVillage(int tileIndex) =>
            Orders.Post(new EditorOrderPacifyVillageAt { MajorEmpireIndex = Sim.LocalEmpireIndex, TileIndex = tileIndex },
                        $"pacify village at tile {tileIndex}");

        public static void SpawnFromVillage(int tileIndex) =>
            Orders.Post(new EditorOrderSpawnFromVillage { VillageTileIndex = tileIndex }, $"spawn from village at tile {tileIndex}");

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
