using System;
using System.Collections.Generic;
using UnityEngine;

namespace DevConsole.Ui
{
    /// <summary>
    /// Ctrl+F search over every console button. Enter runs a safe command or arms a tile tool; Tab, or Enter on
    /// a destructive or disabled command, jumps to it instead, so nothing irreversible fires from a keystroke.
    /// </summary>
    internal sealed class SearchPalette
    {
        private const string FieldName = "forge-search";
        private const int MaxResults = 10;

        private string query = string.Empty;
        private int selected;
        private bool focusPending;
        private IReadOnlyList<Command> results = new Command[0];
        private string searchedQuery;
        private int searchedVersion = -1;

        public bool Open { get; private set; }

        /// <summary>Set when the user picks "go to"; the window switches tab and scrolls to the section.</summary>
        public Action<Command> JumpTo;

        public void Show()
        {
            Open = true;
            focusPending = true;
            selected = 0;
        }

        public void Hide()
        {
            Open = false;
            GUIUtility.keyboardControl = 0;
        }

        public void Draw()
        {
            HandleKeys();
            GUILayout.BeginHorizontal();
            GUI.SetNextControlName(FieldName);
            query = GUILayout.TextField(query, Theme.Field, GUILayout.Height(28));
            if (GUILayout.Button("Close", Theme.Button, GUILayout.Width(70), GUILayout.Height(28)))
            {
                Hide();
            }
            GUILayout.EndHorizontal();
            if (focusPending && Event.current.type == EventType.Repaint)
            {
                GUI.FocusControl(FieldName);
                focusPending = false;
            }

            if (query != searchedQuery || Commands.Version != searchedVersion)
            {
                results = Commands.Index.Search(query, MaxResults);
                searchedQuery = query;
                searchedVersion = Commands.Version;
            }
            selected = results.Count == 0 ? 0 : Mathf.Clamp(selected, 0, results.Count - 1);
            if (query.Trim().Length == 0)
            {
                Widgets.Hint($"Type to search {Commands.Index.Count} commands. Enter runs it (tile tools arm), Tab jumps to it, Esc closes. "
                             + "Destroy-style commands only jump, so they still need their confirm click.");
                return;
            }
            if (results.Count == 0)
            {
                Widgets.Hint("No command matches.");
                return;
            }
            for (var i = 0; i < results.Count; i++)
            {
                Row(results[i], i == selected);
            }
        }

        private void Row(Command command, bool isSelected)
        {
            GUILayout.BeginHorizontal();
            var marker = command.RunAtTile != null ? "⌖ " : command.Destructive ? "⚠ " : string.Empty;
            var previous = GUI.enabled;
            GUI.enabled = previous && command.Enabled;
            if (GUILayout.Button(marker + command.Label, isSelected ? Theme.TabActive : Theme.Button, GUILayout.Height(26)))
            {
                Activate(command);
            }
            GUI.enabled = previous;
            if (GUILayout.Button(command.Location, Theme.Hint, GUILayout.Width(220), GUILayout.Height(26)))
            {
                Jump(command);
            }
            GUILayout.EndHorizontal();
        }

        /// <summary>Keys are read before the text field draws, or the field would swallow Return and Tab.</summary>
        private void HandleKeys()
        {
            var current = Event.current;
            if (current.type != EventType.KeyDown)
            {
                return;
            }
            switch (current.keyCode)
            {
                case KeyCode.Escape:
                    Hide();
                    break;
                case KeyCode.DownArrow:
                    selected++;
                    break;
                case KeyCode.UpArrow:
                    selected = Math.Max(0, selected - 1);
                    break;
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    if (results.Count > 0)
                    {
                        Activate(results[selected]);
                    }
                    break;
                case KeyCode.Tab:
                    if (results.Count > 0)
                    {
                        Jump(results[selected]);
                    }
                    break;
                default:
                    return;
            }
            current.Use();
        }

        private void Activate(Command command)
        {
            if (command.Destructive || !command.Enabled)
            {
                Jump(command);
                return;
            }
            Hide();
            if (command.RunAtTile != null)
            {
                Cheats.World.Arm(command.Label, command.RunAtTile, command.Pick);
            }
            else
            {
                command.Run?.Invoke();
            }
        }

        private void Jump(Command command)
        {
            Hide();
            JumpTo?.Invoke(command);
        }
    }
}
