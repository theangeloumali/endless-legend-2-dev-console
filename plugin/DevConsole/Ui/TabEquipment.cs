using System.Collections.Generic;
using System.Linq;
using DevConsole.Cheats;
using UnityEngine;

namespace DevConsole.Ui
{
    /// <summary>Equipment screen: conjure any definition into the stash, then equip it to a chosen hero.</summary>
    internal sealed class TabEquipment : ITab
    {
        private readonly Widgets.Dropdown definition = new Widgets.Dropdown("Equipment");
        private List<Equipment.Item> stash = new List<Equipment.Item>();
        private List<Heroes.Entry> roster = new List<Heroes.Entry>();
        private Vector2 scroll;
        private string filter = string.Empty;
        private int hero;

        public string Title => "Equipment";

        /// <summary>Orders apply asynchronously, so a just-added item may need one more Refresh to appear.</summary>
        private void Refresh()
        {
            stash = Equipment.Stash();
            roster = Heroes.Roster();
        }

        public void Draw()
        {
            GUILayout.BeginHorizontal();
            Widgets.Button("Refresh", Refresh);
            GUILayout.Label($"{stash.Count} in stash  ·  {Catalog.Equipment.Length} items exist", Theme.Value);
            GUILayout.EndHorizontal();

            if (roster.Count > 0)
            {
                hero = Widgets.Picker("Hero", roster, hero, entry => entry.Label);
            }

            Widgets.Section("Add to stash");
            definition.Draw(Catalog.Equipment);
            var chosen = definition.Selected(Catalog.Equipment);
            Widgets.Button(chosen == null ? "Pick a definition" : "Add " + chosen.Display, () => { Equipment.Add(chosen.Name); Refresh(); }, chosen != null);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Bulk filter", GUILayout.Width(110));
            filter = GUILayout.TextField(filter);
            GUILayout.EndHorizontal();
            var matching = filter.Length == 0
                ? new string[0]
                : Catalog.Equipment.Where(entry => Widgets.Dropdown.Matches(entry, filter)).Select(entry => entry.Name).ToArray();
            Widgets.Button($"Add every item matching the filter ({matching.Length})",
                           () => { Equipment.AddAll(matching); Refresh(); }, matching.Length > 0);

            Widgets.Section("Equipped");
            foreach (var slot in Equipment.Slots)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(slot.ToString(), GUILayout.Width(110));
                Widgets.Button("Unequip", () => { Equipment.Unequip(slot, roster[hero]); Refresh(); }, roster.Count > 0);
                GUILayout.EndHorizontal();
            }

            Widgets.Section("Stash");
            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(200));
            foreach (var item in stash)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(item.Definition);
                Widgets.Button("Equip", () => { Equipment.Equip(item, roster[hero]); Refresh(); }, roster.Count > 0);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            Widgets.Button("Clear ALL equipment in the empire", () => { Equipment.ClearAll(); Refresh(); });
        }
    }
}
