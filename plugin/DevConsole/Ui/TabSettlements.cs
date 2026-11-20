using System.Collections.Generic;
using Amplitude.Mercury.Simulation;
using DevConsole.Cheats;
using UnityEngine;

namespace DevConsole.Ui
{
    /// <summary>Pick a settlement (yours from the list, anyone's on the map), then rename, move the capital,
    /// reshape territory, edit its queue and population, or destroy it.</summary>
    internal sealed class TabSettlements : ITab
    {
        private readonly Widgets.Dropdown picker = new Widgets.Dropdown("Settlement");
        private readonly Widgets.Dropdown constructible = new Widgets.Dropdown("Constructible");
        private readonly Widgets.Dropdown population = new Widgets.Dropdown("Population");
        private readonly Widgets.EnumPicker<EnqueuePosition> position = new Widgets.EnumPicker<EnqueuePosition>("Queue position");
        private List<Build.Site> own = new List<Build.Site>();
        private int ownIndex;
        private Build.Site site;
        private string newName = string.Empty;

        public string Title => "Settlements";

        public void Draw()
        {
            Widgets.Section("Pick a settlement");
            Widgets.Pick("Pick settlement on the map", Pick);
            Widgets.Button("Refresh my settlements", () => { own = Build.Sites(); ownIndex = 0; });
            if (own.Count > 0)
            {
                var picked = picker.Draw(own, ownIndex, s => s.Label);
                if (picked != ownIndex || site == null)
                {
                    ownIndex = picked;
                    Select(own[ownIndex]);
                }
            }
            Widgets.ValueRow("Selected", site == null ? "none: pick one on the map or from the list" : $"{site.Name}  ({site.Kind}, tile #{site.TileIndex})",
                             site != null);
            var has = site != null;

            Widgets.Section("Identity");
            newName = Widgets.TextRow("New name", newName);
            Widgets.Button("Rename", () => Settlements.Rename(site, newName.Trim()), has && newName.Trim().Length > 0);
            Widgets.Button("Make capital", () => Settlements.SetCapital(site), has);
            Widgets.Button("Turn camp into a city", () => Settlements.EvolveCampToCity(site), has && site.Kind == "camp");

            Widgets.Section("Territory");
            Widgets.Plot("Merge the settlement I click into this city", t => Settlements.MergeInto(site, t), has);
            Widgets.Plot("Detach territory (click its administrative center)", t => Settlements.DetachTerritory(site, t), has);
            Widgets.Plot("Build foundation at the clicked tile", t => Settlements.BuildFoundation(site, t), has);

            Widgets.Section("Production queue");
            constructible.Draw(Catalog.Constructibles, 160f);
            var chosen = constructible.Selected(Catalog.Constructibles);
            position.Draw();
            Widgets.Plot(chosen == null ? "Pick a constructible" : $"Enqueue {chosen.Display} at the clicked tile",
                         t => Settlements.Enqueue(site, chosen.Name, t, position.Value), has && chosen != null);
            Widgets.Button(chosen == null ? "Pick a constructible" : $"Enqueue {chosen.Display} at the settlement",
                           () => Settlements.Enqueue(site, chosen.Name, site.TileIndex, position.Value), has && chosen != null);
            Widgets.Hint("Districts need the tile they are built on; units and improvements can use the settlement's own tile.");
            if (has)
            {
                for (var i = 0; i < site.Queue.Count; i++)
                {
                    var index = i;
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"{i + 1}.  {site.Queue[i]}", Theme.Label);
                    Widgets.Button("Remove", () => { Settlements.RemoveConstruction(site, index); Reload(); });
                    GUILayout.EndHorizontal();
                }
            }
            Widgets.Button("Reload queue", Reload, has);

            Widgets.Section("Population");
            population.Draw(Catalog.Populations, 150f);
            var chosenPopulation = population.Selected(Catalog.Populations);
            Widgets.Button(chosenPopulation == null ? "Pick a population" : "Change selected population to " + chosenPopulation.Display,
                           () => Settlements.ChangePopulation(site, chosenPopulation.Name), has && chosenPopulation != null);

            Widgets.Section("Danger zone");
            Widgets.DangerButton(has ? "Destroy " + site.Name : "Destroy settlement", () => { Settlements.Destroy(site); site = null; }, has);
            Widgets.Hint("Destroy asks for a second click. The game has no opposite command, so it cannot be undone.");
        }

        private void Pick(int tile)
        {
            var settlement = Sim.SettlementAt(tile);
            if (settlement == null)
            {
                Orders.Log.LogWarning($"no settlement on tile {tile}");
                return;
            }
            var status = HarmonyLib.Traverse.Create(settlement).Field("SettlementStatus").GetValue()?.ToString() ?? string.Empty;
            Select(Build.SiteOf(settlement, status.IndexOf("Camp", System.StringComparison.OrdinalIgnoreCase) >= 0 ? "camp" : "city"));
        }

        private void Select(Build.Site chosen)
        {
            site = chosen;
            newName = chosen.Name;
        }

        private void Reload()
        {
            var settlement = site == null ? null : Sim.SettlementAt(site.TileIndex);
            if (settlement != null)
            {
                site = Build.SiteOf(settlement, site.Kind);
            }
        }
    }
}
