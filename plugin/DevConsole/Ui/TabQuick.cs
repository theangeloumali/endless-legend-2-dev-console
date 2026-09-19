using System.Linq;
using Amplitude.Mercury;
using DevConsole.Cheats;
using UnityEngine;

namespace DevConsole.Ui
{
    /// <summary>
    /// The switches people actually flip mid-game, gathered on the first tab so none of them need hunting for.
    /// Everything here also lives on its own tab; this is a shortcut, not a second source of truth.
    /// </summary>
    internal sealed class TabQuick : ITab
    {
        private static readonly (BattleCheatType Cheat, string Label)[] BattleToggles =
        {
            (BattleCheatType.InfiniteMovement, "Infinite movement"),
            (BattleCheatType.InfiniteActionToken, "Infinite battle points"),
            (BattleCheatType.InfiniteBattleSkill, "Infinite battle skills"),
            (BattleCheatType.IgnoreRoundCount, "Infinite battle — rounds never run out"),
        };

        private readonly State state;
        private string research = "10000";

        public TabQuick(State state) => this.state = state;

        public string Title => "Quick";

        public void Draw()
        {
            Widgets.Section("Battle");
            foreach (var (cheat, label) in BattleToggles)
            {
                var value = Battle.Get(cheat);
                Widgets.BigToggle(label, value, on => Battle.Set(cheat, on));
            }

            Widgets.Section("Instant — from next turn");
            Widgets.BigToggle("Instant construction", state.InstantBuild.Value, on => state.InstantBuild.Value = on);
            Widgets.BigToggle("Instant research", state.InstantResearch.Value, on => state.InstantResearch.Value = on);
            Widgets.Hint("These multiply production and science as income arrives, so they bite on your next turn.");

            Widgets.Section("Instant — right now");
            Widgets.Button("Finish every build queue  (free, immediate)", Build.CompleteEverything);
            Widgets.Button("Unlock every era of technology", Economy.UnlockAllEras);
            Widgets.ValueButton("Research", ref research, "Add now", Economy.AddResearch);
            Widgets.Hint("These apply the moment you press them, unlike the toggles above.");

            Widgets.Section("All of the above");
            GUILayout.BeginHorizontal();
            Widgets.Button("Turn everything ON", () => SetAll(true), !AllOn);
            Widgets.Button("Turn everything OFF", () => SetAll(false), AnyOn);
            GUILayout.EndHorizontal();
            GUILayout.Label(Summary(), AnyOn ? Theme.Value : Theme.Hint);
        }

        private bool AllOn => BattleToggles.All(entry => Battle.Get(entry.Cheat))
                              && state.InstantBuild.Value && state.InstantResearch.Value;

        private bool AnyOn => BattleToggles.Any(entry => Battle.Get(entry.Cheat))
                              || state.InstantBuild.Value || state.InstantResearch.Value;

        private void SetAll(bool on)
        {
            foreach (var (cheat, _) in BattleToggles)
            {
                Battle.Set(cheat, on);
            }
            state.InstantBuild.Value = on;
            state.InstantResearch.Value = on;
        }

        private string Summary()
        {
            var count = BattleToggles.Count(entry => Battle.Get(entry.Cheat))
                        + (state.InstantBuild.Value ? 1 : 0) + (state.InstantResearch.Value ? 1 : 0);
            return count == 0 ? "Nothing enabled." : $"{count} of 6 enabled — battle cheats clear when you restart the game.";
        }
    }
}
