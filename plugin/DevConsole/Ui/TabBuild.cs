using System.Collections.Generic;
using DevConsole.Cheats;
using UnityEngine;

namespace DevConsole.Ui
{
    /// <summary>Instant build and instant recruit: completing a queue entry is free and immediate.</summary>
    internal sealed class TabBuild : ITab
    {
        private List<Build.Site> sites = new List<Build.Site>();
        private Vector2 scroll;
        private int selected;

        public string Title => "Build";

        public void Draw()
        {
            Widgets.Section("Instant build / recruit  (free, completes immediately)");
            GUILayout.BeginHorizontal();
            Widgets.Button("Refresh settlements", () => { sites = Build.Sites(); selected = 0; });
            Widgets.Button("Complete EVERY settlement", Build.CompleteEverything, sites.Count > 0);
            GUILayout.EndHorizontal();

            if (sites.Count == 0)
            {
                GUILayout.Label("No settlements loaded — press Refresh while a game is running.", GUI.skin.box);
                return;
            }

            GUILayout.BeginHorizontal();
            for (var i = 0; i < sites.Count; i++)
            {
                var index = i;
                GUI.enabled = selected != i;
                if (GUILayout.Button($"{sites[i].Name} ({sites[i].Queue.Count})"))
                {
                    selected = index;
                }
                GUI.enabled = true;
            }
            GUILayout.EndHorizontal();

            var site = sites[Mathf.Clamp(selected, 0, sites.Count - 1)];
            if (site.Queue.Count == 0)
            {
                GUILayout.Label("Queue is empty.", GUI.skin.box);
                return;
            }
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
    }
}
