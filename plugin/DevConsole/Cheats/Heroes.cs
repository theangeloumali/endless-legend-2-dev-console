using System.Collections.Generic;
using Amplitude;
using Amplitude.Mercury.Interop;
using Amplitude.Framework.Simulation;
using Amplitude.Mercury.Simulation;
using HarmonyLib;

namespace DevConsole.Cheats
{
    /// <summary>
    /// Hero roster and the orders that act on it. Every hero order takes a HeroIndex that indexes the GLOBAL
    /// hero list, not our roster, so the index always comes from Hero.Index and never from a loop counter.
    /// </summary>
    internal static class Heroes
    {
        /// <summary>HeroStatistics.Count — the delta array must be exactly this long or the order is rejected.</summary>
        public const int StatisticCount = 4;

        /// <summary>The game's enum calls index 3 "Dexterity" but the processor writes AddedCharisma.</summary>
        public static readonly string[] StatisticNames = { "Strength", "Intellect", "Constitution", "Charisma" };

        public sealed class Entry
        {
            public int Index;          // global HeroIndex, what the orders want
            public string Name;
            public int Level;
            public int SkillPoints;
            public object Handle;      // the internal Hero instance

            public string Label => $"{Name} — lvl {Level}, {SkillPoints} pts";
        }

        public static List<Entry> Roster()
        {
            var roster = new List<Entry>();
            foreach (var hero in Sim.Heroes())
            {
                var traverse = Traverse.Create(hero);
                roster.Add(new Entry
                {
                    Index = traverse.Field<int>("Index").Value,
                    Name = Sim.NameOf(hero, "hero"),
                    Level = PropertyValue(hero, "Level"),
                    SkillPoints = PropertyValue(hero, "SkillPoint"),
                    Handle = hero,
                });
            }
            return roster;
        }

        /// <summary>Hero stats are Property/EditableProperty objects wrapping a FixedPoint.</summary>
        private static int PropertyValue(object hero, string name)
        {
            var property = Traverse.Create(hero).Field(name).GetValue();
            return property == null ? 0 : (int)Traverse.Create(property).Property<FixedPoint>("Value").Value;
        }

        /// <summary>Stat increases are PAID FOR out of SkillPoint, so top it up first. No order grants points, so
        /// this is the one direct write we make. EditableProperty is a STRUCT: reading it through reflection hands
        /// back a boxed copy, so the mutated value has to be written back or the hero keeps its old total.</summary>
        public static void GiveSkillPoints(Entry hero, int points)
        {
            var field = AccessTools.Field(hero.Handle?.GetType(), "SkillPoint");
            if (field == null)
            {
                Orders.Log.LogWarning($"Hero.SkillPoint not found; cannot give points to {hero.Name}.");
                return;
            }
            var property = (EditableProperty)field.GetValue(hero.Handle);
            property.Value = (FixedPoint)(hero.SkillPoints + points);
            field.SetValue(hero.Handle, property);
            Orders.Log.LogInfo($"{hero.Name}: skill points {hero.SkillPoints} -> {hero.SkillPoints + points}");
            Refresh(hero);
        }

        /// <summary>A direct write updates the simulation but the hero panel only repaints when a simulation event
        /// reaches the presentation layer, so the new total looks missing until something else happens. Posting a
        /// zero-experience order is the cheapest round-trip that forces that repaint without changing anything.</summary>
        private static void Refresh(Entry hero)
        {
            var unit = Traverse.Create(hero.Handle).Field("HeroUnit").Property("Entity").GetValue();
            if (unit == null)
            {
                return;
            }
            var collection = Traverse.Create(unit).Field("UnitCollection").Property("Entity").GetValue();
            Orders.Post(new OrderChangeUnitsXP(Sim.GuidOf(collection), new[] { Sim.GuidOf(unit) }, new[] { 0 }),
                        $"refresh {hero.Name}");
        }

        public static void IncreaseStatistics(Entry hero, uint[] deltas)
        {
            if (deltas.Length != StatisticCount)
            {
                return;
            }
            Orders.Post(new OrderHeroStatisticIncrease(hero.Index, deltas), $"stats +{string.Join("/", deltas)} on {hero.Name}");
        }

        public static void GiveExperience(Entry hero, int amount)
        {
            var unit = Traverse.Create(hero.Handle).Field("HeroUnit").Property("Entity").GetValue();
            if (unit == null)
            {
                return;
            }
            var collection = Traverse.Create(unit).Field("UnitCollection").Property("Entity").GetValue();
            Orders.Post(new OrderChangeUnitsXP(Sim.GuidOf(collection), new[] { Sim.GuidOf(unit) }, new[] { amount }),
                        $"+{amount} XP to {hero.Name}");
        }

        public static void Heal(Entry hero) => Orders.Post(new OrderHeroHeal { HeroIndex = hero.Index }, $"heal {hero.Name}");

        public static void Dismiss(Entry hero) => Orders.Post(new OrderHeroDismiss { HeroIndex = hero.Index }, $"dismiss {hero.Name}");

        public static void CreateDraw(int count, int minLevel, int maxLevel) =>
            Orders.Post(new EditorOrderForceCreateHeroDraw
            {
                MajorEmpireIndex = Sim.LocalEmpireIndex,
                NumberOfHeroes = count,
                MinLevel = minLevel,
                MaxLevel = maxLevel,
            }, $"draw {count} heroes at level {minLevel}-{maxLevel}");

        /// <summary>The draw is indexed separately from the roster; recruiting walks it from the top.</summary>
        public static void RecruitDraw(int count)
        {
            for (var i = 0; i < count; i++)
            {
                Orders.Post(new OrderHeroRecruitInDraw { HeroIndex = i }, $"recruit draw #{i}");
            }
        }

        public static void RecruitMarketplace(int count)
        {
            for (var i = 0; i < count; i++)
            {
                Orders.Post(new OrderHeroRecruitInMarketplace { HeroIndex = i }, $"recruit market #{i}");
            }
        }

        public static void Spawn(string heroDefinition, int tileIndex) =>
            Orders.Post(new EditorOrderForceHeroSpawn
            {
                HeroDefinition = new StaticString(heroDefinition),
                EmpireIndex = Sim.LocalEmpireIndex,
                TileIndex = tileIndex,
            }, $"spawn {heroDefinition} at tile {tileIndex}");
    }
}
