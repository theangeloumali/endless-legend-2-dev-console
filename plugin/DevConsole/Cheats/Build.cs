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
            public string Kind;      // city or camp
            public SimulationEntityGUID Guid;
            public List<string> Queue;

            public string Label => $"{Name}  ({Kind}, {Queue.Count} queued)";
        }

        public static List<Site> Sites()
        {
            var sites = new List<Site>();
            foreach (var entry in Sim.Settled())
            {
                sites.Add(new Site
                {
                    Name = Sim.NameOf(entry.Value, entry.Key),
                    Kind = entry.Key,
                    Guid = Sim.GuidOf(entry.Value),
                    Queue = QueueOf(entry.Value),
                });
            }
            return sites;
        }

        /// <summary>Construction is an internal struct, so entries are read through the shared walker and only
        /// their definition name is kept.</summary>
        private static List<string> QueueOf(object settlement)
        {
            var names = new List<string>();
            var queue = Traverse.Create(settlement).Field("ConstructionQueue").Property("Entity").GetValue();
            foreach (var construction in Sim.Structs(queue, "Constructions"))
            {
                var definition = Traverse.Create(construction).Field("ConstructibleDefinition").GetValue();
                var name = definition == null ? null : Traverse.Create(definition).Property("Name").GetValue();
                names.Add(name?.ToString() ?? "construction");
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

        /// <summary>Approval is a per-settlement cheat field the developers left in: the order ADDS to
        /// GodApprovalDelta rather than setting it, and clearing resets that delta to zero.</summary>
        public static void AddApproval(Site site, int amount) =>
            Orders.Post(new OrderChangeGodApproval { SettlementGUID = site.Guid, GiveGodApproval = true, ApprovalDiff = amount },
                        $"{site.Name}: approval {amount:+#;-#;0}");

        public static void ClearApproval(Site site) =>
            Orders.Post(new OrderChangeGodApproval { SettlementGUID = site.Guid, GiveGodApproval = false, ApprovalDiff = 0 },
                        $"{site.Name}: approval cheat cleared");

        public static void AddApprovalEverywhere(int amount)
        {
            foreach (var site in Sites())
            {
                AddApproval(site, amount);
            }
        }

        public static void ClearApprovalEverywhere()
        {
            foreach (var site in Sites())
            {
                ClearApproval(site);
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
