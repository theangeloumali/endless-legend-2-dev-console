using System;
using System.Runtime.CompilerServices;
using Forge.Core;

namespace DevConsole
{
    /// <summary>The console's edit log: every order outcome lands here, with its undo when the game has one.</summary>
    internal static class Edits
    {
        public static readonly EditLog Log = new EditLog();

        /// <summary>The value an order is about to overwrite, kept per order instance until its outcome arrives.</summary>
        private sealed class Previous
        {
            public string Field;
            public int Value;
        }

        private static readonly ConditionalWeakTable<object, Previous> Before = new ConditionalWeakTable<object, Previous>();

        /// <summary>Raised after every entry, for the recipe recorder.</summary>
        public static event Action<EditEntry> Added;

        public static void Initialize()
        {
            Orders.Posting += Capture;
            Orders.Outcome += Record;
        }

        private static void Capture(object order)
        {
            var field = InverseTable.PreviousValueField(order.GetType().Name);
            var value = field == null ? -1 : Sim.StockOf(field);
            Before.Remove(order);
            if (value >= 0)
            {
                Before.Add(order, new Previous { Field = field, Value = value });
            }
        }

        private static void Record(object order, string label, EditOrigin origin, EditOutcome outcome, string reason)
        {
            var record = OrderCodec.Snapshot(order);
            OrderRecord undo = null;
            if (outcome == EditOutcome.Posted)
            {
                Before.TryGetValue(order, out var previous);
                InverseTable.TryInvert(record, field => previous != null && previous.Field == field ? previous.Value : (int?)null, out undo);
            }
            Before.Remove(order);
            Add(new EditEntry { Turn = Sim.Turn, Label = label, Origin = origin, Outcome = outcome, Reason = reason, Record = record, Undo = undo });
        }

        public static void Add(EditEntry entry)
        {
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
                Add(new EditEntry { Turn = Sim.Turn, Label = "undo " + entry.Label, Origin = EditOrigin.Undo, Outcome = EditOutcome.Skipped, Reason = problem });
                return;
            }
            entry.Undone = true;
            Orders.PostAny(order, "undo " + entry.Label, EditOrigin.Undo);
        }
    }
}
