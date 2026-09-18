using System.Collections.Generic;
using System.Linq;
using DevConsole.Cheats;
using UnityEngine;

namespace DevConsole.Ui
{
    /// <summary>Instant build and instant recruit: completing a queue entry is free and immediate.</summary>
    internal sealed class TabBuild : ITab
    {
        private List<Build.Site> sites = new List<Build.Site>();
        private Vector2 scroll;
        private readonly Widgets.Dropdown population2 = new Widgets.Dropdown("Kind");
        private readonly Widgets.Dropdown improvement = new Widgets.Dropdown("Improvement");
        private string approval = "100";
        private string population = "5";
        private int selected;

        public string Title => "Build";

        public void Draw()
        {
            Widgets.Section("Instant build / recruit  (free, completes immediately)");
            GUILayout.BeginHorizontal();
            Widgets.Button("Refresh settlements", () => { sites = Build.Sites(); selected = 0; });
            Widgets.Button("Complete EVERY settlement", Build.CompleteEverything, sites.Count > 0);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{sites.Count(s => s.Kind == "city")} cities · {sites.Count(s => s.Kind == "camp")} camps", Theme.Value);
            GUILayout.EndHorizontal();

            if (sites.Count == 0)
            {
                GUILayout.Label("No settlements loaded — press Refresh while a game is running.", Theme.Hint);
                return;
            }

            selected = Widgets.Picker("Settlement", sites, selected, site => site.Label);
            var site = sites[selected];

            if (site.Queue.Count == 0)
            {
                GUILayout.Label("Queue is empty.", Theme.Hint);
            }
            else
            {
                scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(180));
                for (var i = 0; i < site.Queue.Count; i++)
                {
                    var index = i;
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"{i + 1}.  {site.Queue[i]}");
                    Widgets.Button("Complete", () => { Build.Complete(site, index); sites = Build.Sites(); });
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndScrollView();
                Widgets.Button("Complete this whole queue", () => { Build.CompleteQueue(site); sites = Build.Sites(); });
            }

            Widgets.Section("Approval");
            Widgets.ValueButton("Approval", ref approval, "Add to this city", amount => Build.AddApproval(site, amount));
            GUILayout.BeginHorizontal();
            Widgets.Button("Add to every city", () => { int.TryParse(approval, out var amount); Build.AddApprovalEverywhere(amount); });
            Widgets.Button("Clear this city", () => Build.ClearApproval(site));
            Widgets.Button("Clear every city", Build.ClearApprovalEverywhere);
            GUILayout.EndHorizontal();
            Widgets.Hint("Adds to the city's approval each press; Clear resets the bonus back to zero.");

            Widgets.Section("Population");
            Widgets.ValueButton("Population", ref population, "Add to this city",
                                amount => EmpireCheats.AddPopulation(site, site.TileIndex, amount));
            population2.Draw(Catalog.Populations, 140f);
            var pop = population2.Selected(Catalog.Populations);
            GUILayout.BeginHorizontal();
            Widgets.Button(pop == null ? "Pick a population" : "Add " + pop.Display,
                           () => EmpireCheats.AddSpecificPopulation(site, pop.Name), pop != null);
            Widgets.Button("Buy with money", () => EmpireCheats.BuyPopulationWithMoney(site, pop.Name), pop != null);
            Widgets.Button("Buy with cadavers", () => EmpireCheats.BuyPopulationWithCadavers(site));
            GUILayout.EndHorizontal();

            Widgets.Section("District improvement");
            improvement.Draw(Catalog.Improvements, 140f);
            var imp = improvement.Selected(Catalog.Improvements);
            Widgets.Button(imp == null ? "Pick an improvement"
                                       : World.HasTile ? $"Place {imp.Display} at tile #{World.HoveredTile}" : "Hover a district tile",
                           () => EmpireCheats.SetImprovement(site, imp.Name, World.HoveredTile), imp != null && World.HasTile);

            Widgets.Section("Unit limits");
            GUILayout.BeginHorizontal();
            Widgets.Button("Lift unique-unit limits", Limits.Lift, !Limits.Lifted);
            Widgets.Button("Restore limits", Limits.Restore, Limits.Lifted);
            GUILayout.EndHorizontal();
            Widgets.Hint(Limits.Lifted
                ? $"Lifted on {Limits.Affected} unit definitions — units capped at 0/1 can be recruited repeatedly."
                : "Clears the one-per-empire cap on unique units, so the recruit button stops greying out. Lasts for this session.");
        }
    }
}
