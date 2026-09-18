using System.Collections.Generic;
using System.Linq;
using Amplitude.Mercury.Data.Simulation;
using Amplitude.Mercury.Interop;

namespace DevConsole.Cheats
{
    /// <summary>Stocks, resources and technologies, all through the game's own orders.</summary>
    internal static class Economy
    {
        public static readonly int[] Strategic = { 0, 1, 2, 3, 4, 5 };
        public static readonly int[] Luxury = { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25 };
        public static readonly int[] Specials = { 26, 27 };  // ResourceCadaver, ResourceSpirit
        public static readonly int[] All = Strategic.Concat(Luxury).Concat(Specials).ToArray();
        public const int EraCount = 7;

        public static void SetDust(int amount) =>
            Orders.Post(new EditorOrderSetMoneyStock { EmpireIndex = Sim.LocalEmpireIndex, MoneyStock = amount }, $"set Dust = {amount}");

        public static void SetInfluence(int amount) =>
            Orders.Post(new EditorOrderSetInfluenceStock { EmpireIndex = Sim.LocalEmpireIndex, InfluenceStock = amount }, $"set Influence = {amount}");

        public static void AddResearch(int amount) =>
            Orders.Post(new OrderInvestResearch { Gain = amount }, $"+{amount} Research");

        public static void SetCityCap(int cap) =>
            Orders.Post(new EditorOrderSetGodCityCap { EmpireIndex = Sim.LocalEmpireIndex, CityCap = cap }, $"set city cap = {cap}");

        public static void AddResources(IEnumerable<int> resourceIndexes, int amount)
        {
            foreach (var index in resourceIndexes)
            {
                Orders.Post(new OrderGiveGodResource { ResourceType = (ResourceType)index, QuantityOfResources = amount },
                            $"+{amount} resource #{index}");
            }
        }

        public static void UnlockEra(int eraIndex) =>
            Orders.Post(new EditorOrderCompleteAllTechnology { EmpireIndex = Sim.LocalEmpireIndex, EraIndex = eraIndex }, $"unlock era {eraIndex + 1}");

        public static void UnlockAllEras()
        {
            for (var era = 0; era < EraCount; era++)
            {
                UnlockEra(era);
            }
        }

        public static void CompleteTechnology(string technologyName) =>
            Orders.Post(new EditorOrderCompleteTechnology { EmpireIndex = Sim.LocalEmpireIndex, TechnologyName = Catalog.Name(technologyName) },
                        $"complete {technologyName}");
    }
}
