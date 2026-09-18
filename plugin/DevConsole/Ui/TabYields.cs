using System;
using BepInEx.Configuration;
using UnityEngine;

namespace DevConsole.Ui
{
    /// <summary>The one thing orders cannot do: scale per-turn income. These stay Harmony patches.</summary>
    internal sealed class TabYields : ITab
    {
        private static readonly string[] Labels = Array.ConvertAll(State.MultiplierSteps, m => m == 1 ? "off" : "x" + m);

        private readonly State state;

        public TabYields(State state) => this.state = state;

        public string Title => "Yields";

        public void Draw()
        {
            Widgets.Section("Yield multipliers  (applied to income each turn)");
            Multiplier("Dust", state.DustMultiplier);
            Multiplier("Industry", state.IndustryMultiplier);
            Multiplier("Science", state.ScienceMultiplier);
            Multiplier("Influence", state.InfluenceMultiplier);

            Widgets.Hint(Patches.Applied
                ? "Income is scaled as it arrives, so a change shows on your NEXT turn."
                : "These patches did not load on this build, so the multipliers do nothing.");

            Widgets.Section("Instant");
            Widgets.Toggle(state.InstantBuild);
            Widgets.Toggle(state.InstantResearch);
            GUILayout.Label("Multipliers apply at the next income tick. For a one-off jump use the Economy tab.", Theme.Hint);
        }

        /// <summary>Drawn as individual buttons rather than a SelectionGrid: the grid marks the active cell with
        /// the button's onNormal state, which is indistinguishable from normal in this theme.</summary>
        private static void Multiplier(string label, ConfigEntry<int> entry)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, Theme.Label, GUILayout.Width(Widgets.LabelWidth));
            for (var i = 0; i < State.MultiplierSteps.Length; i++)
            {
                var active = entry.Value == State.MultiplierSteps[i];
                if (GUILayout.Button(Labels[i], active ? Theme.TabActive : Theme.Button, GUILayout.Height(26)))
                {
                    entry.Value = State.MultiplierSteps[i];
                }
            }
            GUILayout.EndHorizontal();
        }
    }
}
