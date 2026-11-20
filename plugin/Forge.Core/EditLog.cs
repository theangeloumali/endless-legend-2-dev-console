using System.Collections.Generic;

namespace Forge.Core
{
    /// <summary>Who caused an edit: recipes record only what the user did, never undos or replays.</summary>
    public enum EditOrigin
    {
        User,
        Undo,
        Replay,
    }

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
        public EditOrigin Origin;
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

        /// <summary>Newest first; walks back from the end instead of copying the whole log.</summary>
        public IReadOnlyList<EditEntry> Latest(int count)
        {
            var latest = new List<EditEntry>(count);
            for (var node = entries.Last; node != null && latest.Count < count; node = node.Previous)
            {
                latest.Add(node.Value);
            }
            return latest;
        }
    }
}
