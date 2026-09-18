using System;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Framework;
using Amplitude.Mercury.Data.Simulation;

namespace DevConsole
{
    /// <summary>
    /// Definition names for the dropdowns, read from the live datatables rather than baked in, so a game patch or
    /// a data mod is picked up automatically. Lists are built on first use because the databases are not loaded
    /// while the plugin is constructed.
    /// </summary>
    internal static class Catalog
    {
        private static readonly Dictionary<Type, string[]> Cache = new Dictionary<Type, string[]>();

        public static string[] Equipment => Names<HeroEquipmentDefinition>();
        public static string[] Heroes => Names<HeroDefinition>();
        public static string[] Units => Names<UnitDefinition>();
        public static string[] Technologies => Names<TechnologyDefinition>();

        /// <summary>Sorted element names of a datatable, or an empty array before the databases are loaded.
        /// Constrained to the interface, not OdinDatatableElement, to keep Sirenix off our reference list.</summary>
        public static string[] Names<T>() where T : class, IDatatableElement
        {
            if (Cache.TryGetValue(typeof(T), out var cached))
            {
                return cached;
            }
            string[] names;
            try
            {
                var database = Databases.GetDatabase<T>(false);
                names = database == null
                    ? new string[0]
                    : database.Select(element => element.Name.ToString()).OrderBy(name => name, StringComparer.Ordinal).ToArray();
            }
            catch (Exception)
            {
                names = new string[0];
            }
            if (names.Length > 0)
            {
                Cache[typeof(T)] = names;  // don't cache the empty pre-load result
            }
            return names;
        }

        public static StaticString Name(string value) => new StaticString(value);
    }
}
