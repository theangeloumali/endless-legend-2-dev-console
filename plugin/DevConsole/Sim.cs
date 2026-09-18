using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Amplitude;
using Amplitude.Framework;
using Amplitude.Framework.Localization;
using Amplitude.Mercury.Interop;
using Amplitude.Mercury.Simulation;
using BepInEx.Logging;
using HarmonyLib;

namespace DevConsole
{
    /// <summary>
    /// Reflection bridge to Amplitude.Mercury.Simulation. Every simulation type is internal, so members are
    /// resolved by name once and null-checked; a missing member disables its feature instead of throwing.
    /// </summary>
    internal static class Sim
    {
        private const string Ns = "Amplitude.Mercury.Simulation.";

        private static readonly Type SandboxType = AccessTools.TypeByName("Amplitude.Mercury.Sandbox.Sandbox");
        private static readonly FieldInfo CurrentSandbox = AccessTools.Field(AccessTools.TypeByName("Amplitude.Mercury.Sandbox.SandboxManager"), "Sandbox");
        private static readonly PropertyInfo LocalEmpireIndexProperty = AccessTools.Property(SandboxType, "LocalEmpireIndex");
        private static readonly PropertyInfo LocalEmpireProperty = AccessTools.Property(SandboxType, "LocalEmpire");
        private static readonly FieldInfo AgencyEmpire = AccessTools.Field(Type("Agency"), "Empire");

        /// <summary>Every simulation type lives in Sandbox's assembly: a hash lookup instead of a scan of all loaded assemblies.</summary>
        public static Type Type(string simpleName) => SandboxType?.Assembly.GetType(Ns + simpleName);

        public static MethodInfo Method(string type, string name, ManualLogSource log)
        {
            var method = AccessTools.Method(Type(type), name);
            if (method == null)
            {
                log.LogWarning($"{type}.{name} not found in this game build; its control does nothing.");
            }
            return method;
        }

        /// <summary>The empire the player at this machine controls; null outside a game. The game keeps it current on hot-seat swaps.</summary>
        public static object LocalEmpire => Sandbox is object sandbox ? LocalEmpireProperty?.GetValue(sandbox) : null;

        public static int LocalEmpireIndex => Sandbox is object sandbox && LocalEmpireIndexProperty != null ? (int)LocalEmpireIndexProperty.GetValue(sandbox) : -1;

        public static bool IsLocal(object empire) => empire != null && ReferenceEquals(empire, LocalEmpire);

        public static bool IsLocal(int empireIndex) => empireIndex >= 0 && empireIndex == LocalEmpireIndex;

        private static object Sandbox => CurrentSandbox?.GetValue(null);

        /// <summary>Agency.Empire is the department's owning empire.</summary>
        public static object EmpireOf(object department) => department == null ? null : AgencyEmpire?.GetValue(department);

        /// <summary>Walk a ReferenceCollection&lt;T&gt; whose element type is internal. The collection type itself is
        /// public, but naming ReferenceCollection&lt;Settlement&gt; is impossible, so Count/Item go through Traverse.</summary>
        public static IEnumerable<object> Collection(object owner, string fieldName)
        {
            var collection = owner == null ? null : Traverse.Create(owner).Field(fieldName).GetValue();
            if (collection == null)
            {
                yield break;
            }
            var traverse = Traverse.Create(collection);
            var count = traverse.Property<int>("Count").Value;
            for (var i = 0; i < count; i++)
            {
                var item = traverse.Property("Item", new object[] { i }).GetValue();
                if (item != null)
                {
                    yield return item;
                }
            }
        }

        /// <summary>Walk an Amplitude ListOfStruct, whose Length/Data are public but whose element type is not.
        /// Elements come back boxed, which is fine here: they are read once to build a UI snapshot.</summary>
        public static IEnumerable<object> Structs(object owner, string fieldName)
        {
            var list = owner == null ? null : Traverse.Create(owner).Field(fieldName).GetValue();
            if (list == null)
            {
                yield break;
            }
            var traverse = Traverse.Create(list);
            var length = traverse.Field<int>("Length").Value;
            if (!(traverse.Field("Data").GetValue() is Array data))
            {
                yield break;
            }
            for (var i = 0; i < length && i < data.Length; i++)
            {
                yield return data.GetValue(i);
            }
        }

        public static IEnumerable<object> Settlements() => Settled().Select(pair => pair.Value);

        /// <summary>Cities and camps live in separate collections that may overlap with the general one, so all
        /// three are walked and deduplicated by GUID. Each entry carries the kind it was found under.</summary>
        public static IEnumerable<KeyValuePair<string, object>> Settled()
        {
            var seen = new HashSet<ulong>();
            foreach (var source in new[] { ("city", "Cities"), ("camp", "Camps"), ("settlement", "Settlements") })
            {
                foreach (var settlement in Collection(LocalEmpire, source.Item2))
                {
                    if (seen.Add((ulong)GuidOf(settlement)))
                    {
                        yield return new KeyValuePair<string, object>(source.Item1, settlement);
                    }
                }
            }
        }

        public static IEnumerable<object> Heroes() => Collection(LocalEmpire, "Heroes");

        public static IEnumerable<object> Armies() => Collection(LocalEmpire, "Armies");

        /// <summary>Every simulation entity carries its GUID on the internal SimulationEntity base.</summary>
        public static SimulationEntityGUID GuidOf(object entity) =>
            entity == null ? SimulationEntityGUID.Zero : Traverse.Create(entity).Field<SimulationEntityGUID>("GUID").Value;

        /// <summary>EntityNameInfo is public; prefer what the player renamed it to, then the generated name.
        /// The localization key is a last resort and has to be resolved, or the UI shows "%Hero_..._1Title".</summary>
        public static string NameOf(object entity, string fallback)
        {
            var info = entity == null ? null : Traverse.Create(entity).Field("EntityName").GetValue();
            if (info is EntityNameInfo name)
            {
                foreach (var candidate in new[] { name.UserDefinedName, name.UniqueName, name.LocalizationKey })
                {
                    if (!string.IsNullOrEmpty(candidate))
                    {
                        return Localize(candidate) ?? candidate;
                    }
                }
            }
            return fallback;
        }

        /// <summary>Resolves a "%Key" through the game's localization service. Plain text passes through; an
        /// unresolvable key returns null so the caller can fall back.</summary>
        public static string Localize(string text)
        {
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

        public static FixedPoint Units(int value) => (FixedPoint)value;  // op_Implicit applies the x1000 fixed point

        /// <summary>Multiply a FixedPoint by an int, clamped so the Int32 raw value cannot wrap.</summary>
        public static FixedPoint Scale(FixedPoint value, int factor)
        {
            var units = (int)value;  // op_Explicit: whole units
            if (units <= 0 || factor <= 1)
            {
                return value;
            }
            const int maxUnits = 2_000_000;  // RawValue is Int32 (x1000): 2,147,483 units is the ceiling; keep headroom for the stock it lands in
            return units > maxUnits / factor ? Units(maxUnits) : value * factor;
        }
    }
}
