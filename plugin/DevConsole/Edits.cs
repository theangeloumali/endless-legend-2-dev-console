using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Forge.Core;
using UnityEngine;

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

        /// <summary>Raised when an entry's undo reached the game, so a recording can drop that step.</summary>
        public static event Action<EditEntry> Undid;

        /// <summary>Undos that destroy an entity: which lookup finds what the original edit created.</summary>
        private static readonly Dictionary<string, Func<int, object>> Destroying = new Dictionary<string, Func<int, object>>
        {
            ["EditorOrderDestroyArmy"] = Sim.ArmyAt,
            ["EditorOrderDestroySettlement"] = Sim.SettlementAt,
        };

        private const int CreatedLookupFrames = 120;  // orders apply on the simulation thread a few frames later
        private static readonly List<(EditEntry Entry, int Until, ulong? Before)> AwaitingCreated = new List<(EditEntry, int, ulong?)>();

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
            var entry = new EditEntry { Turn = Sim.Turn, Session = Sim.Session, Label = label, Origin = origin, Outcome = outcome, Reason = reason, Record = record, Undo = undo };
            if (undo != null && Destroying.ContainsKey(undo.Type))
            {
                // the order is queued but not applied yet, so whatever stands on the tile now is not ours
                var before = OnUndoTile(entry);
                AwaitingCreated.Add((entry, Time.frameCount + CreatedLookupFrames, before == null ? (ulong?)null : (ulong)Sim.GuidOf(before)));
            }
            Add(entry);
        }

        /// <summary>Called every frame: records the GUID of each newly created army or settlement once it appears.</summary>
        public static void Tick()
        {
            for (var i = AwaitingCreated.Count - 1; i >= 0; i--)
            {
                var (entry, until, before) = AwaitingCreated[i];
                var created = OnUndoTile(entry);
                if (created != null && (ulong)Sim.GuidOf(created) == before)
                {
                    created = null;
                }
                if (created != null)
                {
                    entry.Created = (ulong)Sim.GuidOf(created);
                }
                if (created != null || Time.frameCount > until)
                {
                    AwaitingCreated.RemoveAt(i);
                }
            }
        }

        private static object OnUndoTile(EditEntry entry)
        {
            foreach (var tile in entry.Undo.Tiles.Values)
            {
                return Destroying[entry.Undo.Type](tile);
            }
            return null;
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
            if (entry.Session != Sim.Session)
            {
                Skip(entry, "that edit was made in another game");
                return;
            }
            if (Destroying.ContainsKey(entry.Undo.Type))
            {
                var now = OnUndoTile(entry);
                if (entry.Created == null || now == null || (ulong)Sim.GuidOf(now) != entry.Created)
                {
                    Skip(entry, "what this edit created is no longer on that tile, so nothing was destroyed");
                    return;
                }
            }
            var order = OrderCodec.Build(entry.Undo, out var problem);
            if (order == null)
            {
                Skip(entry, problem);
                return;
            }
            entry.Undone = Orders.PostAny(order, "undo " + entry.Label, EditOrigin.Undo);
            if (entry.Undone)
            {
                Undid?.Invoke(entry);
            }
        }

        private static void Skip(EditEntry entry, string reason) =>
            Add(new EditEntry { Turn = Sim.Turn, Label = "undo " + entry.Label, Origin = EditOrigin.Undo, Outcome = EditOutcome.Skipped, Reason = reason });
    }
}
