using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace DevConsole.Ui
{
    /// <summary>IMGUI helpers shared by every tab. Kept free of game types so tabs stay readable.</summary>
    internal static class Widgets
    {
        /// <summary>One label column width everywhere, so fields and buttons line up across every tab.</summary>
        public const int LabelWidth = 130;

        public static void Section(string title)
        {
            GUILayout.Label(title.ToUpperInvariant(), Theme.Section);
            Commands.EnterSection(title);
            if (!Commands.Collecting && Event.current.type == EventType.Repaint)
            {
                Commands.RememberSectionTop(title, GUILayoutUtility.GetLastRect().y);
            }
        }

        /// <summary>The latched map target plus its lock. Every tile action reads this, because the live hover is
        /// always invalid at the moment you click a button — the mouse is over the window by then.</summary>
        public static void TileTarget()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Target tile", Theme.Label, GUILayout.Width(LabelWidth));
            GUILayout.Label(Cheats.World.HasTile ? $"#{Cheats.World.TargetTile}" : "hover the map once",
                            Cheats.World.HasTile ? Theme.Value : Theme.Hint, GUILayout.Width(150));
            Cheats.World.Locked = GUILayout.Toggle(Cheats.World.Locked, Cheats.World.Locked ? " locked" : " follows the mouse", Theme.Toggle);
            GUILayout.EndHorizontal();
            GUILayout.Label("Press a tile tool to arm it, then click the map as many times as you like. "
                            + "Right-click or Escape cancels.", Theme.Hint);
        }

        public static void Hint(string text) => GUILayout.Label(text, Theme.Hint);

        /// <summary>Restores the previous GUI.enabled rather than forcing true, so a disabled block stays disabled.</summary>
        public static void Button(string label, Action action, bool enabled = true)
        {
            Commands.Record(label, action, null, enabled, destructive: false);
            var previous = GUI.enabled;
            GUI.enabled = previous && enabled;
            if (GUILayout.Button(label, Theme.Button, GUILayout.Height(27)))
            {
                action();
            }
            GUI.enabled = previous;
        }

        private const float ConfirmSeconds = 3f;
        private static string pendingConfirm;
        private static float pendingUntil;

        /// <summary>Destroy-style actions with no undo: the first click arms a 3-second confirm, the second runs it.</summary>
        public static void DangerButton(string label, Action action, bool enabled = true)
        {
            Commands.Record(label, action, null, enabled, destructive: true);
            var confirming = pendingConfirm == label && Time.unscaledTime < pendingUntil;
            var previous = GUI.enabled;
            GUI.enabled = previous && enabled;
            if (GUILayout.Button(confirming ? "Click again to confirm: " + label : label, Theme.Danger, GUILayout.Height(27)))
            {
                if (confirming)
                {
                    pendingConfirm = null;
                    action();
                }
                else
                {
                    pendingConfirm = label;
                    pendingUntil = Time.unscaledTime + ConfirmSeconds;
                }
            }
            GUI.enabled = previous;
        }

        /// <summary>Arms a tile tool instead of firing once: the armed button stays highlighted and every map
        /// click places another. Clicking it again cancels.</summary>
        public static void Plot(string label, Action<int> action, bool enabled = true)
        {
            Commands.Record(label, null, action, enabled, destructive: false);
            var armed = Cheats.World.IsArmed && Cheats.World.ArmedLabel == label;
            var previous = GUI.enabled;
            GUI.enabled = previous && enabled;
            if (GUILayout.Button(armed ? label + "  ●" : label, armed ? Theme.TabActive : Theme.Button, GUILayout.Height(27)))
            {
                if (armed)
                {
                    Cheats.World.Disarm();
                }
                else
                {
                    Cheats.World.Arm(label, action);
                }
            }
            GUI.enabled = previous;
        }

        public static void PlotRow(params (string Label, Action<int> Action)[] buttons)
        {
            GUILayout.BeginHorizontal();
            foreach (var (label, action) in buttons)
            {
                Plot(label, action);
            }
            GUILayout.EndHorizontal();
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

        /// <summary>A full-width switch that reads as on or off from across the room. The stock IMGUI checkbox is
        /// a 12px square, which is the wrong affordance for the handful of toggles people flip constantly.</summary>
        public static void BigToggle(string label, bool value, Action<bool> set, string note = null)
        {
            Commands.Record($"{label} ({(value ? "turn off" : "turn on")})", () => set(!value), null, true, destructive: false);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(value ? "  ON " : "  OFF", value ? Theme.TabActive : Theme.Button,
                                 GUILayout.Width(70), GUILayout.Height(34)))
            {
                set(!value);
            }
            if (GUILayout.Button(label, value ? Theme.TabActive : Theme.Button, GUILayout.Height(34)))
            {
                set(!value);
            }
            GUILayout.EndHorizontal();
            if (!string.IsNullOrEmpty(note))
            {
                GUILayout.Label(note, Theme.Hint);
            }
        }

        public static void Toggle(ConfigEntry<bool> entry) =>
            entry.Value = GUILayout.Toggle(entry.Value, entry.Description.Description, Theme.Toggle);

        /// <summary>A labelled text field that only reports a value when it parses and is not negative.</summary>
        public static bool IntField(string label, ref string buffer, out int value, int labelWidth = LabelWidth, int fieldWidth = 120)
        {
            GUILayout.Label(label, Theme.Label, GUILayout.Width(labelWidth));
            buffer = GUILayout.TextField(buffer ?? string.Empty, Theme.Field, GUILayout.Width(fieldWidth));
            return int.TryParse(buffer, out value) && value >= 0;
        }

        /// <summary>A number field and the button that applies it, the shape most controls here need.</summary>
        public static void ValueButton(string label, ref string buffer, string action, Action<int> apply,
                                       bool enabled = true, int labelWidth = LabelWidth, int fieldWidth = 120)
        {
            GUILayout.BeginHorizontal();
            var parsed = IntField(label, ref buffer, out var value, labelWidth, fieldWidth);
            // the palette shows "Dust: Set", not a bare "Set"
            Commands.Record(label + ": " + action, () => apply(value), null, parsed && enabled, destructive: false);
            var previous = GUI.enabled;
            GUI.enabled = previous && parsed && enabled;
            if (GUILayout.Button(action, Theme.Button, GUILayout.Height(27)))
            {
                apply(value);
            }
            GUI.enabled = previous;
            GUILayout.EndHorizontal();
        }

        /// <summary>IMGUI has no combo box. This draws the current choice as a button that expands into a
        /// filterable, scrolling list — the only shape that works for both a 4-hero roster and 177 items.
        /// Catalogue rows carry a tier and a rarity tint; a plain list supplies just a label.</summary>
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
                var count = items == null ? 0 : items.Length;
                var selected = Selected(items);
                var current = selected == null
                    ? (count == 0 ? "not loaded yet" : "select…")
                    : (selected.Tier == null ? selected.Display : $"{selected.Tier}   {selected.Display}");
                var picked = Expand(current, Index, count, listHeight,
                                    i => items[i].Display + " " + items[i].Name,
                                    (i, on) => Entry(items[i], on));
                if (picked >= 0)
                {
                    Index = picked;
                }
            }

            /// <summary>The same list over any collection — heroes, settlements, armies. The index stays with the
            /// caller because those lists are rebuilt by Refresh, which resets the selection.</summary>
            public int Draw<T>(IList<T> items, int index, Func<T, string> describe, float listHeight = 170f)
            {
                var count = items == null ? 0 : items.Count;
                if (count == 0)
                {
                    Expand("none loaded", 0, 0, listHeight, i => string.Empty, (i, on) => false);
                    return 0;
                }
                index = Mathf.Clamp(index, 0, count - 1);
                var picked = Expand($"{describe(items[index])}     {index + 1}/{count}", index, count, listHeight,
                                    i => describe(items[i]),
                                    (i, on) => GUILayout.Button(describe(items[i]), on ? Theme.TabActive : Theme.Button,
                                                                GUILayout.Height(25)));
                return picked >= 0 ? picked : index;
            }

            /// <summary>Header button, filter box, scrolling rows. Returns the row clicked this frame, else -1.
            /// Both Draw overloads share it, so a list can only ever expand — never cycle.</summary>
            private int Expand(string current, int index, int count, float listHeight,
                               Func<int, string> searchable, Func<int, bool, bool> row)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(label, Theme.Label, GUILayout.Width(LabelWidth));
                if (GUILayout.Button(current + "   ▼", open ? Theme.TabActive : Theme.Button, GUILayout.Height(27)))
                {
                    open = !open;
                }
                GUILayout.EndHorizontal();
                if (!open || count == 0)
                {
                    return -1;
                }
                GUILayout.BeginHorizontal();
                GUILayout.Label("Filter", Theme.Label, GUILayout.Width(LabelWidth));
                filter = GUILayout.TextField(filter, Theme.Field);
                GUILayout.EndHorizontal();
                var picked = -1;
                scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(listHeight));
                for (var i = 0; i < count; i++)
                {
                    if (filter.Length > 0 && searchable(i).IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }
                    if (row(i, i == index))
                    {
                        picked = i;
                    }
                }
                GUILayout.EndScrollView();
                if (picked >= 0)
                {
                    open = false;  // collapse on choice, the behaviour a dropdown is expected to have
                }
                return picked;
            }

            /// <summary>One row: tier on the left, name tinted by rarity. Colour comes from the game's own mapper,
            /// so Legendary reads as Legendary without hardcoding a palette.</summary>
            private static bool Entry(Catalog.Entry entry, bool selected)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(entry.Tier ?? string.Empty, Theme.Hint, GUILayout.Width(72));
                var previous = GUI.contentColor;  // contentColor tints only the text; GUI.color would stain the button too
                GUI.contentColor = entry.Tint;
                var clicked = GUILayout.Button(entry.Display, selected ? Theme.TabActive : Theme.Button, GUILayout.Height(25));
                GUI.contentColor = previous;
                GUILayout.EndHorizontal();
                return clicked;
            }

            /// <summary>Matches the title or the element name, so a known internal name still finds its entry.</summary>
            public static bool Matches(Catalog.Entry entry, string filter) =>
                filter.Length == 0
                || entry.Display.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
                || entry.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
