using Amplitude;
using Amplitude.Mercury.Interop;
using Amplitude.Mercury.Simulation;

namespace DevConsole.Cheats
{
    /// <summary>Settlement editing for any empire: identity, territory, production queue, population, foundations.</summary>
    internal static class Settlements
    {
        public static void Destroy(Build.Site site) =>
            Orders.Post(new EditorOrderDestroySettlement { SettlementTileIndex = site.TileIndex }, $"destroy {site.Name}");

        public static void SetCapital(Build.Site site) =>
            Orders.Post(new EditorOrderSetCapital { CityTileIndex = site.TileIndex }, $"make {site.Name} the capital");

        public static void Rename(Build.Site site, string name) =>
            Orders.Post(new EditorOrderRenameSettlement { SettlementTileIndex = site.TileIndex, SettlementName = name },
                        $"rename {site.Name} to {name}");

        public static void MergeInto(Build.Site city, int mergedTile) =>
            Orders.Post(new EditorOrderMergeSettlementIntoCity { CityTileIndex = city.TileIndex, SettlementToMergeTileIndex = mergedTile },
                        $"merge settlement at tile {mergedTile} into {city.Name}");

        // the game's field really is spelled "Adiministrative"
        public static void DetachTerritory(Build.Site city, int adminCenterTile) =>
            Orders.Post(new EditorOrderDetachTerritoryFromCity { CityTileIndex = city.TileIndex, AdiministrativeCenterToDetachTileIndex = adminCenterTile },
                        $"detach territory at tile {adminCenterTile} from {city.Name}");

        public static void EvolveCampToCity(Build.Site camp) =>
            Orders.Post(new EditorOrderEvolveCampToCity { CampTileIndex = camp.TileIndex }, $"turn camp {camp.Name} into a city");

        /// <summary>Districts need a placement tile; units and improvements are queued at the settlement itself.</summary>
        public static void Enqueue(Build.Site site, string constructible, int constructionTile, EnqueuePosition position) =>
            Orders.Post(new EditorOrderEnqueueConstructible
            {
                SettlementTileIndex = site.TileIndex,
                ConstructibleName = new StaticString(constructible),
                ConstructionTileIndex = constructionTile,
                EnqueuePosition = position,
            }, $"{site.Name}: enqueue {constructible} at tile {constructionTile}");

        public static void RemoveConstruction(Build.Site site, int index) =>
            Orders.Post(new EditorOrderRemoveConstructionAt { SettlementTileIndex = site.TileIndex, ConstructionIndex = index },
                        $"{site.Name}: remove queue entry {index + 1}");

        public static void ChangePopulation(Build.Site site, string populationName) =>
            Orders.Post(new EditorOrderChangeSelectedPopulationToCalledPopulation
            {
                SettlementTileIndex = site.TileIndex,
                PopulationName = new StaticString(populationName),
            }, $"{site.Name}: change selected population to {populationName}");

        public static void BuildFoundation(Build.Site site, int tile) =>
            Orders.Post(new EditorOrderBuildFoundationAt
            {
                SettlementGUID = site.Guid,
                TileIndex = tile,
                EmpireIndex = Sim.EmpireIndexOf(Sim.SettlementAt(site.TileIndex)) is int owner && owner >= 0 ? owner : Sim.LocalEmpireIndex,
            }, $"{site.Name}: foundation at tile {tile}");
    }
}
