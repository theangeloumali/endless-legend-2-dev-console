using System.Collections.Generic;
using DevConsole.Cheats;
using UnityEngine;

namespace DevConsole.Ui
{
    /// <summary>World editing, aimed at whatever tile the mouse is over.</summary>
    internal sealed class TabWorld : ITab
    {
        private readonly Widgets.Dropdown unit = new Widgets.Dropdown("Unit");
        private List<World.ArmyEntry> armies = new List<World.ArmyEntry>();
        private string speed = "99";
        private int selected;

        public string Title => "World";

        public void Draw()
        {
            var tile = World.HoveredTile;
            GUILayout.Label(tile >= 0 ? $"Hovered tile  #{tile}" : "Hover a tile on the map to target it", GUI.skin.box);

            Widgets.Section("Spawn at the hovered tile");
            unit.Draw(Catalog.Units);
            var chosen = unit.Selected(Catalog.Units);
            Widgets.Button(chosen == null ? "Pick a unit" : "Spawn " + chosen.Display, () => World.SpawnArmy(chosen.Name, tile), chosen != null && tile >= 0);
            GUILayout.BeginHorizontal();
            Widgets.Button("Create city", () => World.CreateCity(tile), tile >= 0);
            Widgets.Button("Create camp", () => World.CreateCamp(tile), tile >= 0);
            GUILayout.EndHorizontal();

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
