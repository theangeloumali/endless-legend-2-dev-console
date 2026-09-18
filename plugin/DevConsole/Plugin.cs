using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace DevConsole
{
    [BepInPlugin(Guid, "Dev Console", Version)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Guid = "angelo.el2.devconsole";
        public const string Version = "0.1.0";

        private State state;
        private Actions actions;
        private Window window;
        private bool visible;

        private void Awake()
        {
            state = new State(Config);
            actions = new Actions(Logger);
            window = new Window(state, actions);
            Patches.Apply(new Harmony(Guid), state, Logger);
            Logger.LogInfo($"Dev Console {Version} loaded - press {state.Hotkey.Value.MainKey} in game.");
        }

        private void Update()
        {
            if (state.Hotkey.Value.IsDown())
            {
                visible = !visible;
            }
        }

        private void OnGUI()
        {
            if (visible)
            {
                window.Draw();
            }
        }
    }
}
