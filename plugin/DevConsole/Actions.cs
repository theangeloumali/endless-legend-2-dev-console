using System;
using System.Linq;
using System.Reflection;
using Amplitude.Mercury.Data.Simulation;
using BepInEx.Logging;
using HarmonyLib;

namespace DevConsole
{
    /// <summary>Instant actions on the local empire, each wrapped so a missing member logs instead of crashing.</summary>
    internal sealed class Actions
    {
        public static readonly int[] Strategic = { 0, 1, 2, 3, 4, 5 };
        public static readonly int[] Luxury = { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25 };
        public static readonly int[] Specials = { 26, 27 };  // ResourceCadaver, ResourceSpirit
        public static readonly int[] All = Strategic.Concat(Luxury).Concat(Specials).ToArray();
        public const int EraCount = 7;

        private readonly ManualLogSource log;
        private readonly MethodInfo gainMoney, gainInfluence, gainResearch, unlockEra, giveResource, setHealthRatio;

        public Actions(ManualLogSource log)
        {
            this.log = log;
            gainMoney = Sim.Method("DepartmentOfTheTreasury", "GainMoney", log);
            gainInfluence = Sim.Method("DepartmentOfCulture", "GainInfluence", log);
            gainResearch = Sim.Method("DepartmentOfScience", "GainResearch", log);
            unlockEra = Sim.Method("DepartmentOfScience", "UnlockAllTechnologies", log);
            giveResource = Sim.Method("DepartmentOfResources", "GiveGodAccessToResource", log);
            // explicit interface implementation: the method name carries the interface's full name
            setHealthRatio = Sim.Method("Army", "Amplitude.Mercury.Simulation.IDamageableEntity.SetHealthRatio", log);
        }

        public bool CanGainMoney => gainMoney != null;
        public bool CanGainInfluence => gainInfluence != null;
        public bool CanGainResearch => gainResearch != null;
        public bool CanUnlockEras => unlockEra != null;
        public bool CanGiveResources => giveResource != null;
        public bool CanHeal => setHealthRatio != null;

        public void AddDust(int amount) =>
            OnLocalEmpire("DepartmentOfTheTreasury", d => gainMoney.Invoke(d, new object[] { Sim.Units(amount), true, false }));

        public void AddInfluence(int amount) =>
            OnLocalEmpire("DepartmentOfCulture", d => gainInfluence.Invoke(d, new object[] { Sim.Units(amount), true }));

        public void AddResearch(int amount) =>
            OnLocalEmpire("DepartmentOfScience", d => gainResearch.Invoke(d, new object[] { Sim.Units(amount), true }));

        public void UnlockEra(int eraIndex) =>
            OnLocalEmpire("DepartmentOfScience", d => unlockEra.Invoke(d, new object[] { eraIndex, true }));

        public void UnlockAllEras()
        {
            for (var era = 0; era < EraCount; era++)
            {
                UnlockEra(era);
            }
        }

        public void AddResources(int[] resourceIndexes, int amount) =>
            OnLocalEmpire("DepartmentOfResources", d =>
            {
                foreach (var index in resourceIndexes)
                {
                    giveResource.Invoke(d, new object[] { (ResourceType)index, amount });
                }
            });

        public void HealArmies() =>
            OnLocalEmpire("Armies", armies =>
            {
                var collection = Traverse.Create(armies);
                var count = collection.Property<int>("Count").Value;
                for (var i = 0; i < count; i++)
                {
                    setHealthRatio.Invoke(collection.Property("Item", new object[] { i }).GetValue(), new object[] { Sim.Units(1) });
                }
            });

        private void OnLocalEmpire(string department, Action<object> action)
        {
            var empire = Sim.LocalEmpire;
            var target = empire == null ? null : Sim.Department(empire, department);
            if (target == null)
            {
                log.LogWarning($"{department}: no local empire to apply to (not in a game?)");
                return;
            }
            Guard(() => action(target), department);
            log.LogInfo($"{department}: applied to empire #{Sim.LocalEmpireIndex}");
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
