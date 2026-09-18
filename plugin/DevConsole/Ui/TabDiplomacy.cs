using System;
using Amplitude.Mercury.Data.Simulation;
using DevConsole.Cheats;
using UnityEngine;

namespace DevConsole.Ui
{
    /// <summary>Relations with a chosen empire, plus switching which empire you play.</summary>
    internal sealed class TabDiplomacy : ITab
    {
        private static readonly EndGameVictoryPath[] Paths =
            { EndGameVictoryPath.Glorify, EndGameVictoryPath.Enrich, EndGameVictoryPath.Master };

        private string target = "1";
        private string warScore = "100";
        private string playAs = "0";
        private int path;

        public string Title => "Diplomacy";

        public void Draw()
        {
            Widgets.Section("Target empire");
            GUILayout.BeginHorizontal();
            var hasTarget = Widgets.IntField("Empire index", ref target, out var other);
            GUILayout.EndHorizontal();
            GUI.enabled = hasTarget;
            Widgets.Row(("Declare war", () => Diplomacy.DeclareWar(other)),
                        ("Force peace", () => Diplomacy.ForcePeace(other)),
                        ("All treaties", () => Diplomacy.AllTreaties(other)));
            Widgets.Button("Make them offer surrender", () => Diplomacy.ForceSurrender(other), hasTarget);
            Widgets.ValueButton("War score", ref warScore, "Apply", score => Diplomacy.ChangeWarScore(other, score), hasTarget);
            GUI.enabled = true;

            Widgets.Section("Empire");
            GUILayout.BeginHorizontal();
            Widgets.Button("Meet everybody", Diplomacy.MeetEverybody);
            Widgets.Button("Pacify all minor empires", Diplomacy.PacifyMinorEmpires);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Victory path", GUILayout.Width(110));
            path = GUILayout.SelectionGrid(path, Array.ConvertAll(Paths, p => p.ToString()), Paths.Length);
            GUILayout.EndHorizontal();
            Widgets.Button("Select victory path", () => Diplomacy.SelectVictoryPath(Paths[path]));

            Widgets.Section("Play as another empire");
            Widgets.ValueButton("Empire index", ref playAs, "Switch local empire", Diplomacy.PlayAs);
            GUILayout.Label("Switching hands you that empire; the console retargets automatically.", Theme.Hint);
        }
    }
}
