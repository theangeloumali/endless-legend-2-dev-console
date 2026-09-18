using System;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Framework;
using Amplitude.Framework.Localization;
using Amplitude.Mercury.Data.Simulation;
using Amplitude.Mercury.UI;
using Amplitude.UI;
using DevConsole.Ui;
using UnityEngine;

namespace DevConsole
{
    /// <summary>
    /// Definition names for the dropdowns, read from the live datatables so a game patch or a data mod is picked
    /// up automatically. Each definition is paired with its UIMapper — which carries the player-facing title and
    /// icon — by element name, the convention the game's own data uses.
    /// </summary>
    internal static class Catalog
    {
        /// <summary>A definition as the UI shows it and as the orders need it.</summary>
        public sealed class Entry
        {
            public string Name;      // element name, what an order wants
            public string Display;   // localized title, falling back to Name
            public int Rank;         // higher is better: rarity level, unit tier, era
            public string Tier;      // the rank as the player knows it ("Legendary", "Era IV")
            public Color Tint = Theme.Text;

            public override string ToString() => Display;
        }

        private static readonly Dictionary<Type, Entry[]> Cache = new Dictionary<Type, Entry[]>();

        public static Entry[] Equipment => Entries<HeroEquipmentDefinition, HeroEquipmentUIMapper>(Rarity);
        public static Entry[] Heroes => Entries<HeroDefinition, HeroUnitUIMapper>(UnitTier);
        public static Entry[] Units => Entries<UnitDefinition, UnitUIMapper>(UnitTier);
        public static Entry[] Technologies => Entries<TechnologyDefinition, TechnologyUIMapper>(Era);
        public static Entry[] Districts => Entries<DistrictDefinition, ConstructibleUIMapper>(UnitTier);
        public static Entry[] Improvements => Entries<DistrictImprovementDefinition, ConstructibleUIMapper>(UnitTier);
        public static Entry[] Wonders => Filtered<DistrictDefinition, ConstructibleUIMapper, ArtificialWonderDefinition>();
        public static Entry[] Quests => Entries<QuestDefinition, UIMapper>(None);
        public static Entry[] Populations => Entries<PopulationDefinition, PopulationUIMapper>(None);
        public static Entry[] Statuses => Entries<StatusDefinition, StatusUIMapper>(None);

        /// <summary>Some definitions share a database with their base type — artificial wonders are stored as
        /// districts — so the list is the database filtered to the concrete type.</summary>
        private static Entry[] Filtered<TDefinition, TMapper, TWanted>()
            where TDefinition : class, IDatatableElement
            where TMapper : UIMapper
            where TWanted : TDefinition
        {
            if (Cache.TryGetValue(typeof(TWanted), out var cached))
            {
                return cached;
            }
            Entry[] entries;
            try
            {
                var database = Databases.GetDatabase<TDefinition>(false);
                entries = database == null
                    ? new Entry[0]
                    : database.OfType<TWanted>()
                              .Select(definition => Describe<TMapper>(definition.Name))
                              .OrderBy(entry => entry.Display, StringComparer.CurrentCultureIgnoreCase)
                              .ToArray();
            }
            catch (Exception)
            {
                entries = new Entry[0];
            }
            if (entries.Length > 0)
            {
                Cache[typeof(TWanted)] = entries;
            }
            return entries;
        }

        /// <summary>Catalogues with no meaningful ordering fall back to alphabetical.</summary>
        private static void None<T>(T definition, Entry entry)
        {
        }

        /// <summary>Rarity drives both the order and the colour, taken from the rarity's own UIMapper so the
        /// console matches whatever palette the game uses.</summary>
        private static void Rarity(HeroEquipmentDefinition definition, Entry entry)
        {
            var rarity = definition.EquipmentRarity.GetDatatableElement<HeroEquipmentRarityDefinition>();
            if (rarity == null)
            {
                return;
            }
            entry.Rank = rarity.RarityLevel;
            var mapper = Mapper<HeroEquipmentRarityUIMapper>(rarity.Name);
            entry.Tier = mapper == null ? null : Title(mapper);
            if (mapper != null)
            {
                entry.Tint = mapper.Color;
            }
        }

        private static void UnitTier(ConstructibleDefinition definition, Entry entry)
        {
            entry.Rank = definition.Level;
            entry.Tier = definition.Level > 0 ? "Tier " + definition.Level : null;
        }

        private static void Era(TechnologyDefinition definition, Entry entry)
        {
            var era = definition.EraReference.GetDatatableElement<EraDefinition>();
            if (era == null)
            {
                return;
            }
            entry.Rank = era.EraIndex;
            entry.Tier = "Era " + era.EraIndex;
        }

        /// <summary>Built on first use: the databases are not loaded while the plugin is constructed, and an empty
        /// result is never cached so the next call retries.</summary>
        /// <summary>Best first — rarest equipment, highest unit tier, latest era — then alphabetical, which is
        /// what you want when reaching for something powerful in a list of 177.</summary>
        private static Entry[] Entries<TDefinition, TMapper>(Action<TDefinition, Entry> rank)
            where TDefinition : class, IDatatableElement
            where TMapper : UIMapper
        {
            if (Cache.TryGetValue(typeof(TDefinition), out var cached))
            {
                return cached;
            }
            Entry[] entries;
            try
            {
                var definitions = Databases.GetDatabase<TDefinition>(false);
                entries = definitions == null ? new Entry[0] : definitions.Select(definition =>
                {
                    var entry = Describe<TMapper>(definition.Name);
                    try
                    {
                        rank(definition, entry);
                    }
                    catch (Exception)
                    {
                        // a definition with no rank simply sorts last
                    }
                    return entry;
                }).OrderByDescending(entry => entry.Rank)
                  .ThenBy(entry => entry.Display, StringComparer.CurrentCultureIgnoreCase)
                  .ToArray();
            }
            catch (Exception)
            {
                entries = new Entry[0];
            }
            if (entries.Length > 0)
            {
                Cache[typeof(TDefinition)] = entries;
            }
            return entries;
        }

        private static Entry Describe<TMapper>(StaticString name) where TMapper : UIMapper
        {
            var entry = new Entry { Name = name.ToString(), Display = name.ToString() };
            var mapper = Mapper<TMapper>(name);
            if (mapper == null)
            {
                return entry;
            }
            var title = Title(mapper);
            if (!string.IsNullOrEmpty(title))
            {
                entry.Display = title;
            }
            return entry;
        }

        private static TMapper Mapper<TMapper>(StaticString name) where TMapper : UIMapper
        {
            try
            {
                return Databases.GetDatabase<TMapper>(false) is IDatabase<TMapper> database
                       && database.TryGetValue(name, out var mapper)
                    ? mapper
                    : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>Both Title and RawTitle hold a "%Key" — the game resolves it in the widget, not on the mapper.</summary>
        private static string Title(UIMapper mapper) =>
            Sim.Localize(string.IsNullOrEmpty(mapper.Title) ? mapper.RawTitle : mapper.Title);

        public static StaticString Name(string value) => new StaticString(value);
    }
}
