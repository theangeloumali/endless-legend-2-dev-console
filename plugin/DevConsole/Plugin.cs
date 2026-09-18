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
        public const string Version = "1.4.0";

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
                new TabEmpire(),
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
            Cheats.World.Track();
            Place();
        }

        /// <summary>An armed tool places on a left click that lands on the map rather than on the console.</summary>
        private void Place()
        {
            if (!Cheats.World.IsArmed)
            {
                return;
            }
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                Cheats.World.Disarm();
                return;
            }
            if (Input.GetMouseButtonDown(0) && !(visible && window.ContainsMouse()))
            {
                Cheats.World.PlaceAt(Cheats.World.HoveredTile);
            }
        }

        private void OnGUI()
        {
            if (visible)
            {
                visible = window.Draw();
            }
        }
    }
}
