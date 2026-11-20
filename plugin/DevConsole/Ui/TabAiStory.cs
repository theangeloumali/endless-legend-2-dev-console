using System.Collections.Generic;
using System.Linq;
using Amplitude.Mercury.Data.Simulation;
using Amplitude.Mercury.Simulation;
using DevConsole.Cheats;
using UnityEngine;

namespace DevConsole.Ui
{
    /// <summary>Other empires and the story: command any army, moods, dialogs, dungeons, heroes, minor
    /// factions, empire state, council seats and the tech queue. Most actions take the "acting empire".</summary>
    internal sealed class TabAiStory : ITab
    {
        private readonly Widgets.Dropdown actingPicker = new Widgets.Dropdown("Acting empire");
        private readonly Widgets.Dropdown otherPicker = new Widgets.Dropdown("Other empire");
        private readonly Widgets.Dropdown mood = new Widgets.Dropdown("Mood message");
        private readonly Widgets.Dropdown dialog = new Widgets.Dropdown("Dialog");
        private readonly Widgets.Dropdown heroPicker = new Widgets.Dropdown("Hero");
        private readonly Widgets.Dropdown seatPicker = new Widgets.Dropdown("Council seat");
        private readonly Widgets.Dropdown technology = new Widgets.Dropdown("Technology");
        private readonly Widgets.EnumPicker<ArmyMissionTypes> mission = new Widgets.EnumPicker<ArmyMissionTypes>("Mission");
        private readonly Widgets.EnumPicker<EmpireActionType> empireAction = new Widgets.EnumPicker<EmpireActionType>("Empire action");
        private readonly Widgets.EnumPicker<BypassSGELevelType> sgeLevel = new Widgets.EnumPicker<BypassSGELevelType>("SGE level");
        private readonly Widgets.EnumPicker<EnqueuePosition> techPosition = new Widgets.EnumPicker<EnqueuePosition>("Queue position");
        private List<Sim.EmpireRow> empires = new List<Sim.EmpireRow>();
        private List<Heroes.Entry> heroes = new List<Heroes.Entry>();
        private List<AiStory.SeatRow> seats = new List<AiStory.SeatRow>();
        private int acting, other, hero, seat;
        private Units.ArmyPick army;
        private string seed = "1";
        private string minorFaction = "0";
        private string traitIndex = "0";
        private bool cohabitation;
        private bool clearExcluded;
        private string empireState = string.Empty;

        public string Title => "AI & Story";

        public void Draw()
        {
            Widgets.Section("Empires");
            Widgets.Button("Refresh empires, heroes and seats", Refresh);
            if (empires.Count > 0)
            {
                var pickedActing = actingPicker.Draw(empires, acting, row => row.Name);
                if (pickedActing != acting)
                {
                    acting = pickedActing;
                    RefreshActing();
                }
                other = otherPicker.Draw(empires, other, row => row.Name);
            }
            var hasEmpires = empires.Count > 0;
            var actingIndex = hasEmpires ? empires[acting].Index : Sim.LocalEmpireIndex;
            var otherIndex = hasEmpires ? empires[other].Index : -1;

            Widgets.Section("Command any army (AI included)");
            Widgets.Pick("Pick army on the map", t => army = Units.At(t));
            Widgets.Hint(army == null ? "No army picked." : $"{army.Name} · empire #{army.Owner} · tile #{army.Tile}");
            mission.Draw();
            Widgets.Plot($"Set mission {mission.Value} (click the target tile)", t => AiStory.SetArmyMission(army, mission.Value, t), army != null);
            Widgets.Plot("Attack the city I click", t => AiStory.AttackCity(army, t), army != null);

            Widgets.Section("Relations: acting → other");
            mood.Draw(Catalog.MoodMessages, 150f);
            var chosenMood = mood.Selected(Catalog.MoodMessages);
            Widgets.Button(chosenMood == null ? "Pick a mood message" : "Send " + chosenMood.Display,
                           () => AiStory.MoodMessage(actingIndex, otherIndex, chosenMood.Name), chosenMood != null && otherIndex >= 0);
            clearExcluded = GUILayout.Toggle(clearExcluded, " also clear the exclusion list", Theme.Toggle);
            Widgets.Button("Clear mood cooldown", () => AiStory.ClearMoodCooldown(actingIndex, otherIndex, clearExcluded), otherIndex >= 0);

            Widgets.Section("Story");
            dialog.Draw(Catalog.Dialogs, 150f);
            var chosenDialog = dialog.Selected(Catalog.Dialogs);
            Widgets.Button(chosenDialog == null ? "Pick a dialog" : "Start " + chosenDialog.Display,
                           () => AiStory.StartDialog(chosenDialog.Name, actingIndex), chosenDialog != null);
            GUILayout.BeginHorizontal();
            var seedOk = Widgets.IntField("Dungeon seed", ref seed, out var seedValue, fieldWidth: 80);
            GUILayout.EndHorizontal();
            Widgets.PlotRow(("Clear dungeon", AiStory.ClearDungeon),
                            ("Regenerate dungeon", t => { if (seedOk) AiStory.RegenerateDungeon(t, seedValue); }),
                            ("Continue crawl", AiStory.ContinueCrawl));

            Widgets.Section("Heroes of the acting empire");
            if (heroes.Count > 0)
            {
                hero = heroPicker.Draw(heroes, hero, h => h.Label);
            }
            var hasHero = hero < heroes.Count;
            // orders take the hero's global Index, not its position in this list
            Widgets.Plot("Deploy hero at the clicked tile", t => AiStory.DeployHero(heroes[hero].Index, actingIndex, t), hasHero);
            Widgets.Plot("Build partner den at the clicked tile", t => AiStory.PartnerDen(actingIndex, heroes[hero].Handle, t), hasHero);
            Widgets.DangerButton("Dismiss hero", () => AiStory.DismissHero(heroes[hero].Index, actingIndex), hasHero);
            Widgets.Button("Redistribute heroes", AiStory.RedistributeHeroes);

            Widgets.Section("Minor factions");
            GUILayout.BeginHorizontal();
            var minorOk = Widgets.IntField("Minor faction #", ref minorFaction, out var minorIndex, fieldWidth: 60);
            var traitOk = Widgets.IntField("Trait #", ref traitIndex, out var trait, 60, 60);
            GUILayout.EndHorizontal();
            cohabitation = GUILayout.Toggle(cohabitation, " cohabitation trait (off: assimilation trait)", Theme.Toggle);
            Widgets.Button("Replace trait", () => AiStory.ReplaceMinorTrait(minorIndex, trait, cohabitation), minorOk && traitOk);

            Widgets.Section("Empire state of the acting empire");
            empireState = Widgets.TextRow("State definition", empireState);
            Widgets.Button("Set empire state", () => AiStory.SetEmpireState(empireState.Trim(), actingIndex), empireState.Trim().Length > 0);
            Widgets.Hint("Use the element name from the game's EmpireState data; the edit log shows a rejection if it does not exist.");
            empireAction.Draw();
            Widgets.Button("Make " + empireAction.Value + " ready", () => AiStory.ReadyEmpireAction(empireAction.Value, actingIndex));
            sgeLevel.Draw();
            Widgets.Button("Bypass SGE level " + sgeLevel.Value, () => AiStory.BypassSgeLevel(actingIndex, sgeLevel.Value));
            if (seats.Count > 0)
            {
                seat = seatPicker.Draw(seats, seat, row => row.Name + (row.Locked ? "  (locked)" : string.Empty));
            }
            Widgets.Button("Unlock council seat", () => AiStory.UnlockSeat(seats[seat], actingIndex), seat < seats.Count);

            Widgets.Section("Technology queue of the acting empire");
            technology.Draw(Catalog.Technologies, 150f);
            var chosenTech = technology.Selected(Catalog.Technologies);
            techPosition.Draw();
            Widgets.Button(chosenTech == null ? "Pick a technology" : "Enqueue " + chosenTech.Display,
                           () => AiStory.EnqueueTechnology(actingIndex, chosenTech.Name, techPosition.Value), chosenTech != null);
            Widgets.DangerButton("Clear technology queue", () => AiStory.ClearTechQueue(actingIndex));
        }

        private void Refresh()
        {
            empires = Sim.MajorEmpires();
            acting = Mathf.Max(0, empires.FindIndex(row => Sim.IsLocal(row.Index)));
            other = empires.Count > 1 ? (acting == 0 ? 1 : 0) : 0;
            RefreshActing();
        }

        private void RefreshActing()
        {
            var empire = acting < empires.Count ? empires[acting].Entity : null;
            heroes = empire == null ? new List<Heroes.Entry>() : Heroes.Roster(empire);
            seats = AiStory.Seats(empire);
            hero = 0;
            seat = 0;
        }
    }
}
