using System.Reflection;
using Amplitude;
using BepInEx.Logging;
using HarmonyLib;

namespace DevConsole
{
    /// <summary>
    /// Yield multipliers: the one thing no order does, since orders set a stock rather than scaling income.
    /// A console "+research" is multiplied too — orders apply asynchronously, so no flag can exempt them.
    /// </summary>
    internal static class Patches
    {
        internal static State State;

        /// <summary>True when every income patch attached; the Yields tab says so rather than failing silently.</summary>
        internal static bool Applied { get; private set; }

        private static ManualLogSource log;
        private static bool loggedScale;

        private static PropertyInfo settlementEmpireIndex;

        public static void Apply(Harmony harmony, State state, ManualLogSource logger)
        {
            State = state;
            log = logger;
            Applied = true;
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
                Applied = false;
                return;
            }
            var patcher = new HarmonyMethod(typeof(Patches).GetMethod(patch, BindingFlags.Static | BindingFlags.NonPublic));
            harmony.Patch(original, prefix ? patcher : null, prefix ? null : patcher);
        }

        private static void ScaleGain(object department, ref FixedPoint gain, int factor)
        {
            if (factor <= 1 || !Sim.IsLocal(Sim.EmpireOf(department)))
            {
                return;
            }
            var before = gain;
            gain = Sim.Scale(gain, factor);
            if (!loggedScale)
            {
                loggedScale = true;  // once per session: enough to prove the patches fire, without spamming
                log.LogInfo($"yield multiplier active: {(int)before} -> {(int)gain} (x{factor})");
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
