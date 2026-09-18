using System.Collections.Generic;
using DevConsole.Cheats;
using UnityEngine;

namespace DevConsole.Ui
{
    /// <summary>World editing. Tile tools arm on press, then place on every map click until cancelled.</summary>
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
            Widgets.TileTarget();

            Widgets.Section("Spawn at the hovered tile");
            unit.Draw(Catalog.Units);
            var chosen = unit.Selected(Catalog.Units);
            Widgets.Plot(chosen == null ? "Pick a unit" : "Spawn " + chosen.Display,
                         t => World.SpawnArmy(chosen.Name, t), chosen != null);
            Widgets.PlotRow(("Create city", World.CreateCity), ("Create camp", World.CreateCamp));

            Widgets.Section("Build at the hovered tile");
            district.Draw(Catalog.Districts, 140f);
            var chosenDistrict = district.Selected(Catalog.Districts);
            Widgets.Plot(chosenDistrict == null ? "Pick a district" : "Create " + chosenDistrict.Display,
                         t => World.CreateDistrict(chosenDistrict.Name, t), chosenDistrict != null);
            wonder.Draw(Catalog.Wonders, 140f);
            var chosenWonder = wonder.Selected(Catalog.Wonders);
            Widgets.Plot(chosenWonder == null ? "Pick a wonder" : "Create " + chosenWonder.Display,
                         t => World.CreateWonder(chosenWonder.Name, t), chosenWonder != null);

            Widgets.Section("Terraform the hovered tile");
            Widgets.PlotRow(("Plant forest", World.PlantForest), ("Cut forest", World.CutForest),
                            ("Clear mountain", World.ClearMountain));
            Widgets.PlotRow(("Bridge", World.BuildBridge), ("Dam", World.BuildDam),
                            ("Raise sand ruin", World.RaiseSandRuin));

            Widgets.Section("Villages at the hovered tile");
            Widgets.PlotRow(("Create", World.CreateVillage), ("Pacify", World.PacifyVillage),
                            ("Spawn from", World.SpawnFromVillage), ("Destroy", World.DestroyVillage));

            Widgets.Section("Armies");
            Widgets.Button("Refresh armies", () => { armies = World.Armies(); selected = 0; });
            if (armies.Count > 0)
            {
                selected = Widgets.Picker("Army", armies, selected, army => army.Name);
            }
            Widgets.Plot("Teleport selected army", t => World.Teleport(armies[selected], t), armies.Count > 0);
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
