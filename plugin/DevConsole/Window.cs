using System;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;

namespace DevConsole
{
    /// <summary>One IMGUI window with every control; each control applies on press. Rows are built once because
    /// IMGUI redraws several times per frame.</summary>
    internal sealed class Window
    {
        private const int Id = 0x0DEC0DE;
        private static readonly string[] MultiplierLabels = State.MultiplierSteps.Select(m => m == 1 ? "off" : $"x{m}").ToArray();
        private static readonly string[] Roman = { "I", "II", "III", "IV", "V", "VI", "VII" };

        private readonly State state;
        private readonly string title;
        private readonly Control[] economy, research, resources, technologies, combat;
        private Rect rect = new Rect(40, 80, 560, 720);
        private string amountText;

        public Window(State state, Actions actions)
        {
            this.state = state;
            title = "Dev Console  -  " + state.Hotkey.Value.MainKey + " to hide";
            amountText = state.Amount.Value.ToString();
            economy = new[]
            {
                new Control("+10,000 Dust", () => actions.AddDust(10_000), actions.CanGainMoney),
                new Control("+100,000 Dust", () => actions.AddDust(100_000), actions.CanGainMoney),
                new Control("+10,000 Influence", () => actions.AddInfluence(10_000), actions.CanGainInfluence),
                new Control("+100,000 Influence", () => actions.AddInfluence(100_000), actions.CanGainInfluence),
            };
            research = new[]
            {
                new Control("+10,000 Research", () => actions.AddResearch(10_000), actions.CanGainResearch),
                new Control("+100,000 Research", () => actions.AddResearch(100_000), actions.CanGainResearch),
            };
            resources = new[]  // the amount is read at click time so the field above the row always wins
            {
                new Control("All strategic", () => actions.AddResources(Actions.Strategic, state.Amount.Value), actions.CanGiveResources),
                new Control("All luxury", () => actions.AddResources(Actions.Luxury, state.Amount.Value), actions.CanGiveResources),
                new Control("Cadavers & Spirits", () => actions.AddResources(Actions.Specials, state.Amount.Value), actions.CanGiveResources),
                new Control("Everything", () => actions.AddResources(Actions.All, state.Amount.Value), actions.CanGiveResources),
            };
            technologies = Enumerable.Range(0, Actions.EraCount)
                .Select(era => new Control("Era " + Roman[era], () => actions.UnlockEra(era), actions.CanUnlockEras))
                .Append(new Control("All", actions.UnlockAllEras, actions.CanUnlockEras))
                .ToArray();
            combat = new[] { new Control("Heal all armies", actions.HealArmies, actions.CanHeal) };
        }

        /// <summary>IMGUI draws in raw pixels, so the window is scaled up on high-resolution screens. GUI.matrix
        /// scales the mouse position too, so the rect stays in unscaled coordinates.</summary>
        public void Draw()
        {
            var previous = GUI.matrix;
            var scale = state.EffectiveUiScale;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            rect = GUILayout.Window(Id, rect, Contents, title, GUILayout.MinWidth(560));
            rect.x = Mathf.Clamp(rect.x, 0f, Screen.width / scale - 80f);
            rect.y = Mathf.Clamp(rect.y, 0f, Screen.height / scale - 40f);
            GUI.matrix = previous;
        }

        private void Contents(int id)
        {
            Section("Economy");
            Row(economy);
            Row(research);

            Section("Resources");
            GUILayout.BeginHorizontal();
            GUILayout.Label("Amount per resource", GUILayout.Width(150));
            amountText = GUILayout.TextField(amountText, GUILayout.Width(100));
            if (int.TryParse(amountText, out var amount) && amount > 0 && amount != state.Amount.Value)
            {
                state.Amount.Value = amount;
            }
            GUILayout.EndHorizontal();
            Row(resources);

            Section("Technologies");
            Row(technologies);

            Section("Yield multipliers (applied to income each turn)");
            Multiplier("Dust", state.DustMultiplier);
            Multiplier("Industry", state.IndustryMultiplier);
            Multiplier("Science", state.ScienceMultiplier);
            Multiplier("Influence", state.InfluenceMultiplier);

            Section("Instant");
            Toggle(state.InstantBuild);
            Toggle(state.InstantResearch);

            Section("Combat");
            Toggle(state.Invulnerable);
            Toggle(state.OneHitKills);
            Row(combat);

            GUILayout.Space(8);
            GUILayout.Label("Your empire only. Multipliers apply at the next income tick; buttons apply now.", GUI.skin.box);
            GUI.DragWindow();
        }

        private static void Multiplier(string label, ConfigEntry<int> entry)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(80));
            var current = Math.Max(0, Array.IndexOf(State.MultiplierSteps, entry.Value));
            var chosen = GUILayout.SelectionGrid(current, MultiplierLabels, MultiplierLabels.Length);
            if (chosen != current)
            {
                entry.Value = State.MultiplierSteps[chosen];
            }
            GUILayout.EndHorizontal();
        }

        private static void Toggle(ConfigEntry<bool> entry) =>
            entry.Value = GUILayout.Toggle(entry.Value, entry.Description.Description);

        private static void Section(string title)
        {
            GUILayout.Space(6);
            GUILayout.Label(title, GUI.skin.box);
        }

        private static void Row(Control[] controls)
        {
            GUILayout.BeginHorizontal();
            foreach (var control in controls)
            {
                GUI.enabled = control.Enabled;
                if (GUILayout.Button(control.Label, GUILayout.Height(28)))
                {
                    control.Action();
                }
                GUI.enabled = true;
            }
            GUILayout.EndHorizontal();
        }

        private sealed class Control
        {
            public readonly string Label;
            public readonly Action Action;
            public readonly bool Enabled;  // false when the game member behind the button is missing

            public Control(string label, Action action, bool enabled)
            {
                Label = label;
                Action = action;
                Enabled = enabled;
            }
        }
    }
}
