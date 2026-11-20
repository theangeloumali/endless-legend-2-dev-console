using System.Linq;
using UnityEngine;

namespace DevConsole.Ui
{
    /// <summary>Window shell: header, search, tab bar, status line, tab content and the edit log.</summary>
    internal sealed class Window
    {
        private const int Id = 0x0DEC0DE;
        private const float Width = 780f;
        private const int TabsPerRow = 7;

        private readonly State state;
        private readonly ITab[] tabs;
        private readonly string[] titles;
        private readonly SearchPalette palette = new SearchPalette();
        private Rect rect = new Rect(40, 60, Width, 0f);  // height follows the content
        private Vector2 scroll;
        private int active;
        private bool closeRequested;
        private bool paletteRequested;
        private Command pendingJump;

        public Window(State state, ITab[] tabs)
        {
            this.state = state;
            this.tabs = tabs;
            titles = tabs.Select(tab => tab.Title).ToArray();
            palette.JumpTo = command => pendingJump = command;
        }

        /// <summary>Applied at the next Layout event: opening mid-event would change the layout between passes.</summary>
        public void RequestPalette() => paletteRequested = true;

        /// <summary>IMGUI draws in raw pixels, so the window is scaled up on high-resolution screens. GUI.matrix
        /// scales the mouse position too, so the rect stays in unscaled coordinates. Returns false once the close
        /// button is used, so the caller can hide the window.</summary>
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
            palette.Hide();
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
            ApplyRequests();
            var showPalette = palette.Open;  // one value per event: Esc can close it mid-event, the layout must not change
            Header();
            if (!showPalette && GUILayout.Button("Search every command…  (" + state.SearchHotkey.Value + ")", Theme.Field, GUILayout.Height(28)))
            {
                paletteRequested = true;
            }
            Tabs();
            StatusLine();
            ArmedLine();

            // a fixed viewport keeps the window from resizing every time a tab or a dropdown changes
            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(state.WindowHeight.Value));
            if (showPalette)
            {
                palette.Draw();
            }
            else
            {
                Commands.BeginTab(titles[active], collecting: false);
                tabs[active].Draw();
                Commands.EndTab();
            }
            GUILayout.EndScrollView();
            ScrollToPendingSection();

            EditLogStrip.Draw();
            if (showPalette)
            {
                CollectCommands();
            }

            // stops short of the close button: DragWindow swallows clicks inside its rect
            GUI.DragWindow(new Rect(0, 0, Width - 130f, 24f));
        }

        private void ApplyRequests()
        {
            if (Event.current.type != EventType.Layout)
            {
                return;
            }
            if (paletteRequested)
            {
                paletteRequested = false;
                palette.Show();
            }
            if (pendingJump != null)
            {
                var index = System.Array.IndexOf(titles, pendingJump.Tab);
                if (index >= 0)
                {
                    active = index;
                }
            }
        }

        /// <summary>The section's position is only known after the tab has drawn once, so the scroll lands a frame later.</summary>
        private void ScrollToPendingSection()
        {
            if (pendingJump == null || Event.current.type != EventType.Repaint || titles[active] != pendingJump.Tab)
            {
                return;
            }
            scroll.y = Commands.SectionTop(pendingJump.Tab, pendingJump.Section) ?? 0f;
            pendingJump = null;
        }

        /// <summary>Draws every tab off-screen and disabled, so each Widgets button records itself for the search
        /// with its current selections. Runs in every event while the palette is open, keeping layout passes equal.</summary>
        private void CollectCommands()
        {
            Commands.Index.Clear();
            var previous = GUI.enabled;
            GUI.enabled = false;
            GUILayout.BeginArea(new Rect(-40000f, -40000f, Width, 40000f));
            for (var i = 0; i < tabs.Length; i++)
            {
                Commands.BeginTab(titles[i], collecting: true);
                tabs[i].Draw();
                Commands.EndTab();
            }
            GUILayout.EndArea();
            GUI.enabled = previous;
        }

        private void Header()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("GELODGREAT DEV CONSOLE", Theme.Header, GUILayout.ExpandWidth(false));
            GUILayout.Label("2.0 · ENDLESS FORGE", Theme.Value, GUILayout.ExpandWidth(true));
            GUILayout.Label(state.Hotkey.Value.MainKey + " hides", Theme.Hint, GUILayout.Width(80));
            if (GUILayout.Button("✕", Theme.Button, GUILayout.Width(34), GUILayout.Height(26)))
            {
                closeRequested = true;
            }
            GUILayout.EndHorizontal();
        }

        private void Tabs()
        {
            for (var from = 0; from < titles.Length; from += TabsPerRow)
            {
                GUILayout.BeginHorizontal();
                for (var i = from; i < Mathf.Min(from + TabsPerRow, titles.Length); i++)
                {
                    if (GUILayout.Button(titles[i], i == active ? Theme.TabActive : Theme.Tab, GUILayout.Height(26)))
                    {
                        active = i;
                        palette.Hide();
                    }
                }
                GUILayout.EndHorizontal();
            }
        }

        private static void StatusLine()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(Orders.Ready ? $"Empire #{Sim.LocalEmpireIndex}   ·   orders ready" : "No game running — start or load a game",
                            Orders.Ready ? Theme.Value : Theme.Hint);
            GUILayout.Label(SelfCheck.Summary, SelfCheck.Done && SelfCheck.Missing.Count == 0 ? Theme.Value : Theme.Hint);
            GUILayout.EndHorizontal();
        }

        private static void ArmedLine()
        {
            if (!Cheats.World.IsArmed)
            {
                return;
            }
            GUILayout.BeginHorizontal();
            GUILayout.Label($"ARMED — {Cheats.World.ArmedLabel}: click the map to place", Theme.Section);
            if (GUILayout.Button("Cancel", Theme.Danger, GUILayout.Width(90), GUILayout.Height(26)))
            {
                Cheats.World.Disarm();
            }
            GUILayout.EndHorizontal();
        }
    }
}
