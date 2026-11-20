using Forge.Core;
using UnityEngine;

namespace DevConsole.Ui
{
    /// <summary>The last few edits under the tab content: ✓ posted, ✗ rejected or failed, with Undo where one exists.</summary>
    internal static class EditLogStrip
    {
        private const int Rows = 5;
        private static bool collapsed;

        public static void Draw()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button((collapsed ? "▸ " : "▾ ") + "EDIT LOG", Theme.Section, GUILayout.Width(130)))
            {
                collapsed = !collapsed;
            }
            GUILayout.Label(Edits.Log.TotalAdded == 0
                                ? "nothing yet"
                                : $"last {Mathf.Min(Rows, Edits.Log.Count)} of {Edits.Log.TotalAdded} · Undo only where the game has an opposite command",
                            Theme.Hint);
            GUILayout.EndHorizontal();
            if (collapsed)
            {
                return;
            }
            foreach (var entry in Edits.Log.Latest(Rows))
            {
                Row(entry);
            }
        }

        private static void Row(EditEntry entry)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(entry.Turn >= 0 ? "turn " + entry.Turn : string.Empty, Theme.Hint, GUILayout.Width(62));
            var ok = entry.Outcome == EditOutcome.Posted;
            GUILayout.Label(ok ? "✓" : "✗", ok ? Theme.Value : Theme.Danger, GUILayout.Width(18));
            var text = entry.Reason == null ? entry.Label : $"{entry.Label}: {entry.Outcome.ToString().ToLowerInvariant()} ({entry.Reason})";
            GUILayout.Label(text, Theme.Label);
            var previous = GUI.enabled;
            GUI.enabled = previous && entry.Undo != null && !entry.Undone;
            if (GUILayout.Button(entry.Undone ? "undone" : entry.Undo == null ? "—" : "Undo", Theme.Button, GUILayout.Width(70), GUILayout.Height(22)))
            {
                Edits.Undo(entry);
            }
            GUI.enabled = previous;
            GUILayout.EndHorizontal();
        }
    }
}
