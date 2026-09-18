using DevConsole.Cheats;
using UnityEngine;

namespace DevConsole.Ui
{
    /// <summary>Cooldowns, statuses and quests — the empire-wide levers that are not tied to one city.</summary>
    internal sealed class TabEmpire : ITab
    {
        private readonly Widgets.Dropdown status = new Widgets.Dropdown("Status");
        private readonly Widgets.Dropdown quest = new Widgets.Dropdown("Quest");
        private string duration = "0";
        private string questIndex = "0";

        public string Title => "Empire";

        public void Draw()
        {
            Widgets.Section("Cooldowns");
            Widgets.Button("Reset EVERY cooldown", EmpireCheats.ResetEverything);
            Widgets.Row(("Empire actions", EmpireCheats.ResetEmpireActions),
                        ("Culture affinity", EmpireCheats.ResetCultureAffinity),
                        ("Raise reservist", EmpireCheats.ResetRaiseReservist));
            Widgets.Row(("Steal population", EmpireCheats.ResetStealPopulation),
                        ("Merchant affinity", EmpireCheats.ResetMerchantAffinity));

            Widgets.Section("Statuses at the hovered tile");
            Widgets.TileTarget();
            status.Draw(Catalog.Statuses, 150f);
            var chosen = status.Selected(Catalog.Statuses);
            GUILayout.BeginHorizontal();
            Widgets.IntField("Duration", ref duration, out var turns);
            GUILayout.Label("turns (0 = the definition decides)", Theme.Hint);
            GUILayout.EndHorizontal();
            Widgets.Plot(chosen == null ? "Pick a status" : "Add " + chosen.Display,
                         t => EmpireCheats.AddStatus(chosen.Name, t, turns), chosen != null);
            Widgets.Plot(chosen == null ? "Pick a status" : "Remove " + chosen.Display,
                         t => EmpireCheats.RemoveStatus(chosen.Name, t), chosen != null);
            Widgets.Hint("Duration 0 lets the status definition decide how long it lasts.");

            Widgets.Section("Quests");
            quest.Draw(Catalog.Quests, 150f);
            var chosenQuest = quest.Selected(Catalog.Quests);
            Widgets.Button(chosenQuest == null ? "Pick a quest" : "Start " + chosenQuest.Display,
                           () => EmpireCheats.StartQuest(chosenQuest.Name), chosenQuest != null);
            Widgets.ValueButton("Quest index", ref questIndex, "Advance to next step", EmpireCheats.AdvanceQuest);
            Widgets.Hint("Advance works on quests you already have, numbered from 0 in the order you took them.");
        }
    }
}
