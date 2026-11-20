using Amplitude;
using Amplitude.Mercury.Interop;
using Amplitude.Mercury.Simulation;

namespace DevConsole.Cheats
{
    /// <summary>World content placed on, or removed from, a tile: curiosities, dungeons, observatories, burrows,
    /// coral, the Tormented, village spawn points, rivers, roads and curiosity paths.</summary>
    internal static class Places
    {
        public static void CreateCuriosity(string definition, int tile) =>
            Orders.Post(new EditorOrderCreateCuriosityAt { CuriosityDefinitionName = new StaticString(definition), TileIndex = tile },
                        $"curiosity {definition} at tile {tile}");

        public static void RemoveCollectible(int tile) =>
            Orders.Post(new EditorOrderRemoveCollectibleAt { TileIndex = tile }, $"remove collectible at tile {tile}");

        public static void SpawnDungeon(int tile, int danger) =>
            Orders.Post(new EditorOrderSpawnDungeonOrSpawnPointAt { TileIndex = tile, DangerLevel = danger },
                        $"dungeon (danger {danger}) at tile {tile}");

        public static void RemoveDungeon(int tile) =>
            Orders.Post(new EditorOrderRemoveDungeonOrSpawnPointAt { TileIndex = tile }, $"remove dungeon at tile {tile}");

        public static void SpawnObservatory(int tile) =>
            Orders.Post(new EditorOrderSpawnObservatoryAt { TileIndex = tile, OwnerEmpireIndex = Sim.LocalEmpireIndex },
                        $"observatory at tile {tile}");

        public static void EvolveObservatory(int tile, ObservatoryType type) =>
            Orders.Post(new EditorOrderEvolveObservatoryAt { TileIndex = tile, OwnerEmpireIndex = Sim.LocalEmpireIndex, EvolveTo = type },
                        $"evolve observatory at tile {tile} to {type}");

        public static void RemoveObservatory(int tile) =>
            Orders.Post(new EditorOrderRemoveObservatoryAt { TileIndex = tile }, $"remove observatory at tile {tile}");

        public static void SpawnBurrow(int tile) =>
            Orders.Post(new EditorOrderSpawnBurrowAt { TileIndex = tile, OwnerEmpireIndex = Sim.LocalEmpireIndex }, $"burrow at tile {tile}");

        public static void RemoveBurrow(int tile) =>
            Orders.Post(new EditorOrderRemoveBurrowAt { TileIndex = tile }, $"remove burrow at tile {tile}");

        public static void CreateCoralSpore(int tile) =>
            Orders.Post(new EditorOrderCreateCoralSporeAt { EmpireIndex = Sim.LocalEmpireIndex, TileIndex = tile }, $"coral spore at tile {tile}");

        public static void CreateTormented(int tile, string armyType, bool isTormented, bool onlyTormented) =>
            Orders.Post(new EditorOrderCreateTormentedAt
            {
                TileIndex = tile,
                TormentedArmyTypeDefinitionName = armyType,
                IsTormented = isTormented,
                OnlyTormented = onlyTormented,
            }, $"Tormented {armyType} at tile {tile}");

        public static void FillTormentedGauge(int settlementTile) =>
            Orders.Post(new EditorOrderFillTormentedGaugeAt { SettlementTileIndex = settlementTile }, $"fill Tormented gauge at tile {settlementTile}");

        /// <summary>The order names the Tormented by GUID; it is whatever army or settlement stands on the tile.</summary>
        public static void ToggleTormentedSettle(int tile, bool settled)
        {
            var target = Sim.ArmyAt(tile) ?? Sim.SettlementAt(tile);
            if (target == null)
            {
                Orders.Log.LogWarning($"Tormented settle: nothing stands on tile {tile}");
                return;
            }
            Orders.Post(new EditorOrderToggleTormentedSettle { TormentedGUID = Sim.GuidOf(target), ToggleSettledState = settled },
                        $"Tormented at tile {tile} {(settled ? "settle" : "unsettle")}");
        }

        public static void AddVillageSpawnPoint(int tile, string minorFaction) =>
            Orders.Post(new EditorOrderCreateVillageSpawnLocationAt { TileIndex = tile, MinorFactionName = minorFaction },
                        $"{minorFaction} village spawn point at tile {tile}");

        public static void RemoveVillageSpawnPoint(int tile) =>
            Orders.Post(new EditorOrderRemoveVillageSpawnLocationAt { TileIndex = tile }, $"remove village spawn point at tile {tile}");

        public static void RefreshRiver(int tile) =>
            Orders.Post(new EditorOrderRefreshRiverAt { TileIndex = tile }, $"refresh river at tile {tile}");

        public static void RemoveDistrict(int tile) =>
            Orders.Post(new EditorOrderRemoveDistrictAt { TileIndex = tile }, $"remove district at tile {tile}");

        public static void SetRoadQuality(Amplitude.Mercury.Terrain.RoadData.RoadQuality quality) =>
            Orders.Post(new EditorOrderSetRoadQuality { RoadQuality = quality, OwnerEmpireIndex = Sim.LocalEmpireIndex },
                        $"road quality = {quality}");

        public static void ForceRoadUpdate() => Orders.Post(new EditorOrderForceRoadUpdate(), "force road update");

        public static void AddPath() => Orders.Post(new EditorOrderPathCuriosityAddPath(), "add curiosity path");

        public static void RemovePath(int pathIndex) =>
            Orders.Post(new EditorOrderPathCuriosityRemovePath { PathIndex = pathIndex }, $"remove curiosity path #{pathIndex}");

        public static void AddPathCuriosity(int pathIndex, string definition, int tile) =>
            Orders.Post(new EditorOrderPathCuriosityAdd { PathCuriosityIndex = pathIndex, TileIndex = tile, CuriosityDefinition = new StaticString(definition) },
                        $"path #{pathIndex} curiosity {definition} at tile {tile}");

        public static void RemovePathCuriosity(int tile) =>
            Orders.Post(new EditorOrderPathCuriosityRemove { TileIndex = tile }, $"remove path curiosity at tile {tile}");
    }
}
