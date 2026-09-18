using System.Linq;
using UnityEngine;

namespace DevConsole.Ui
{
    /// <summary>Window shell: tab bar, status line, and the scaling that keeps it readable at 4K.</summary>
    internal sealed class Window
    {
        private const int Id = 0x0DEC0DE;

        private readonly State state;
        private readonly ITab[] tabs;
        private readonly string[] titles;
        private Rect rect = new Rect(40, 80, 620, 700);
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
            var previous = GUI.matrix;
            var scale = state.EffectiveUiScale;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            rect = GUILayout.Window(Id, rect, Contents, "Dev Console  -  " + state.Hotkey.Value.MainKey + " to hide",
                                    GUILayout.MinWidth(620));
            rect.x = Mathf.Clamp(rect.x, 0f, Screen.width / scale - 80f);
            rect.y = Mathf.Clamp(rect.y, 0f, Screen.height / scale - 40f);
            GUI.matrix = previous;
        }

        private void Contents(int id)
        {
            active = GUILayout.Toolbar(active, titles);
            GUILayout.Label(Status(), GUI.skin.box);
            scroll = GUILayout.BeginScrollView(scroll);
            tabs[active].Draw();
            GUILayout.EndScrollView();
            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }

        private static string Status() =>
            Orders.Ready ? $"empire #{Sim.LocalEmpireIndex} — orders ready" : "no game running — start or load a game";
    }
}
