using System;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Framework;
using Amplitude.Framework.Localization;
using Amplitude.Mercury.Data.Simulation;
using Amplitude.Mercury.UI;
using Amplitude.UI;

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

            public override string ToString() => Display;
        }

        private static readonly Dictionary<Type, Entry[]> Cache = new Dictionary<Type, Entry[]>();

        public static Entry[] Equipment => Entries<HeroEquipmentDefinition, HeroEquipmentUIMapper>();
        public static Entry[] Heroes => Entries<HeroDefinition, HeroUnitUIMapper>();
        public static Entry[] Units => Entries<UnitDefinition, UnitUIMapper>();
        public static Entry[] Technologies => Entries<TechnologyDefinition, TechnologyUIMapper>();

        /// <summary>Built on first use: the databases are not loaded while the plugin is constructed, and an empty
        /// result is never cached so the next call retries.</summary>
        private static Entry[] Entries<TDefinition, TMapper>()
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
                entries = definitions == null
                    ? new Entry[0]
                    : definitions.Select(definition => Describe<TMapper>(definition.Name))
                                 .OrderBy(entry => entry.Display, StringComparer.CurrentCultureIgnoreCase)
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

        /// <summary>Both Title and RawTitle hold a "%Key" localization key — the game resolves it in the widget,
        /// not on the mapper — so anything %-prefixed goes through the localization service before display.</summary>
        private static string Title(UIMapper mapper)
        {
            var text = string.IsNullOrEmpty(mapper.Title) ? mapper.RawTitle : mapper.Title;
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }
            if (text[0] != '%')
            {
                return text;
            }
            var localized = Services.GetService<ILocalizationService>()?.Localize(text);
            return string.IsNullOrEmpty(localized) || localized == text ? null : localized;
        }

        public static StaticString Name(string value) => new StaticString(value);
    }
}
