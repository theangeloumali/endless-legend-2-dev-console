using BepInEx;
using DevConsole.Ui;
using HarmonyLib;
using UnityEngine;

namespace DevConsole
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInIncompatibility("com.yourname.el2resourcemanager")]  // Nexus "EL2 Resource Manager" patches the same Gain* methods
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Guid = "angelo.el2.devconsole";  // unchanged: it names the config file
        public const string Name = "GeloDGreat Dev Console";
        public const string Version = "1.7.0";

        private State state;
        private Window window;
        private bool visible;
        private int lastPlacedFrame = -1;

        private void Awake()
        {
            state = new State(Config);
            Orders.Initialize(Logger);
            window = new Window(state, new ITab[]
            {
                new TabQuick(state),
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
            Logger.LogInfo($"{Name} {Version} loaded - press {state.Hotkey.Value.MainKey} in game.");
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

        /// <summary>The game ships Unity's new Input System, so UnityEngine.Input and IMGUI mouse events never
        /// arrive here. Everything goes through BepInEx's abstraction — the same path the hotkey already uses.</summary>
        private void Place()
        {
            if (!Cheats.World.IsArmed)
            {
                return;
            }
            var input = UnityInput.Current;
            if (input.GetMouseButtonDown(1) || input.GetKey(KeyCode.Escape))
            {
                Cheats.World.Disarm();
                return;
            }
            if (!input.GetMouseButtonDown(0) || Time.frameCount == lastPlacedFrame)
            {
                return;
            }
            if (visible && window.ContainsMouse())
            {
                return;  // a click on the console is not a click on the map
            }
            lastPlacedFrame = Time.frameCount;
            Cheats.World.PlaceAt(Cheats.World.HoveredTile);
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
