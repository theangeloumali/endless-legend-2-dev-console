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
        private bool closeRequested;

        public Window(State state, ITab[] tabs)
        {
            this.state = state;
            this.tabs = tabs;
            titles = tabs.Select(tab => tab.Title).ToArray();
        }

        /// <summary>IMGUI draws in raw pixels, so the window is scaled up on high-resolution screens. GUI.matrix
        /// scales the mouse position too, so the rect stays in unscaled coordinates.</summary>
        /// <summary>Returns false once the close button is used, so the caller can hide the window.</summary>
        public bool Draw()
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
            if (!closeRequested)
            {
                return true;
            }
            closeRequested = false;  // IMGUI runs several passes per frame; the request has to outlive them
            return false;
        }

        /// <summary>Screen-space hit test, used so an armed map click is not swallowed by the window. IMGUI
        /// coordinates put Y at the top and are pre-scale, so the mouse has to be converted both ways.</summary>
        public bool ContainsMouse()
        {
            var scale = state.EffectiveUiScale;
            var position = BepInEx.UnityInput.Current.mousePosition;
            var mouse = new Vector2(position.x / scale, (Screen.height - position.y) / scale);
            return rect.Contains(mouse);
        }

        private void Contents(int id)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("DEV CONSOLE", Theme.Header, GUILayout.ExpandWidth(true));
            GUILayout.Label(state.Hotkey.Value.MainKey + " hides", Theme.Hint, GUILayout.Width(80));
            if (GUILayout.Button("✕", Theme.Button, GUILayout.Width(34), GUILayout.Height(26)))
            {
                closeRequested = true;
            }
            GUILayout.EndHorizontal();

            Tabs();
            GUILayout.Label(Status(), Orders.Ready ? Theme.Value : Theme.Hint);

            if (Cheats.World.IsArmed)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"ARMED — {Cheats.World.ArmedLabel}: click the map to place", Theme.Section);
                if (GUILayout.Button("Cancel", Theme.Danger, GUILayout.Width(90), GUILayout.Height(26)))
                {
                    Cheats.World.Disarm();
                }
                GUILayout.EndHorizontal();
            }

            // a fixed viewport keeps the window from resizing every time a tab or a dropdown changes
            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(state.WindowHeight.Value));
            tabs[active].Draw();
            GUILayout.EndScrollView();

            // stops short of the close button: DragWindow swallows clicks inside its rect
            GUI.DragWindow(new Rect(0, 0, Width - 130f, 24f));
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
