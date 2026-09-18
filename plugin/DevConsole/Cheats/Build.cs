using System.Collections.Generic;
using System.Linq;
using Amplitude.Mercury.Interop;
using Amplitude.Mercury.Simulation;
using HarmonyLib;

namespace DevConsole.Cheats
{
    /// <summary>
    /// Instant build and instant recruit. Units are constructions in EL2, so completing a queue entry is both.
    /// OrderCompleteConstructionAt is free and immediate — the game's own editor path, not a production multiplier.
    /// </summary>
    internal static class Build
    {
        /// <summary>A settlement and its queue, snapshotted for the UI.</summary>
        public sealed class Site
        {
            public string Name;
            public SimulationEntityGUID Guid;
            public List<string> Queue;
        }

        public static List<Site> Sites()
        {
            var sites = new List<Site>();
            foreach (var settlement in Sim.Settlements())
            {
                sites.Add(new Site
                {
                    Name = Sim.NameOf(settlement, "settlement"),
                    Guid = Sim.GuidOf(settlement),
                    Queue = QueueOf(settlement),
                });
            }
            return sites;
        }

        /// <summary>Construction is an internal struct inside a public ListOfStruct, so the entries are read
        /// through Traverse and only their definition name is kept.</summary>
        private static List<string> QueueOf(object settlement)
        {
            var names = new List<string>();
            var queue = Traverse.Create(settlement).Field("ConstructionQueue").Property("Entity").GetValue();
            var constructions = queue == null ? null : Traverse.Create(queue).Field("Constructions").GetValue();
            if (constructions == null)
            {
                return names;
            }
            var list = Traverse.Create(constructions);
            var length = list.Field<int>("Length").Value;
            if (!(list.Field("Data").GetValue() is System.Array data))
            {
                return names;
            }
            for (var i = 0; i < length && i < data.Length; i++)
            {
                var definition = Traverse.Create(data.GetValue(i)).Field("ConstructibleDefinition").GetValue();
                var name = definition == null ? null : Traverse.Create(definition).Property("Name").GetValue();
                names.Add(name?.ToString() ?? $"entry {i}");
            }
            return names;
        }

        public static void Complete(Site site, int index) =>
            Orders.Post(new OrderCompleteConstructionAt { SettlementGUID = site.Guid, ConstructionIndex = index },
                        $"complete {site.Name}[{index}]");

        /// <summary>Completing shifts the queue down, so repeatedly finishing index 0 empties it.</summary>
        public static void CompleteQueue(Site site)
        {
            for (var i = 0; i < site.Queue.Count; i++)
            {
                Complete(site, 0);
            }
        }

        public static void CompleteEverything()
        {
            foreach (var site in Sites().Where(site => site.Queue.Count > 0))
            {
                CompleteQueue(site);
            }
        }
    }
}
