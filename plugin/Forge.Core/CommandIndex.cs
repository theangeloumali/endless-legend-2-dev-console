using System;
using System.Collections.Generic;
using System.Linq;

namespace Forge.Core
{
    /// <summary>Ranks commands for the search palette: a word starting with the query beats the query appearing
    /// mid-word, which beats a hit on the location only. Ties sort alphabetically.</summary>
    public sealed class CommandIndex<T>
    {
        private sealed class Item
        {
            public T Value;
            public string Label;
            public string Location;
        }

        private readonly List<Item> items = new List<Item>();

        public int Count => items.Count;

        public void Clear() => items.Clear();

        public void Add(T value, string label, string location) =>
            items.Add(new Item { Value = value, Label = label ?? string.Empty, Location = location ?? string.Empty });

        public IReadOnlyList<T> Search(string query, int max = 8)
        {
            query = query?.Trim() ?? string.Empty;
            if (query.Length == 0)
            {
                return new T[0];
            }
            return items.Select(item => (item, score: Score(item, query)))
                        .Where(hit => hit.score > 0)
                        .OrderByDescending(hit => hit.score)
                        .ThenBy(hit => hit.item.Label, StringComparer.OrdinalIgnoreCase)
                        .Take(max)
                        .Select(hit => hit.item.Value)
                        .ToList();
        }

        private static int Score(Item item, string query)
        {
            if (StartsAnyWord(item.Label, query))
            {
                return 3;
            }
            if (item.Label.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return 2;
            }
            if (item.Location.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return 1;
            }
            return 0;
        }

        private static bool StartsAnyWord(string text, string query)
        {
            for (var i = 0; i < text.Length; i++)
            {
                var wordStart = i == 0 || !char.IsLetterOrDigit(text[i - 1]);
                if (wordStart && string.Compare(text, i, query, 0, query.Length, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
