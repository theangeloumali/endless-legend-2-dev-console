using System;
using System.Collections.Generic;
using System.Reflection;
using Amplitude;
using BepInEx.Logging;
using HarmonyLib;

namespace DevConsole
{
    /// <summary>Instant actions on the human empire, each wrapped so a missing member logs instead of crashing.</summary>
    internal sealed class Actions
    {
        public static readonly int[] Strategic = { 0, 1, 2, 3, 4, 5 };
        public static readonly int[] Luxury = { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25 };
        public static readonly int[] Specials = { 26, 27 };  // ResourceCadaver, ResourceSpirit
        public const int EraCount = 7;

        private readonly ManualLogSource log;
        private readonly MethodInfo gainMoney = AccessTools.Method(Sim.Type("DepartmentOfTheTreasury"), "GainMoney");
        private readonly MethodInfo gainInfluence = AccessTools.Method(Sim.Type("DepartmentOfCulture"), "GainInfluence");
        private readonly MethodInfo gainResearch = AccessTools.Method(Sim.Type("DepartmentOfScience"), "GainResearch");
        private readonly MethodInfo unlockEra = AccessTools.Method(Sim.Type("DepartmentOfScience"), "UnlockAllTechnologies");
        private readonly MethodInfo giveResource = AccessTools.Method(Sim.Type("DepartmentOfResources"), "GiveGodAccessToResource");
        // explicit interface implementation: the method name carries the interface's full name
        private readonly MethodInfo setHealthRatio = AccessTools.Method(Sim.Type("Army"), "Amplitude.Mercury.Simulation.IDamageableEntity.SetHealthRatio");

        public Actions(ManualLogSource log)
        {
            this.log = log;
            foreach (var (name, method) in new[] { ("GainMoney", gainMoney), ("GainInfluence", gainInfluence), ("GainResearch", gainResearch),
                                                   ("UnlockAllTechnologies", unlockEra), ("GiveGodAccessToResource", giveResource), ("SetHealthRatio", setHealthRatio) })
            {
                if (method == null)
                {
                    log.LogWarning($"{name} not found in this game build; its button is disabled.");
                }
            }
        }

        public bool CanGainMoney => gainMoney != null;
        public bool CanGainInfluence => gainInfluence != null;
        public bool CanGainResearch => gainResearch != null;
        public bool CanUnlockEras => unlockEra != null;
        public bool CanGiveResources => giveResource != null;
        public bool CanHeal => setHealthRatio != null;

        public void AddDust(int amount) =>
            ForEachHuman("DepartmentOfTheTreasury", d => gainMoney.Invoke(d, new object[] { Sim.Units(amount), true, false }));

        public void AddInfluence(int amount) =>
            ForEachHuman("DepartmentOfCulture", d => gainInfluence.Invoke(d, new object[] { Sim.Units(amount), true }));

        public void AddResearch(int amount) =>
            ForEachHuman("DepartmentOfScience", d => gainResearch.Invoke(d, new object[] { Sim.Units(amount), true }));

        public void UnlockEra(int eraIndex) =>
            ForEachHuman("DepartmentOfScience", d => unlockEra.Invoke(d, new object[] { eraIndex, true }));

        public void AddResources(IEnumerable<int> resourceIndexes, int amount) =>
            ForEachHuman("DepartmentOfResources", d =>
            {
                foreach (var index in resourceIndexes)
                {
                    giveResource.Invoke(d, new[] { Enum.ToObject(Sim.ResourceType, index), amount });
                }
            });

        public void HealArmies()
        {
            foreach (var empire in Sim.HumanEmpires())
            {
                var armies = Traverse.Create(empire).Field("Armies");
                var count = armies.Property<int>("Count").Value;
                for (var i = 0; i < count; i++)
                {
                    var army = armies.Property("Item", new object[] { i }).GetValue();
                    Guard(() => setHealthRatio.Invoke(army, new object[] { Sim.Units(1) }), "HealArmies");
                }
            }
        }

        private void ForEachHuman(string department, Action<object> action)
        {
            var count = 0;
            foreach (var empire in Sim.HumanEmpires())
            {
                count++;
                var target = Sim.Department(empire, department);
                if (target == null)
                {
                    log.LogWarning($"{department} missing on empire #{Traverse.Create(empire).Field<int>("Index").Value}");
                    continue;
                }
                Guard(() => action(target), department);
            }
            log.LogInfo($"{department}: applied to {count} human empire(s)");
        }

        private void Guard(Action action, string what)
        {
            Patches.Suppress = true;  // the console's own +N must not be multiplied by the yield patches
            try
            {
                action();
            }
            catch (Exception exception)
            {
                log.LogError($"{what} failed: {exception.InnerException?.Message ?? exception.Message}");
            }
            finally
            {
                Patches.Suppress = false;
            }
        }
    }
}
