using BepInEx;
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
        public const string Version = "1.0.1";

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
                new TabHeroes(),
                new TabEquipment(),
                new TabWorld(),
                new TabDiplomacy(),
                new TabBattle(),
                new TabYields(state),
            });
            var harmony = new Harmony(Guid);
            Patches.Apply(harmony, state, Logger);
            DebugOverlay.Enable(harmony, Logger, state.NativeOverlay.Value);
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
