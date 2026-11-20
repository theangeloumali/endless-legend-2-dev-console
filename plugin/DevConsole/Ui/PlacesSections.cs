using Amplitude.Mercury.Simulation;
using DevConsole.Cheats;
using UnityEngine;

namespace DevConsole.Ui
{
    /// <summary>The World tab's Forge sections: world content, terrain extras and curiosity paths.</summary>
    internal sealed class PlacesSections
    {
        private readonly Widgets.Dropdown curiosity = new Widgets.Dropdown("Curiosity");
        private readonly Widgets.Dropdown tormented = new Widgets.Dropdown("Tormented army");
        private readonly Widgets.Dropdown minorFaction = new Widgets.Dropdown("Minor faction");
        private readonly Widgets.EnumPicker<ObservatoryType> observatory = new Widgets.EnumPicker<ObservatoryType>("Evolve to");
        private readonly Widgets.EnumPicker<Amplitude.Mercury.Terrain.RoadData.RoadQuality> road = new Widgets.EnumPicker<Amplitude.Mercury.Terrain.RoadData.RoadQuality>("Road quality");
        private string danger = "3";
        private string pathIndex = "0";
        private bool isTormented = true;
        private bool onlyTormented;
        private bool showPaths;

        public void Draw()
        {
            Widgets.Section("Places: world content at the hovered tile");
            curiosity.Draw(Catalog.Collectibles, 150f);
            var chosenCuriosity = curiosity.Selected(Catalog.Collectibles);
            GUILayout.BeginHorizontal();
            Widgets.Plot(chosenCuriosity == null ? "Pick a curiosity" : "Place " + chosenCuriosity.Display,
                         t => Places.CreateCuriosity(chosenCuriosity.Name, t), chosenCuriosity != null);
            Widgets.Plot("Remove collectible", Places.RemoveCollectible);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            var dangerOk = Widgets.IntField("Dungeon danger", ref danger, out var dangerLevel, fieldWidth: 60);
            Widgets.Plot("Spawn dungeon / spawn point", t => Places.SpawnDungeon(t, dangerLevel), dangerOk);
            Widgets.Plot("Remove dungeon", Places.RemoveDungeon);
            GUILayout.EndHorizontal();

            Widgets.PlotRow(("Spawn observatory", Places.SpawnObservatory), ("Remove observatory", Places.RemoveObservatory));
            observatory.Draw();
            Widgets.Plot("Evolve observatory to " + observatory.Value, t => Places.EvolveObservatory(t, observatory.Value));
            Widgets.PlotRow(("Spawn burrow", Places.SpawnBurrow), ("Remove burrow", Places.RemoveBurrow),
                            ("Coral spore", Places.CreateCoralSpore));

            tormented.Draw(Catalog.TormentedArmies, 150f);
            var chosenTormented = tormented.Selected(Catalog.TormentedArmies);
            GUILayout.BeginHorizontal();
            isTormented = GUILayout.Toggle(isTormented, " is Tormented", Theme.Toggle);
            onlyTormented = GUILayout.Toggle(onlyTormented, " only Tormented", Theme.Toggle);
            GUILayout.EndHorizontal();
            Widgets.Plot(chosenTormented == null ? "Pick a Tormented army" : "Create " + chosenTormented.Display,
                         t => Places.CreateTormented(t, chosenTormented.Name, isTormented, onlyTormented), chosenTormented != null);
            Widgets.PlotRow(("Fill Tormented gauge (settlement)", Places.FillTormentedGauge),
                            ("Settle Tormented", t => Places.ToggleTormentedSettle(t, true)),
                            ("Unsettle Tormented", t => Places.ToggleTormentedSettle(t, false)));

            minorFaction.Draw(Catalog.MinorFactions, 150f);
            var chosenFaction = minorFaction.Selected(Catalog.MinorFactions);
            GUILayout.BeginHorizontal();
            Widgets.Plot(chosenFaction == null ? "Pick a minor faction" : "Add " + chosenFaction.Display + " spawn point",
                         t => Places.AddVillageSpawnPoint(t, chosenFaction.Name), chosenFaction != null);
            Widgets.Plot("Remove village spawn point", Places.RemoveVillageSpawnPoint);
            GUILayout.EndHorizontal();

            Widgets.Section("Terrain extras");
            Widgets.PlotRow(("Refresh river", Places.RefreshRiver), ("Remove district", Places.RemoveDistrict));
            road.Draw();
            Widgets.Row(("Set road quality to " + road.Value, () => Places.SetRoadQuality(road.Value)),
                        ("Force road update", Places.ForceRoadUpdate));

            Widgets.Section("Curiosity paths (advanced)");
            if (!Widgets.Foldout("Curiosity path tools", ref showPaths))
            {
                return;
            }
            Widgets.Hint("Paths chain curiosities for the Path Curiosity victory condition. The path index counts from 0.");
            GUILayout.BeginHorizontal();
            var pathOk = Widgets.IntField("Path index", ref pathIndex, out var path, fieldWidth: 60);
            Widgets.Button("Add path", Places.AddPath);
            Widgets.Button("Remove path", () => Places.RemovePath(path), pathOk);
            GUILayout.EndHorizontal();
            Widgets.PlotRow(("Add selected curiosity to path", t => { if (chosenCuriosity != null && pathOk) Places.AddPathCuriosity(path, chosenCuriosity.Name, t); }),
                            ("Remove path curiosity", Places.RemovePathCuriosity));
        }
    }
}
