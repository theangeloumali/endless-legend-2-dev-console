using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Amplitude;
using Amplitude.Framework;
using Amplitude.Mercury.Data.Simulation;
using HarmonyLib;

namespace DevConsole.Cheats
{
    /// <summary>
    /// Lifts the "only one of these ever" cap on unique units and buildings. ConstructibleDefinition.Unicity drives
    /// both the prerequisite check and the 0/1 the UI draws, so clearing the flag on the definition beats patching
    /// the check — the button stops being greyed out too. Originals are kept so it can be put back.
    /// </summary>
    internal static class Limits
    {
        private static readonly FieldInfo UnicityField = AccessTools.Field(typeof(ConstructibleDefinition), "Unicity");
        private static readonly Dictionary<string, object> Original = new Dictionary<string, object>();

        public static bool Lifted => Original.Count > 0;

        public static int Affected => Original.Count;

        /// <summary>Definitions are shared runtime data, not save data, so this lasts for the session and is undone
        /// either by toggling it off or by restarting the game.</summary>
        public static void Lift()
        {
            if (UnicityField == null || Lifted)
            {
                return;
            }
            var any = Enum.ToObject(UnicityField.FieldType, 0);  // UnicityFlags.Any — the enum is not public, so it is built by value
            foreach (var definition in Definitions())
            {
                var current = UnicityField.GetValue(definition);
                if (Convert.ToInt32(current) == 0)
                {
                    continue;  // already unrestricted, nothing to remember
                }
                Original[definition.Name.ToString()] = current;
                UnicityField.SetValue(definition, any);
            }
            Orders.Log.LogInfo($"unit limits lifted on {Original.Count} definition(s)");
        }

        public static void Restore()
        {
            if (UnicityField == null || !Lifted)
            {
                return;
            }
            foreach (var definition in Definitions())
            {
                if (Original.TryGetValue(definition.Name.ToString(), out var value))
                {
                    UnicityField.SetValue(definition, value);
                }
            }
            Orders.Log.LogInfo($"unit limits restored on {Original.Count} definition(s)");
            Original.Clear();
        }

        private static IEnumerable<ConstructibleDefinition> Definitions()
        {
            var units = Databases.GetDatabase<UnitDefinition>(false);
            return units == null ? Enumerable.Empty<ConstructibleDefinition>() : units.Cast<ConstructibleDefinition>();
        }
    }
}
