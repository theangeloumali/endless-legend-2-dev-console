using System.Reflection;
using Amplitude;
using BepInEx.Logging;
using HarmonyLib;

namespace DevConsole
{
    /// <summary>
    /// Harmony patches that scale the human empire's income and battle damage. Targets are resolved by name
    /// (internal types) and patched one by one, so a renamed method disables that toggle instead of the plugin.
    /// </summary>
    internal static class Patches
    {
        internal static State State;

        /// <summary>Set while the console itself calls a Gain* method, so its own +N is not multiplied.</summary>
        internal static bool Suppress;

        public static void Apply(Harmony harmony, State state, ManualLogSource log)
        {
            State = state;
            Patch(harmony, log, "DepartmentOfTheTreasury", "GainMoney", nameof(MoneyPrefix), prefix: true);
            Patch(harmony, log, "DepartmentOfCulture", "GainInfluence", nameof(InfluencePrefix), prefix: true);
            Patch(harmony, log, "DepartmentOfScience", "GainResearch", nameof(ResearchPrefix), prefix: true);
            Patch(harmony, log, "DepartmentOfIndustry", "ComputeProductionIncome", nameof(ProductionPostfix), prefix: false);
            Patch(harmony, log, "BattleUnit", "ApplyDamage", nameof(DamagePrefix), prefix: true);
        }

        private static void Patch(Harmony harmony, ManualLogSource log, string type, string method, string patch, bool prefix)
        {
            var original = AccessTools.Method(Sim.Type(type), method);
            if (original == null)
            {
                log.LogWarning($"{type}.{method} not found; the related toggle does nothing on this build.");
                return;
            }
            var patcher = new HarmonyMethod(typeof(Patches).GetMethod(patch, BindingFlags.Static | BindingFlags.NonPublic));
            harmony.Patch(original, prefix ? patcher : null, prefix ? null : patcher);
        }

        private static bool HumanDepartment(object department) => !Suppress && Sim.IsHuman(Sim.EmpireOf(department));

        private static void MoneyPrefix(object __instance, ref FixedPoint gain)
        {
            if (HumanDepartment(__instance))
            {
                gain = Sim.Scale(gain, State.DustMultiplier.Value);
            }
        }

        private static void InfluencePrefix(object __instance, ref FixedPoint gain)
        {
            if (HumanDepartment(__instance))
            {
                gain = Sim.Scale(gain, State.InfluenceMultiplier.Value);
            }
        }

        private static void ResearchPrefix(object __instance, ref FixedPoint gain)
        {
            if (HumanDepartment(__instance))
            {
                gain = Sim.Scale(gain, State.EffectiveScience);
            }
        }

        // static ComputeProductionIncome(Settlement): the settlement carries its empire index
        private static void ProductionPostfix(object settlement, ref FixedPoint __result)
        {
            if (Sim.IsHumanIndex(Sim.EmpireIndexOf(settlement)))
            {
                __result = Sim.Scale(__result, State.EffectiveIndustry);
            }
        }

        // BattleUnit.ApplyDamage(FixedPoint damage, BattleUnit attackerUnit, bool sendBattleEvent)
        private static void DamagePrefix(object __instance, ref FixedPoint damage, object attackerUnit)
        {
            var targetIsHuman = Sim.IsHumanIndex(Sim.EmpireIndexOf(__instance));
            if (State.Invulnerable.Value && targetIsHuman)
            {
                damage = Sim.Units(0);
            }
            else if (State.OneHitKills.Value && !targetIsHuman && Sim.IsHumanIndex(Sim.EmpireIndexOf(attackerUnit)))
            {
                damage = Sim.Units(99_999);
            }
        }
    }
}
