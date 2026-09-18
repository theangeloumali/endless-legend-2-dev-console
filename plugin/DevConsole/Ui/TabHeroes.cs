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
                GUILayout.Label("No heroes loaded — press Refresh while a game is running.", GUI.skin.box);
            }
            else
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Hero", GUILayout.Width(110));
                selected = Mathf.Clamp(selected, 0, roster.Count - 1);
                if (GUILayout.Button(roster[selected].Label + "   ▼"))
                {
                    selected = (selected + 1) % roster.Count;  // small rosters cycle faster than a list expands
                }
                GUILayout.EndHorizontal();
                GUILayout.Label($"global HeroIndex #{roster[selected].Index}" + (applyToAll ? "  —  ignored while 'apply to all' is on" : ""),
                                GUI.skin.box);
            }

            Widgets.Section("Level & points");
            GUILayout.BeginHorizontal();
            var hasPoints = Widgets.IntField("Skill points", ref skillPoints, out var points);
            Widgets.Button("Give", () => ForEachTarget(hero => Heroes.GiveSkillPoints(hero, points)), hasPoints && roster.Count > 0);
            GUILayout.EndHorizontal();

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

            GUILayout.BeginHorizontal();
            var hasXp = Widgets.IntField("Experience", ref experience, out var xp);
            Widgets.Button("Give XP", () => ForEachTarget(hero => Heroes.GiveExperience(hero, xp)), hasXp && roster.Count > 0);
            Widgets.Button("Heal", () => ForEachTarget(Heroes.Heal), roster.Count > 0);
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
