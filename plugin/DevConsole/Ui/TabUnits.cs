using System.Globalization;
using DevConsole.Cheats;
using UnityEngine;

namespace DevConsole.Ui
{
    /// <summary>Pick any army on the map, then add, remove, evolve or damage its units, change its movement,
    /// send it through a burrow, or destroy it.</summary>
    internal sealed class TabUnits : ITab
    {
        private readonly Widgets.Dropdown addUnit = new Widgets.Dropdown("Unit to add");
        private readonly Widgets.Dropdown evolveInto = new Widgets.Dropdown("Evolve into");
        private Units.ArmyPick army;
        private int unitIndex;
        private string damage = "50";
        private string ratio = "2";

        public string Title => "Units";

        public void Draw()
        {
            Widgets.Section("Pick an army");
            Widgets.Plot("Pick army on the map", Pick);
            var has = army != null;
            GUILayout.BeginHorizontal();
            GUILayout.Label("Army", Theme.Label, GUILayout.Width(Widgets.LabelWidth));
            GUILayout.Label(has ? $"{army.Name} · tile #{army.Tile} · empire #{army.Owner} · {army.Units.Count} units" : "none: pick one on the map",
                            has ? Theme.Value : Theme.Hint);
            GUILayout.EndHorizontal();
            Widgets.Button("Reload army", () => army = Units.At(army.Tile), has);
            Widgets.Hint("Works on any empire's army, AI included.");

            Widgets.Section("Units in this army");
            if (has)
            {
                for (var i = 0; i < army.Units.Count; i++)
                {
                    var unit = army.Units[i];
                    var index = i;
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button($"{i + 1}. {unit.Name}", i == unitIndex ? Theme.TabActive : Theme.Button, GUILayout.Height(25)))
                    {
                        unitIndex = i;
                    }
                    GUILayout.Label($"{unit.Health}/{unit.MaxHealth} HP", Theme.Value, GUILayout.Width(110));
                    Widgets.Button("Remove", () => { Units.Remove(army, index); army = Units.At(army.Tile); });
                    GUILayout.EndHorizontal();
                }
            }
            var selected = has && unitIndex < army.Units.Count ? army.Units[unitIndex] : null;
            evolveInto.Draw(Catalog.Units, 150f);
            var target = evolveInto.Selected(Catalog.Units);
            Widgets.Button(selected == null || target == null ? "Pick a unit and what it evolves into" : $"Evolve {selected.Name} into {target.Display}",
                           () => Units.Evolve(army, selected, target.Name), selected != null && target != null);
            Widgets.Hint("Only evolutions the unit's upgrade tree allows are accepted; the edit log shows a rejection.");

            Widgets.Section("Add to this army");
            addUnit.Draw(Catalog.Units, 150f);
            var chosen = addUnit.Selected(Catalog.Units);
            Widgets.Button(chosen == null ? "Pick a unit" : "Add " + chosen.Display, () => { Units.Add(army, chosen.Name); army = Units.At(army.Tile); },
                           has && chosen != null);

            Widgets.Section("Combat and movement");
            Widgets.ValueButton("Damage", ref damage, "Damage every unit", amount => Units.Damage(army, amount), has);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Movement ratio", Theme.Label, GUILayout.Width(Widgets.LabelWidth));
            ratio = GUILayout.TextField(ratio, Theme.Field, GUILayout.Width(80));
            var ratioOk = float.TryParse(ratio, NumberStyles.Float, CultureInfo.InvariantCulture, out var movement) && movement > 0f;
            Widgets.Button("Set movement", () => Units.SetMovementRatio(army, movement), has && ratioOk);
            GUILayout.EndHorizontal();
            Widgets.Plot("Travel by burrow (click the exit burrow)", t => Units.EnterBurrow(army, t), has);

            Widgets.Section("Danger zone");
            Widgets.DangerButton(has ? "Destroy " + army.Name : "Destroy army", () => { Units.Destroy(army); army = null; }, has);
        }

        private void Pick(int tile)
        {
            army = Units.At(tile);
            unitIndex = 0;
            if (army == null)
            {
                Orders.Log.LogWarning($"no army on tile {tile}");
            }
            Cheats.World.Disarm();  // one pick, not a repeating tool
        }
    }
}
