using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace DevConsole.Ui
{
    /// <summary>IMGUI helpers shared by every tab. Kept free of game types so tabs stay readable.</summary>
    internal static class Widgets
    {
        public static void Section(string title)
        {
            GUILayout.Space(6);
            GUILayout.Label(title, GUI.skin.box);
        }

        /// <summary>Restores the previous GUI.enabled rather than forcing true, so a disabled block stays disabled.</summary>
        public static void Button(string label, Action action, bool enabled = true)
        {
            var previous = GUI.enabled;
            GUI.enabled = previous && enabled;
            if (GUILayout.Button(label, GUILayout.Height(26)))
            {
                action();
            }
            GUI.enabled = previous;
        }

        public static void Row(params (string Label, Action Action)[] buttons)
        {
            GUILayout.BeginHorizontal();
            foreach (var (label, action) in buttons)
            {
                Button(label, action);
            }
            GUILayout.EndHorizontal();
        }

        public static void Toggle(ConfigEntry<bool> entry) =>
            entry.Value = GUILayout.Toggle(entry.Value, " " + entry.Description.Description);

        /// <summary>A labelled text field that only reports a value when it parses and is not negative.</summary>
        public static bool IntField(string label, ref string buffer, out int value, int labelWidth = 120, int fieldWidth = 110)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(labelWidth));
            buffer = GUILayout.TextField(buffer ?? string.Empty, GUILayout.Width(fieldWidth));
            GUILayout.EndHorizontal();
            return int.TryParse(buffer, out value) && value >= 0;
        }

        /// <summary>A number field and the button that applies it, the shape most controls here need.</summary>
        public static void ValueButton(string label, ref string buffer, string action, Action<int> apply,
                                       bool enabled = true, int labelWidth = 120, int fieldWidth = 110)
        {
            GUILayout.BeginHorizontal();
            var parsed = IntField(label, ref buffer, out var value, labelWidth, fieldWidth);
            Button(action, () => apply(value), parsed && enabled);
            GUILayout.EndHorizontal();
        }

        /// <summary>Cycles through a short list in place. Used where a full dropdown would cost more room than the
        /// handful of entries (heroes, settlements, armies) are worth.</summary>
        public static int Picker<T>(string label, IList<T> items, int index, Func<T, string> describe)
        {
            if (items == null || items.Count == 0)
            {
                return 0;
            }
            index = Mathf.Clamp(index, 0, items.Count - 1);
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(110));
            if (GUILayout.Button(describe(items[index]) + $"   ({index + 1}/{items.Count})  ▼"))
            {
                index = (index + 1) % items.Count;
            }
            GUILayout.EndHorizontal();
            return index;
        }

        /// <summary>IMGUI has no combo box. This draws the current choice as a button that expands into a
        /// filterable, scrolling list — the only practical shape for the larger definition catalogues.
        /// Rows show the player-facing title and icon; the caller reads Name for the order.</summary>
        public sealed class Dropdown
        {
            private readonly string label;
            private Vector2 scroll;
            private string filter = string.Empty;
            private bool open;

            public int Index;

            public Dropdown(string label) => this.label = label;

            public Catalog.Entry Selected(Catalog.Entry[] items) =>
                items != null && Index >= 0 && Index < items.Length ? items[Index] : null;

            public void Draw(Catalog.Entry[] items, float listHeight = 200f)
            {
                var selected = Selected(items);
                GUILayout.BeginHorizontal();
                GUILayout.Label(label, GUILayout.Width(110));
                var current = selected?.Display ?? (items == null || items.Length == 0 ? "(none loaded)" : "(select)");
                if (GUILayout.Button(current + "   ▼"))
                {
                    open = !open;
                }
                GUILayout.EndHorizontal();
                if (!open || items == null || items.Length == 0)
                {
                    return;
                }
                GUILayout.BeginHorizontal();
                GUILayout.Label("filter", GUILayout.Width(110));
                filter = GUILayout.TextField(filter);
                GUILayout.EndHorizontal();
                scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(listHeight));
                for (var i = 0; i < items.Length; i++)
                {
                    if (!Matches(items[i], filter))
                    {
                        continue;
                    }
                    if (GUILayout.Button(items[i].Display))
                    {
                        Index = i;
                        open = false;
                    }
                }
                GUILayout.EndScrollView();
            }

            /// <summary>Matches the title or the element name, so a known internal name still finds its entry.</summary>
            public static bool Matches(Catalog.Entry entry, string filter) =>
                filter.Length == 0
                || entry.Display.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
                || entry.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
