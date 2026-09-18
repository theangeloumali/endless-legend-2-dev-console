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

            Widgets.Section("Instant");
            Widgets.Toggle(state.InstantBuild);
            Widgets.Toggle(state.InstantResearch);
            GUILayout.Label("Multipliers apply at the next income tick. For a one-off jump use the Economy tab.", GUI.skin.box);
        }

        private static void Multiplier(string label, ConfigEntry<int> entry)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(90));
            var current = Math.Max(0, Array.IndexOf(State.MultiplierSteps, entry.Value));
            var chosen = GUILayout.SelectionGrid(current, Labels, Labels.Length);
            if (chosen != current)
            {
                entry.Value = State.MultiplierSteps[chosen];
            }
            GUILayout.EndHorizontal();
        }
    }
}
