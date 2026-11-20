using System.Linq;
using Amplitude;
using Amplitude.Mercury.Interop;
using Amplitude.Mercury.Simulation;

namespace DevConsole.Cheats
{
    /// <summary>Army and unit editing for any empire's army, AI included: picked by the tile it stands on.</summary>
    internal static class Units
    {
        public sealed class ArmyPick
        {
            public int Tile;
            public SimulationEntityGUID Guid;
            public int Owner;
            public string Name;
            public System.Collections.Generic.List<Sim.UnitRow> Units;
        }

        /// <summary>Null when no army stands on the tile.</summary>
        public static ArmyPick At(int tile)
        {
            var army = Sim.ArmyAt(tile);
            return army == null
                ? null
                : new ArmyPick
                {
                    Tile = tile,
                    Guid = Sim.GuidOf(army),
                    Owner = Sim.EmpireIndexOf(army),
                    Name = Sim.NameOf(army, "army"),
                    Units = Sim.UnitsOf(army),
                };
        }

        /// <summary>Tile-addressed orders hit whatever army stands there now; refuse if the picked one has moved.</summary>
        public static bool StillThere(ArmyPick army)
        {
            var now = Sim.ArmyAt(army.Tile);
            if (now != null && Sim.GuidOf(now).Equals(army.Guid))
            {
                return true;
            }
            Orders.Log.LogWarning($"{army.Name} is no longer on tile {army.Tile}: pick it again");
            return false;
        }

        public static void Add(ArmyPick army, string unitDefinition) =>
            _ = StillThere(army) && Orders.Post(new EditorOrderAddUnitToArmy { UnitDefinitionName = new StaticString(unitDefinition), ArmyTileIndex = army.Tile },
                        $"{army.Name}: add {unitDefinition}");

        /// <summary>The order takes a position, which shifts as units leave; it is looked up from the unit's GUID now.</summary>
        public static void Remove(ArmyPick army, Sim.UnitRow unit)
        {
            if (!StillThere(army))
            {
                return;
            }
            var index = Sim.UnitsOf(Sim.ArmyAt(army.Tile)).FindIndex(row => row.Guid.Equals(unit.Guid));
            if (index < 0)
            {
                Orders.Log.LogWarning($"{army.Name}: {unit.Name} is no longer in this army");
                return;
            }
            Orders.Post(new EditorOrderRemoveUnitFromArmy { ArmyTileIndex = army.Tile, UnitIndex = index }, $"{army.Name}: remove {unit.Name}");
        }

        /// <summary>An evolution is named by the unit definition it turns into.</summary>
        public static void Evolve(ArmyPick army, Sim.UnitRow unit, string evolution) =>
            Orders.Post(new EditorOrderEvolveUnit { UnitGUID = unit.Guid, EvolutionName = new StaticString(evolution) },
                        $"{army.Name}: evolve {unit.Name} into {evolution}");

        public static void Damage(ArmyPick army, int amount) =>
            Orders.Post(new EditorOrderDamageUnits(army.Guid, army.Units.Select(unit => unit.Guid).ToArray(), amount),
                        $"{army.Name}: {amount} damage to every unit");

        public static void Destroy(ArmyPick army) =>
            _ = StillThere(army) && Orders.Post(new EditorOrderDestroyArmy { ArmyTileIndex = army.Tile }, $"destroy {army.Name}");

        public static void SetMovementRatio(ArmyPick army, float ratio) =>
            _ = StillThere(army) && Orders.Post(new EditorOrderSetArmyMovementRatio { ArmyTileIndex = army.Tile, MovementRatio = ratio },
                        $"{army.Name}: movement ratio {ratio}");

        /// <summary>The army travels through the burrow network and comes out at the burrow on the clicked tile.</summary>
        public static void EnterBurrow(ArmyPick army, int exitTile)
        {
            var exit = Sim.BurrowAt(exitTile);
            if (exit == null)
            {
                Orders.Log.LogWarning($"no burrow on tile {exitTile}");
                return;
            }
            Orders.Post(new EditorOrderEnterBurrow
            {
                ArmyGUID = army.Guid,
                DestinationTileIndex = exitTile,
                ExitBurrowGUID = Sim.GuidOf(exit),
                StopAction = false,
            }, $"{army.Name}: travel by burrow to tile {exitTile}");
        }
    }
}
