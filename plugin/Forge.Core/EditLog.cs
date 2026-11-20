using System.Collections.Generic;
using System.Linq;

namespace Forge.Core
{
    public enum EditOutcome
    {
        Posted,
        Rejected,
        Failed,
        Skipped,
    }

    public sealed class EditEntry
    {
        public int Turn;
        public string Label;
        public EditOutcome Outcome;
        public string Reason;
        public OrderRecord Record;

        /// <summary>The opposite order, when the game has one; null means no Undo button.</summary>
        public OrderRecord Undo;

        public bool Undone;
    }

    /// <summary>Most recent edits, oldest dropped first once the capacity is reached.</summary>
    public sealed class EditLog
    {
        private readonly int capacity;
        private readonly LinkedList<EditEntry> entries = new LinkedList<EditEntry>();

        public EditLog(int capacity = 200) => this.capacity = capacity < 1 ? 1 : capacity;

        public int Count => entries.Count;

        public int TotalAdded { get; private set; }

        public void Add(EditEntry entry)
        {
            entries.AddLast(entry);
            TotalAdded++;
            while (entries.Count > capacity)
            {
                entries.RemoveFirst();
            }
        }

        /// <summary>Newest first.</summary>
        public IReadOnlyList<EditEntry> Latest(int count) => entries.Reverse().Take(count).ToList();
    }
}
