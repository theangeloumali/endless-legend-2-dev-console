using System;
using System.Collections.Generic;
using System.Reflection;
using Amplitude;
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

        public static readonly Type Sandbox = AccessTools.TypeByName("Amplitude.Mercury.Sandbox.Sandbox");
        public static readonly Type MajorEmpire = AccessTools.TypeByName(Ns + "MajorEmpire");
        public static readonly Type ResourceType = AccessTools.TypeByName("Amplitude.Mercury.Data.Simulation.ResourceType");

        private static readonly FieldInfo MajorEmpiresField = AccessTools.Field(Sandbox, "MajorEmpires");

        public static Type Type(string simpleName) => AccessTools.TypeByName(Ns + simpleName);

        /// <summary>Major empires currently controlled by a human player (single player: exactly one).</summary>
        public static IEnumerable<object> HumanEmpires()
        {
            if (MajorEmpiresField?.GetValue(null) is Array empires)
            {
                foreach (var empire in empires)
                {
                    if (empire != null && IsHuman(empire))
                    {
                        yield return empire;
                    }
                }
            }
        }

        /// <summary>IsControlledByHuman is only set through the lobby message path; in a single-player sandbox the
        /// player's empire is the one whose AI brain is off, so both signals count.</summary>
        public static bool IsHuman(object empire)
        {
            if (empire == null)
            {
                return false;
            }
            var traverse = Traverse.Create(empire);
            return traverse.Property<bool>("IsControlledByHuman").Value || !traverse.Field<bool>("IsAIBrainActivated").Value;
        }

        /// <summary>True when the empire at that index is human; used by patches that only see an index.</summary>
        public static bool IsHumanIndex(int empireIndex)
        {
            if (empireIndex < 0 || !(MajorEmpiresField?.GetValue(null) is Array empires) || empireIndex >= empires.Length)
            {
                return false;
            }
            return IsHuman(empires.GetValue(empireIndex));
        }

        /// <summary>Departments hang off MajorEmpire as fields (DepartmentOfTheTreasury, DepartmentOfScience, ...).</summary>
        public static object Department(object empire, string name) => Traverse.Create(empire).Field(name).GetValue();

        /// <summary>Agency.Empire is the department's owning empire.</summary>
        public static object EmpireOf(object department) => Traverse.Create(department).Field("Empire").GetValue();

        public static int EmpireIndexOf(object entity) =>
            entity == null ? -1 : Traverse.Create(entity).Property<int>("EmpireIndex").Value;

        public static FixedPoint Units(int value) => (FixedPoint)value;  // op_Implicit applies the x1000 fixed point

        /// <summary>Multiply a FixedPoint by an int, clamped so the Int32 raw value cannot wrap.</summary>
        public static FixedPoint Scale(FixedPoint value, int factor)
        {
            var units = (int)value;  // op_Explicit: whole units
            if (units <= 0 || factor <= 1)
            {
                return value;
            }
            const int maxUnits = 2_000_000;  // RawValue is Int32 (x1000): 2,147,483 units is the hard ceiling
            return units > maxUnits / factor ? Units(maxUnits) : value * factor;
        }
    }
}
