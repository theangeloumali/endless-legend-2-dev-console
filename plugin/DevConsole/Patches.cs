using System.Reflection;
using Amplitude;
using BepInEx.Logging;
using HarmonyLib;

namespace DevConsole
{
    /// <summary>
    /// Yield multipliers only. Everything else now goes through the game's orders; these stay patched because an
    /// order can SET a stock but nothing in the order set scales per-turn income. Each patch reads its toggle
    /// before touching reflection, so with the multipliers off they cost one field read. Note a console "+research"
    /// is multiplied too: orders are processed asynchronously, so no flag can exempt them.
    /// </summary>
    internal static class Patches
    {
        internal static State State;

        private static PropertyInfo settlementEmpireIndex;

        public static void Apply(Harmony harmony, State state, ManualLogSource log)
        {
            State = state;
            settlementEmpireIndex = AccessTools.Property(Sim.Type("Settlement"), "EmpireIndex");
            Patch(harmony, log, "DepartmentOfTheTreasury", "GainMoney", nameof(MoneyPrefix));
            Patch(harmony, log, "DepartmentOfCulture", "GainInfluence", nameof(InfluencePrefix));
            Patch(harmony, log, "DepartmentOfScience", "GainResearch", nameof(ResearchPrefix));
            Patch(harmony, log, "DepartmentOfIndustry", "ComputeProductionIncome", nameof(ProductionPostfix), prefix: false);
        }

        private static void Patch(Harmony harmony, ManualLogSource log, string type, string method, string patch, bool prefix = true)
        {
            var original = Sim.Method(type, method, log);
            if (original == null)
            {
                return;
            }
            var patcher = new HarmonyMethod(typeof(Patches).GetMethod(patch, BindingFlags.Static | BindingFlags.NonPublic));
            harmony.Patch(original, prefix ? patcher : null, prefix ? null : patcher);
        }

        private static void ScaleGain(object department, ref FixedPoint gain, int factor)
        {
            if (factor > 1 && Sim.IsLocal(Sim.EmpireOf(department)))
            {
                gain = Sim.Scale(gain, factor);
            }
        }

        private static void MoneyPrefix(object __instance, ref FixedPoint gain) => ScaleGain(__instance, ref gain, State.DustMultiplier.Value);

        private static void InfluencePrefix(object __instance, ref FixedPoint gain) => ScaleGain(__instance, ref gain, State.InfluenceMultiplier.Value);

        private static void ResearchPrefix(object __instance, ref FixedPoint gain) => ScaleGain(__instance, ref gain, State.EffectiveScience);

        // static ComputeProductionIncome(Settlement): the settlement carries its empire index
        private static void ProductionPostfix(object settlement, ref FixedPoint __result)
        {
            var factor = State.EffectiveIndustry;
            if (factor > 1 && settlement != null && settlementEmpireIndex != null
                && Sim.IsLocal((int)settlementEmpireIndex.GetValue(settlement)))
            {
                __result = Sim.Scale(__result, factor);
            }
        }
    }
}
