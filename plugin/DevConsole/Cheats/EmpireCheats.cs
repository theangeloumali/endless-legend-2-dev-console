using Amplitude;
using Amplitude.Mercury.Interop;

namespace DevConsole.Cheats
{
    /// <summary>Empire-wide levers: population, cooldowns, statuses and quests.</summary>
    internal static class EmpireCheats
    {
        public static void AddPopulation(Build.Site site, int tileIndex, int diff) =>
            Orders.Post(new EditorOrderAddOrRemovePopulation { SettlementTileIndex = tileIndex, PopulationDiff = diff },
                        $"{site.Name}: population {diff:+#;-#;0}");

        public static void AddSpecificPopulation(Build.Site site, string populationName) =>
            Orders.Post(new EditorOrderAddSpecificPopulation { SettlementGUID = site.Guid, PopulationName = new StaticString(populationName) },
                        $"{site.Name}: +1 {populationName}");

        public static void BuyPopulationWithMoney(Build.Site site, string populationName) =>
            Orders.Post(new OrderBuyoutPopulationWithMoney { SettlementGUID = site.Guid, PopulationDefinitionName = new StaticString(populationName) },
                        $"{site.Name}: buy {populationName}");

        public static void BuyPopulationWithCadavers(Build.Site site) =>
            Orders.Post(new OrderBuyoutPopulationWithCadavers { SettlementGUID = site.Guid }, $"{site.Name}: buy population with cadavers");

        /// <summary>Improvements attach to a district tile but are owned by a settlement, so both indexes matter.</summary>
        public static void SetImprovement(Build.Site site, string improvementName, int tileIndex) =>
            Orders.Post(new EditorOrderSetDistrictImprovement
            {
                SettlementTileIndex = site.TileIndex,
                SettlementImprovementName = new StaticString(improvementName),
                TileIndex = tileIndex,
            }, $"{site.Name}: improvement {improvementName} at tile {tileIndex}");

        public static void ResetEmpireActions() =>
            Orders.Post(new EditorOrderResetEmpireActionsCooldown { EmpireIndex = Sim.LocalEmpireIndex }, "reset empire action cooldowns");

        public static void ResetCultureAffinity() =>
            Orders.Post(new EditorOrderResetCultureAffinityCooldown(), "reset culture affinity cooldown");

        public static void ResetRaiseReservist() =>
            Orders.Post(new EditorOrderResetRaiseReservistCooldown(), "reset raise reservist cooldown");

        public static void ResetStealPopulation() =>
            Orders.Post(new EditorOrderResetStealPopulationCooldown(), "reset steal population cooldown");

        public static void ResetMerchantAffinity() =>
            Orders.Post(new EditorOrderResetMerchantAffinityGauge(), "reset merchant affinity gauge");

        public static void ResetEverything()
        {
            ResetEmpireActions();
            ResetCultureAffinity();
            ResetRaiseReservist();
            ResetStealPopulation();
            ResetMerchantAffinity();
        }

        /// <summary>Statuses attach to whatever occupies the tile; duration 0 means the definition decides.</summary>
        public static void AddStatus(string statusName, int tileIndex, int duration) =>
            Orders.Post(new EditorOrderAddStatus
            {
                StatusDefinitionName = new StaticString(statusName),
                TileIndex = tileIndex,
                AddStatusForcedDuration = duration,
            }, $"status {statusName} at tile {tileIndex}");

        public static void RemoveStatus(string statusName, int tileIndex) =>
            Orders.Post(new EditorOrderRemoveStatus { StatusDefinitionName = new StaticString(statusName), TileIndex = tileIndex },
                        $"remove status {statusName} at tile {tileIndex}");

        public static void StartQuest(string questName) =>
            Orders.Post(new EditorOrderForceQuestStart
            {
                EmpireIndex = Sim.LocalEmpireIndex,
                QuestName = new StaticString(questName),
                QuestChoiceIndex = 0,
                QuestStepIndex = 0,
            }, $"start quest {questName}");

        public static void AdvanceQuest(int questIndex) =>
            Orders.Post(new EditorOrderForceQuestNextStep { EmpireIndex = Sim.LocalEmpireIndex, QuestIndex = questIndex },
                        $"advance quest #{questIndex}");
    }
}
