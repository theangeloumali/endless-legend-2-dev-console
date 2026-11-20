using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Mercury.Data.Simulation;
using Amplitude.Mercury.Interop;
using Amplitude.Mercury.Simulation;
using HarmonyLib;

namespace DevConsole.Cheats
{
    /// <summary>Levers over other empires and the story: army missions, moods, dialogs, dungeons, heroes,
    /// minor factions, empire state, council seats and the tech queue.</summary>
    internal static class AiStory
    {
        public static void SetArmyMission(Units.ArmyPick army, ArmyMissionTypes mission, int targetTile) =>
            _ = Units.StillThere(army) && Orders.Post(new EditorOrderSetArmyMission
            {
                ArmyTileIndex = army.Tile,
                ArmyMissionType = mission,
                TargetTileIndex = targetTile,
                TargetIndex = -1,
                TargetGuid = 0,
            }, $"{army.Name}: mission {mission} → tile {targetTile}");

        /// <summary>The attacker is the army's owner, not the player: this is how an AI army is sent at a city.</summary>
        public static void AttackCity(Units.ArmyPick army, int cityTile) =>
            _ = Units.StillThere(army) && Orders.Post(new EditorOrderAttackCity { ArmyTileIndex = army.Tile, CityTileIndex = cityTile, EmpireIndex = army.Owner },
                        $"{army.Name} (empire #{army.Owner}) attacks the city at tile {cityTile}");

        public static void MoodMessage(int source, int target, string mood) =>
            Orders.Post(new EditorOrderForceMoodMessage { SourceEmpireIndex = source, TargetEmpireIndex = target, MoodMessageName = new StaticString(mood) },
                        $"mood {mood}: #{source} → #{target}");

        public static void ClearMoodCooldown(int source, int target, bool clearExcluded) =>
            Orders.Post(new EditorOrderForceClearMoodMessageDeadzone { SourceEmpireIndex = source, TargetEmpireIndex = target, ClearExcludeListToo = clearExcluded },
                        $"clear mood cooldown #{source} → #{target}");

        public static void StartDialog(string dialog, int empire) =>
            Orders.Post(new EditorOrderForceStartDialog { DialogName = new StaticString(dialog), EmpireIndex = empire }, $"start dialog {dialog} for #{empire}");

        public static void ClearDungeon(int tile) => WithDungeon(tile, guid =>
            Orders.Post(new EditorOrderForceDungeonClear { DungeonGUID = guid }, $"clear dungeon at tile {tile}"));

        public static void RegenerateDungeon(int tile, int seed) => WithDungeon(tile, guid =>
            Orders.Post(new EditorOrderForceDungeonContentGeneration { DungeonGUID = guid, Seed = seed }, $"regenerate dungeon at tile {tile} (seed {seed})"));

        // no separate crawl entity exists on this build, so the dungeon's own GUID names the crawl
        public static void ContinueCrawl(int tile) => WithDungeon(tile, guid =>
            Orders.Post(new EditorOrderContinueDungeonCrawl { DungeonCrawlGUID = guid }, $"continue dungeon crawl at tile {tile}"));

        public static void DeployHero(int heroIndex, int empire, int tile) =>
            Orders.Post(new EditorOrderForceHeroDeployAt { HeroIndex = heroIndex, EmpireIndex = empire, TileIndex = tile },
                        $"deploy hero #{heroIndex} of empire #{empire} at tile {tile}");

        public static void DismissHero(int heroIndex, int empire) =>
            Orders.Post(new EditorOrderForceHeroDismiss { HeroIndex = heroIndex, EmpireIndex = empire }, $"dismiss hero #{heroIndex} of empire #{empire}");

        public static void RedistributeHeroes() => Orders.Post(new EditorOrderForceHeroDistribution(), "redistribute heroes");

        public static void PartnerDen(int empire, object hero, int tile) =>
            Orders.Post(new EditorOrderBuildPartnerDen { EmpireIndex = empire, HeroGUID = (ulong)Sim.GuidOf(hero), TileIndex = tile },
                        $"partner den for {Sim.NameOf(hero, "hero")} at tile {tile}");

        public static void ReplaceMinorTrait(int minorFaction, int traitIndex, bool cohabitation) =>
            Orders.Post(new EditorOrderReplaceMinorFactionTrait
            {
                MinorFactionIndex = minorFaction,
                ReplacedTraitIndex = traitIndex,
                ReplaceCohabitationTraitInsteadOfAssimilationTrait = cohabitation,
            }, $"minor faction #{minorFaction}: replace {(cohabitation ? "cohabitation" : "assimilation")} trait {traitIndex}");

        public static void SetEmpireState(string definition, int empire) =>
            Orders.Post(new EditorOrderSetEmpireStateDefinition { EmpireStateDefinitionName = new StaticString(definition), OwnerEmpireIndex = empire },
                        $"#{empire}: empire state {definition}");

        public static void ReadyEmpireAction(EmpireActionType action, int empire) =>
            Orders.Post(new EditorOrderSetEmpireActionProjectReady { EmpireActionType = action, EmpireIndex = empire }, $"#{empire}: {action} ready");

        public static void BypassSgeLevel(int empire, BypassSGELevelType level) =>
            Orders.Post(new EditorOrderBypassSGELevel { EmpireIndex = empire, BypassSGELevel = level }, $"#{empire}: bypass SGE level {level}");

        public static void EnqueueTechnology(int empire, string technology, EnqueuePosition position) =>
            Orders.Post(new EditorOrderEnqueueTechnology { EmpireIndex = empire, TechnologyName = new StaticString(technology), EnqueuePosition = position },
                        $"#{empire}: enqueue {technology}");

        public static void ClearTechQueue(int empire) =>
            Orders.Post(new EditorOrderClearTechnologyQueue { EmpireIndex = empire }, $"#{empire}: clear technology queue");

        public sealed class SeatRow
        {
            public string Name;
            public bool Locked;
            public SimulationEntityGUID Guid;
        }

        public static List<SeatRow> Seats(object empire) =>
            Sim.Collection(empire, "CouncilSeats").Select(seat =>
            {
                return new SeatRow
                {
                    Name = Sim.DefinitionName(seat, "CouncilSeatDefinition", "seat"),
                    Locked = Traverse.Create(seat).Field("IsLocked").GetValue() is bool locked && locked,
                    Guid = Sim.GuidOf(seat),
                };
            }).ToList();

        public static void UnlockSeat(SeatRow seat, int empire) =>
            Orders.Post(new EditorOrderUnlockSeat { SeatGUID = seat.Guid, EmpireIndex = empire }, $"#{empire}: unlock council seat {seat.Name}");

        private static void WithDungeon(int tile, System.Action<SimulationEntityGUID> post)
        {
            var dungeon = Sim.DungeonAt(tile);
            if (dungeon == null)
            {
                Orders.Log.LogWarning($"no dungeon on tile {tile}");
                return;
            }
            post(Sim.GuidOf(dungeon));
        }
    }
}
