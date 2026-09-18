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
            Widgets.Button(chosen == null ? "Pick a unit" : "Spawn army", () => World.SpawnArmy(chosen, tile), chosen != null && tile >= 0);
            GUILayout.BeginHorizontal();
            Widgets.Button("Create city", () => World.CreateCity(tile), tile >= 0);
            Widgets.Button("Create camp", () => World.CreateCamp(tile), tile >= 0);
            GUILayout.EndHorizontal();

            Widgets.Section("Armies");
            GUILayout.BeginHorizontal();
            Widgets.Button("Refresh armies", () => { armies = World.Armies(); selected = 0; });
            if (armies.Count > 0)
            {
                selected = Mathf.Clamp(selected, 0, armies.Count - 1);
                if (GUILayout.Button(armies[selected].Name + "   ▼"))
                {
                    selected = (selected + 1) % armies.Count;
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            Widgets.Button("Teleport here", () => World.Teleport(armies[selected], tile), armies.Count > 0 && tile >= 0);
            var hasSpeed = Widgets.IntField("Speed", ref speed, out var value, 60, 50);
            Widgets.Button("Set speed", () => World.SetSpeed(armies[selected], value), armies.Count > 0 && hasSpeed);
            GUILayout.EndHorizontal();

            Widgets.Section("Map");
            GUILayout.BeginHorizontal();
            Widgets.Button("Reveal entire map", World.RevealMap);
            Widgets.Button("Collect all curiosities", World.CollectCuriosities);
            GUILayout.EndHorizontal();
        }
    }
}
