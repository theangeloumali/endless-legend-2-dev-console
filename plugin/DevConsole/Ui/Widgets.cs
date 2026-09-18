using System;
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

        public static void Button(string label, Action action, bool enabled = true)
        {
            GUI.enabled = enabled;
            if (GUILayout.Button(label, GUILayout.Height(26)))
            {
                action();
            }
            GUI.enabled = true;
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

        /// <summary>A labelled text field that only reports a value when it parses and is positive.</summary>
        public static bool IntField(string label, ref string buffer, out int value, int labelWidth = 120, int fieldWidth = 110)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(labelWidth));
            buffer = GUILayout.TextField(buffer ?? string.Empty, GUILayout.Width(fieldWidth));
            GUILayout.EndHorizontal();
            return int.TryParse(buffer, out value) && value >= 0;
        }

        /// <summary>IMGUI has no combo box. This draws the current choice as a button that expands into a
        /// filterable, scrolling list — the only practical shape for catalogues of 177+ entries.</summary>
        public sealed class Dropdown
        {
            private readonly string label;
            private Vector2 scroll;
            private string filter = string.Empty;
            private bool open;

            public int Index;

            public Dropdown(string label) => this.label = label;

            public string Selected(string[] items) => items != null && Index >= 0 && Index < items.Length ? items[Index] : null;

            public void Draw(string[] items, float listHeight = 160f)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(label, GUILayout.Width(110));
                var current = Selected(items) ?? (items == null || items.Length == 0 ? "(none loaded)" : "(select)");
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
                    if (filter.Length > 0 && items[i].IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }
                    if (GUILayout.Button(items[i]))
                    {
                        Index = i;
                        open = false;
                    }
                }
                GUILayout.EndScrollView();
            }
        }
    }
}
