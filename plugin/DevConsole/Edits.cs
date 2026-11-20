using System;
using System.Runtime.CompilerServices;
using Forge.Core;

namespace DevConsole
{
    /// <summary>The console's edit log: every order outcome lands here, with its undo when the game has one.</summary>
    internal static class Edits
    {
        public static readonly EditLog Log = new EditLog();

        /// <summary>Values an order is about to overwrite, keyed by the order instance until its outcome arrives.</summary>
        private static readonly ConditionalWeakTable<object, Func<string, int?>> Before = new ConditionalWeakTable<object, Func<string, int?>>();

        /// <summary>Raised after every entry, for the recipe recorder.</summary>
        public static event Action<EditEntry> Added;

        public static void Initialize()
        {
            Orders.Posting += Capture;
            Orders.Outcome += Record;
        }

        private static void Capture(object order)
        {
            int money = Sim.MoneyStock, influence = Sim.InfluenceStock;
            Before.Remove(order);
            Before.Add(order, field => field == "MoneyStock" && money >= 0 ? money
                                     : field == "InfluenceStock" && influence >= 0 ? influence
                                     : (int?)null);
        }

        private static void Record(object order, string label, EditOutcome outcome, string reason)
        {
            var record = OrderCodec.Snapshot(order);
            OrderRecord undo = null;
            if (outcome == EditOutcome.Posted)
            {
                Before.TryGetValue(order, out var previous);
                InverseTable.TryInvert(record, previous, out undo);
            }
            Before.Remove(order);
            var entry = new EditEntry { Turn = Sim.Turn, Label = label, Outcome = outcome, Reason = reason, Record = record, Undo = undo };
            Log.Add(entry);
            Added?.Invoke(entry);
        }

        /// <summary>Posts the opposite order; its own outcome shows as a new row, so a rejected undo is visible.</summary>
        public static void Undo(EditEntry entry)
        {
            if (entry?.Undo == null || entry.Undone)
            {
                return;
            }
            var order = OrderCodec.Build(entry.Undo, out var problem);
            if (order == null)
            {
                Log.Add(new EditEntry { Turn = Sim.Turn, Label = "undo " + entry.Label, Outcome = EditOutcome.Skipped, Reason = problem });
                return;
            }
            entry.Undone = true;
            Orders.PostAny(order, "undo " + entry.Label);
        }
    }
}
