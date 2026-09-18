using System.Reflection;
using Amplitude;
using BepInEx.Logging;
using HarmonyLib;

namespace DevConsole
{
    /// <summary>
    /// Harmony patches that scale the local empire's income and battle damage. Targets are resolved by name
    /// (internal types) and patched one by one, so a renamed method disables that toggle instead of the plugin.
    /// Every patch checks its toggle before touching reflection, so with everything off they cost a field read.
    /// </summary>
    internal static class Patches
    {
        internal static State State;

        /// <summary>Set while the console itself calls a Gain* method, so its own +N is not multiplied.</summary>
        internal static bool Suppress;

        private static PropertyInfo settlementEmpireIndex, battleUnitEmpireIndex;

        public static void Apply(Harmony harmony, State state, ManualLogSource log)
        {
            State = state;
            settlementEmpireIndex = AccessTools.Property(Sim.Type("Settlement"), "EmpireIndex");
            battleUnitEmpireIndex = AccessTools.Property(Sim.Type("BattleUnit"), "EmpireIndex");
            Patch(harmony, log, "DepartmentOfTheTreasury", "GainMoney", nameof(MoneyPrefix));
            Patch(harmony, log, "DepartmentOfCulture", "GainInfluence", nameof(InfluencePrefix));
            Patch(harmony, log, "DepartmentOfScience", "GainResearch", nameof(ResearchPrefix));
            Patch(harmony, log, "DepartmentOfIndustry", "ComputeProductionIncome", nameof(ProductionPostfix), prefix: false);
            Patch(harmony, log, "BattleUnit", "ApplyDamage", nameof(DamagePrefix));
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
            if (factor > 1 && !Suppress && Sim.IsLocal(Sim.EmpireOf(department)))
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
            if (factor > 1 && Sim.IsLocal(EmpireIndex(settlementEmpireIndex, settlement)))
            {
                __result = Sim.Scale(__result, factor);
            }
        }

        // BattleUnit.ApplyDamage(FixedPoint damage, BattleUnit attackerUnit, bool sendBattleEvent)
        private static void DamagePrefix(object __instance, ref FixedPoint damage, object attackerUnit)
        {
            if (!State.Invulnerable.Value && !State.OneHitKills.Value)
            {
                return;
            }
            var targetIsLocal = Sim.IsLocal(EmpireIndex(battleUnitEmpireIndex, __instance));
            if (State.Invulnerable.Value && targetIsLocal)
            {
                damage = Sim.Units(0);
            }
            else if (State.OneHitKills.Value && !targetIsLocal && Sim.IsLocal(EmpireIndex(battleUnitEmpireIndex, attackerUnit)))
            {
                damage = Sim.Units(99_999);
            }
        }

        private static int EmpireIndex(PropertyInfo property, object entity) =>
            entity == null || property == null ? -1 : (int)property.GetValue(entity);
    }
}
