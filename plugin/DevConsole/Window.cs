using System;
using System.Linq;
using UnityEngine;

namespace DevConsole
{
    /// <summary>One IMGUI window with every control; each control applies on press.</summary>
    internal sealed class Window
    {
        private const int Id = 0x0DEC0DE;
        private static readonly string[] MultiplierLabels = State.MultiplierSteps.Select(m => m == 1 ? "off" : $"x{m}").ToArray();

        private readonly State state;
        private readonly Actions actions;
        private Rect rect = new Rect(40, 80, 560, 720);
        private string amountText;

        public Window(State state, Actions actions)
        {
            this.state = state;
            this.actions = actions;
            amountText = state.Amount.Value.ToString();
        }

        public void Draw()
        {
            rect = GUILayout.Window(Id, rect, Contents, "Dev Console  -  " + state.Hotkey.Value.MainKey + " to hide", GUILayout.MinWidth(560));
        }

        private void Contents(int id)
        {
            Section("Economy");
            Row(("+10,000 Dust", () => actions.AddDust(10_000), actions.CanGainMoney),
                ("+100,000 Dust", () => actions.AddDust(100_000), actions.CanGainMoney),
                ("+10,000 Influence", () => actions.AddInfluence(10_000), actions.CanGainInfluence),
                ("+100,000 Influence", () => actions.AddInfluence(100_000), actions.CanGainInfluence));
            Row(("+10,000 Research", () => actions.AddResearch(10_000), actions.CanGainResearch),
                ("+100,000 Research", () => actions.AddResearch(100_000), actions.CanGainResearch));

            Section("Resources");
            GUILayout.BeginHorizontal();
            GUILayout.Label("Amount per resource", GUILayout.Width(150));
            amountText = GUILayout.TextField(amountText, GUILayout.Width(100));
            if (int.TryParse(amountText, out var amount) && amount > 0 && amount != state.Amount.Value)
            {
                state.Amount.Value = amount;
            }
            GUILayout.EndHorizontal();
            var n = state.Amount.Value;
            Row(("All strategic", () => actions.AddResources(Actions.Strategic, n), actions.CanGiveResources),
                ("All luxury", () => actions.AddResources(Actions.Luxury, n), actions.CanGiveResources),
                ("Cadavers & Spirits", () => actions.AddResources(Actions.Specials, n), actions.CanGiveResources),
                ("Everything", () => actions.AddResources(Actions.Strategic.Concat(Actions.Luxury).Concat(Actions.Specials), n), actions.CanGiveResources));

            Section("Technologies");
            GUILayout.BeginHorizontal();
            for (var era = 0; era < Actions.EraCount; era++)
            {
                var index = era;
                Button($"Era {ToRoman(era + 1)}", () => actions.UnlockEra(index), actions.CanUnlockEras);
            }
            Button("All", () => { for (var era = 0; era < Actions.EraCount; era++) actions.UnlockEra(era); }, actions.CanUnlockEras);
            GUILayout.EndHorizontal();

            Section("Yield multipliers (applied to income each turn)");
            Multiplier("Dust", state.DustMultiplier);
            Multiplier("Industry", state.IndustryMultiplier);
            Multiplier("Science", state.ScienceMultiplier);
            Multiplier("Influence", state.InfluenceMultiplier);

            Section("Instant");
            state.InstantBuild.Value = GUILayout.Toggle(state.InstantBuild.Value, " Instant build  (production x1000: anything completes next turn)");
            state.InstantResearch.Value = GUILayout.Toggle(state.InstantResearch.Value, " Instant research  (science x1000: a technology per turn)");

            Section("Combat");
            state.Invulnerable.Value = GUILayout.Toggle(state.Invulnerable.Value, " Invulnerable  (your units take no damage)");
            state.OneHitKills.Value = GUILayout.Toggle(state.OneHitKills.Value, " One-hit kills  (your units deal 99,999 damage)");
            Row(("Heal all armies", actions.HealArmies, actions.CanHeal));

            GUILayout.Space(8);
            GUILayout.Label("Human empire only. Multipliers apply at the next income tick; buttons apply now.", GUI.skin.box);
            GUI.DragWindow();
        }

        private void Multiplier(string label, BepInEx.Configuration.ConfigEntry<int> entry)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(80));
            var current = Array.IndexOf(State.MultiplierSteps, entry.Value);
            var chosen = GUILayout.SelectionGrid(current < 0 ? 0 : current, MultiplierLabels, MultiplierLabels.Length);
            if (chosen != current)
            {
                entry.Value = State.MultiplierSteps[chosen];
            }
            GUILayout.EndHorizontal();
        }

        private static void Section(string title)
        {
            GUILayout.Space(6);
            GUILayout.Label(title, GUI.skin.box);
        }

        private static void Row(params (string label, Action action, bool enabled)[] buttons)
        {
            GUILayout.BeginHorizontal();
            foreach (var (label, action, enabled) in buttons)
            {
                Button(label, action, enabled);
            }
            GUILayout.EndHorizontal();
        }

        private static void Button(string label, Action action, bool enabled)
        {
            GUI.enabled = enabled;
            if (GUILayout.Button(label, GUILayout.Height(28)))
            {
                action();
            }
            GUI.enabled = true;
        }

        private static string ToRoman(int value) => new[] { "I", "II", "III", "IV", "V", "VI", "VII" }[value - 1];
    }
}
