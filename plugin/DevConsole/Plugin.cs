using BepInEx;
using DevConsole.Cheats;
using DevConsole.Ui;
using HarmonyLib;
using UnityEngine;

namespace DevConsole
{
    [BepInPlugin(Guid, "Dev Console", Version)]
    [BepInIncompatibility("com.yourname.el2resourcemanager")]  // Nexus "EL2 Resource Manager" patches the same Gain* methods
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Guid = "angelo.el2.devconsole";
        public const string Version = "0.2.0";

        private State state;
        private Window window;
        private bool visible;

        private void Awake()
        {
            state = new State(Config);
            Orders.Initialize(Logger);
            window = new Window(state, new ITab[]
            {
                new TabEconomy(),
                new TabBuild(),
                new TabBattle(),
                new TabYields(state),
            });
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
