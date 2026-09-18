using System.Collections.Generic;
using System.Linq;
using DevConsole.Cheats;
using UnityEngine;

namespace DevConsole.Ui
{
    /// <summary>Hero screen: pick one hero, or tick "apply to all" to hit the whole roster.</summary>
    internal sealed class TabHeroes : ITab
    {
        private readonly Widgets.Dropdown definition = new Widgets.Dropdown("Hero");
        private List<Heroes.Entry> roster = new List<Heroes.Entry>();
        private readonly string[] statBuffers = { "5", "5", "5", "5" };
        private string skillPoints = "10";
        private string experience = "10000";
        private string drawCount = "5";
        private string drawMin = "1";
        private string drawMax = "10";
        private bool applyToAll;
        private int selected;

        public string Title => "Heroes";

        public void Draw()
        {
            GUILayout.BeginHorizontal();
            Widgets.Button("Refresh roster", () => { roster = Heroes.Roster(); selected = 0; });
            applyToAll = GUILayout.Toggle(applyToAll, $" apply to all {roster.Count} heroes");
            GUILayout.EndHorizontal();

            if (roster.Count == 0)
            {
                GUILayout.Label("No heroes loaded - press Refresh while a game is running.", GUI.skin.box);
            }
            else
            {
                selected = Widgets.Picker("Hero", roster, selected, hero => hero.Label);
                GUILayout.Label(applyToAll ? "applying to every hero" : $"global HeroIndex #{roster[selected].Index}", GUI.skin.box);
            }

            Widgets.Section("Level & points");
            Widgets.ValueButton("Skill points", ref skillPoints, "Give",
                                points => ForEachTarget(hero => Heroes.GiveSkillPoints(hero, points)), roster.Count > 0);

            GUILayout.BeginHorizontal();
            var deltas = new uint[Heroes.StatisticCount];
            var statsValid = true;
            for (var i = 0; i < Heroes.StatisticCount; i++)
            {
                GUILayout.Label(Heroes.StatisticNames[i], GUILayout.Width(95));
                statBuffers[i] = GUILayout.TextField(statBuffers[i], GUILayout.Width(45));
                statsValid &= uint.TryParse(statBuffers[i], out deltas[i]);
            }
            GUILayout.EndHorizontal();
            Widgets.Button("Apply stats  (spends skill points)",
                           () => ForEachTarget(hero => Heroes.IncreaseStatistics(hero, deltas)),
                           statsValid && roster.Count > 0);

            Widgets.ValueButton("Experience", ref experience, "Give XP",
                                xp => ForEachTarget(hero => Heroes.GiveExperience(hero, xp)), roster.Count > 0);
            GUILayout.BeginHorizontal();
            Widgets.Button("Heal", () => ForEachTarget(Heroes.Heal), roster.Count > 0);
            Widgets.Button("Dismiss", () => ForEachTarget(Heroes.Dismiss), roster.Count > 0);
            GUILayout.EndHorizontal();

            Widgets.Section("Recruit");
            GUILayout.BeginHorizontal();
            GUILayout.Label("Draw", GUILayout.Width(40));
            drawCount = GUILayout.TextField(drawCount, GUILayout.Width(40));
            GUILayout.Label("heroes, level", GUILayout.Width(80));
            drawMin = GUILayout.TextField(drawMin, GUILayout.Width(35));
            GUILayout.Label("to", GUILayout.Width(20));
            drawMax = GUILayout.TextField(drawMax, GUILayout.Width(35));
            GUILayout.EndHorizontal();
            int count = 0, min = 0, max = 0;
            var drawValid = int.TryParse(drawCount, out count) & int.TryParse(drawMin, out min) & int.TryParse(drawMax, out max);
            GUILayout.BeginHorizontal();
            Widgets.Button("Create draw", () => Heroes.CreateDraw(count, min, max), drawValid);
            Widgets.Button("Recruit all in draw", () => Heroes.RecruitDraw(count), drawValid);
            Widgets.Button("Recruit all in market", () => Heroes.RecruitMarketplace(count), drawValid);
            GUILayout.EndHorizontal();

            Widgets.Section("Spawn a named hero at the hovered tile");
            definition.Draw(Catalog.Heroes);
            var chosen = definition.Selected(Catalog.Heroes);
            Widgets.Button(World.HasTile ? $"Spawn at tile #{World.HoveredTile}" : "Hover a map tile first",
                           () => Heroes.Spawn(chosen, World.HoveredTile), chosen != null && World.HasTile);
        }

        /// <summary>The whole point of the checkbox: one hero, or every hero, from the same controls.</summary>
        private void ForEachTarget(System.Action<Heroes.Entry> action)
        {
            foreach (var hero in applyToAll ? roster : roster.Skip(selected).Take(1))
            {
                action(hero);
            }
            roster = Heroes.Roster();
        }
    }
}
