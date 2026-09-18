using System.Linq;
using UnityEngine;

namespace DevConsole.Ui
{
    /// <summary>Window shell: tab bar, status line, and the scaling that keeps it readable at 4K.</summary>
    internal sealed class Window
    {
        private const int Id = 0x0DEC0DE;
        private const float Width = 660f;

        private readonly State state;
        private readonly ITab[] tabs;
        private readonly string[] titles;
        private Rect rect = new Rect(40, 60, Width, 0f);  // height follows the content
        private Vector2 scroll;
        private int active;

        public Window(State state, ITab[] tabs)
        {
            this.state = state;
            this.tabs = tabs;
            titles = tabs.Select(tab => tab.Title).ToArray();
        }

        /// <summary>IMGUI draws in raw pixels, so the window is scaled up on high-resolution screens. GUI.matrix
        /// scales the mouse position too, so the rect stays in unscaled coordinates.</summary>
        public void Draw()
        {
            var previousMatrix = GUI.matrix;
            var previousSkin = Theme.Begin();
            var scale = state.EffectiveUiScale;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            rect = GUILayout.Window(Id, rect, Contents, string.Empty, Theme.Window, GUILayout.Width(Width));
            rect.x = Mathf.Clamp(rect.x, 0f, Screen.width / scale - 120f);
            rect.y = Mathf.Clamp(rect.y, 0f, Screen.height / scale - 40f);
            Theme.End(previousSkin);
            GUI.matrix = previousMatrix;
        }

        private void Contents(int id)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("DEV CONSOLE", Theme.Header, GUILayout.ExpandWidth(true));
            GUILayout.Label(state.Hotkey.Value.MainKey + " hides", Theme.Hint, GUILayout.Width(90));
            GUILayout.EndHorizontal();

            Tabs();
            GUILayout.Label(Status(), Orders.Ready ? Theme.Value : Theme.Hint);

            // a fixed viewport keeps the window from resizing every time a tab or a dropdown changes
            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(state.WindowHeight.Value));
            tabs[active].Draw();
            GUILayout.EndScrollView();

            GUI.DragWindow(new Rect(0, 0, Width, 24));
        }

        /// <summary>Two rows: eight tabs on one line are unreadably narrow at this width.</summary>
        private void Tabs()
        {
            var half = (titles.Length + 1) / 2;
            Row(0, half);
            Row(half, titles.Length);
        }

        private void Row(int from, int to)
        {
            GUILayout.BeginHorizontal();
            for (var i = from; i < to; i++)
            {
                if (GUILayout.Button(titles[i], i == active ? Theme.TabActive : Theme.Tab, GUILayout.Height(26)))
                {
                    active = i;
                }
            }
            GUILayout.EndHorizontal();
        }

        private static string Status() =>
            Orders.Ready ? $"Empire #{Sim.LocalEmpireIndex}   ·   orders ready" : "No game running — start or load a game";
    }
}
