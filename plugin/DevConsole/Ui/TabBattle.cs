using DevConsole.Cheats;
using UnityEngine;

namespace DevConsole.Ui
{
    /// <summary>The game's own battle cheats — no patching involved.</summary>
    internal sealed class TabBattle : ITab
    {
        public string Title => "Battle";

        public void Draw()
        {
            Widgets.Section("Native battle cheats  (Amplitude's own, not written to the registry)");
            foreach (var (cheat, label) in Battle.All)
            {
                var before = Battle.Get(cheat);
                var after = GUILayout.Toggle(before, " " + label);
                if (after != before)
                {
                    Battle.Set(cheat, after);
                }
            }
            GUILayout.Space(6);
            Widgets.Button("Clear all battle cheats", Battle.ClearAll, Battle.AnyEnabled);
            GUILayout.Label("These live only for this session — restarting the game clears them.", GUI.skin.box);
        }
    }
}
