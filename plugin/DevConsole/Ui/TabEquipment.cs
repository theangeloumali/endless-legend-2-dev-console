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

        public void Draw()
        {
            GUILayout.BeginHorizontal();
            Widgets.Button("Refresh", () => { stash = Equipment.Stash(); roster = Heroes.Roster(); hero = 0; });
            GUILayout.Label($"{stash.Count} in stash · {Catalog.Equipment.Length} definitions", GUI.skin.box);
            GUILayout.EndHorizontal();

            if (roster.Count > 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Hero", GUILayout.Width(110));
                hero = Mathf.Clamp(hero, 0, roster.Count - 1);
                if (GUILayout.Button(roster[hero].Label + "   ▼"))
                {
                    hero = (hero + 1) % roster.Count;
                }
                GUILayout.EndHorizontal();
            }

            Widgets.Section("Add to stash");
            definition.Draw(Catalog.Equipment);
            var chosen = definition.Selected(Catalog.Equipment);
            Widgets.Button(chosen == null ? "Pick a definition" : "Add " + chosen, () => Equipment.Add(chosen), chosen != null);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Bulk filter", GUILayout.Width(110));
            filter = GUILayout.TextField(filter);
            GUILayout.EndHorizontal();
            var matching = Catalog.Equipment.Where(name => filter.Length > 0 && name.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
            Widgets.Button($"Add every definition matching the filter ({matching.Length})",
                           () => Equipment.AddAll(matching), matching.Length > 0);

            Widgets.Section("Equipped");
            foreach (var slot in Equipment.Slots)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(slot.ToString(), GUILayout.Width(110));
                Widgets.Button("Unequip", () => Equipment.Unequip(slot, roster[hero]), roster.Count > 0);
                GUILayout.EndHorizontal();
            }

            Widgets.Section("Stash");
            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(200));
            foreach (var item in stash)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(item.Definition);
                Widgets.Button("Equip", () => { Equipment.Equip(item, roster[hero]); }, roster.Count > 0);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            Widgets.Button("Clear ALL equipment in the empire", Equipment.ClearAll);
        }
    }
}
