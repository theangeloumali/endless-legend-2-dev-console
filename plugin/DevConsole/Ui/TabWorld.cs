using System.Collections.Generic;
using DevConsole.Cheats;
using UnityEngine;

namespace DevConsole.Ui
{
    /// <summary>World editing, aimed at whatever tile the mouse is over.</summary>
    internal sealed class TabWorld : ITab
    {
        private readonly Widgets.Dropdown unit = new Widgets.Dropdown("Unit");
        private readonly Widgets.Dropdown district = new Widgets.Dropdown("District");
        private readonly Widgets.Dropdown wonder = new Widgets.Dropdown("Wonder");
        private List<World.ArmyEntry> armies = new List<World.ArmyEntry>();
        private string speed = "99";
        private int selected;

        public string Title => "World";

        public void Draw()
        {
            var tile = World.HoveredTile;
            GUILayout.BeginHorizontal();
            GUILayout.Label("Hovered tile", Theme.Label, GUILayout.Width(Widgets.LabelWidth));
            GUILayout.Label(tile >= 0 ? $"#{tile}" : "move the mouse over the map", tile >= 0 ? Theme.Value : Theme.Hint);
            GUILayout.EndHorizontal();

            Widgets.Section("Spawn at the hovered tile");
            unit.Draw(Catalog.Units);
            var chosen = unit.Selected(Catalog.Units);
            Widgets.Button(chosen == null ? "Pick a unit" : "Spawn " + chosen.Display, () => World.SpawnArmy(chosen.Name, tile), chosen != null && tile >= 0);
            GUILayout.BeginHorizontal();
            Widgets.Button("Create city", () => World.CreateCity(tile), tile >= 0);
            Widgets.Button("Create camp", () => World.CreateCamp(tile), tile >= 0);
            GUILayout.EndHorizontal();

            Widgets.Section("Build at the hovered tile");
            district.Draw(Catalog.Districts, 140f);
            var chosenDistrict = district.Selected(Catalog.Districts);
            Widgets.Button(chosenDistrict == null ? "Pick a district" : "Create " + chosenDistrict.Display,
                           () => World.CreateDistrict(chosenDistrict.Name, tile), chosenDistrict != null && tile >= 0);
            wonder.Draw(Catalog.Wonders, 140f);
            var chosenWonder = wonder.Selected(Catalog.Wonders);
            Widgets.Button(chosenWonder == null ? "Pick a wonder" : "Create " + chosenWonder.Display,
                           () => World.CreateWonder(chosenWonder.Name, tile), chosenWonder != null && tile >= 0);

            Widgets.Section("Terraform the hovered tile");
            GUI.enabled = tile >= 0;
            Widgets.Row(("Plant forest", () => World.PlantForest(tile)),
                        ("Cut forest", () => World.CutForest(tile)),
                        ("Clear mountain", () => World.ClearMountain(tile)));
            Widgets.Row(("Bridge", () => World.BuildBridge(tile)),
                        ("Dam", () => World.BuildDam(tile)),
                        ("Raise sand ruin", () => World.RaiseSandRuin(tile)));
            GUI.enabled = true;

            Widgets.Section("Villages at the hovered tile");
            GUI.enabled = tile >= 0;
            Widgets.Row(("Create", () => World.CreateVillage(tile)),
                        ("Pacify", () => World.PacifyVillage(tile)),
                        ("Spawn from", () => World.SpawnFromVillage(tile)),
                        ("Destroy", () => World.DestroyVillage(tile)));
            GUI.enabled = true;

            Widgets.Section("Armies");
            Widgets.Button("Refresh armies", () => { armies = World.Armies(); selected = 0; });
            if (armies.Count > 0)
            {
                selected = Widgets.Picker("Army", armies, selected, army => army.Name);
            }
            Widgets.Button("Teleport selected army here", () => World.Teleport(armies[selected], tile), armies.Count > 0 && tile >= 0);
            Widgets.ValueButton("God speed", ref speed, "Set speed",
                                value => World.SetSpeed(armies[selected], value), armies.Count > 0);

            Widgets.Section("Map");
            GUILayout.BeginHorizontal();
            Widgets.Button("Reveal entire map", World.RevealMap);
            Widgets.Button("Collect all curiosities", World.CollectCuriosities);
            GUILayout.EndHorizontal();
        }
    }
}
