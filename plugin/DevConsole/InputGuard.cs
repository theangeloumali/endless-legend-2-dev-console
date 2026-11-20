using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DevConsole
{
    /// <summary>
    /// Typing "wasd" into a console field must not pan the camera. IMGUI reads native key events, while the game
    /// reads the Input System keyboard device, so disabling that device while a field has focus silences the
    /// game's shortcuts and leaves typing intact.
    /// </summary>
    internal static class InputGuard
    {
        private static Keyboard blocked;

        /// <summary>Called from OnGUI with whether one of the console's text fields owns the keyboard.</summary>
        public static void Update(bool textFocused)
        {
            if (textFocused && blocked == null)
            {
                var keyboard = Keyboard.current;
                if (keyboard != null && keyboard.enabled)
                {
                    Toggle(keyboard, enable: false);
                    blocked = keyboard;
                }
            }
            else if (!textFocused && blocked != null)
            {
                Release();
            }
        }

        public static void Release()
        {
            if (blocked == null)
            {
                return;
            }
            Toggle(blocked, enable: true);
            blocked = null;
        }

        private static void Toggle(Keyboard keyboard, bool enable)
        {
            try
            {
                if (enable)
                {
                    InputSystem.EnableDevice(keyboard);
                }
                else
                {
                    InputSystem.DisableDevice(keyboard);
                }
            }
            catch (Exception exception)
            {
                Orders.Log.LogWarning($"input guard could not {(enable ? "re-enable" : "pause")} the keyboard: {exception.Message}");
            }
        }

        /// <summary>IMGUI gives keyboard control to text fields only, so any owner means a field is being typed in.</summary>
        public static bool ConsoleTextFocused => GUIUtility.keyboardControl != 0;
    }
}
