using System.Reflection;
using Amplitude.UI.DebugOverlay;
using BepInEx.Logging;
using HarmonyLib;

namespace DevConsole
{
    /// <summary>
    /// Amplitude shipped their debug overlay in the retail build but compiled DebugOverlayManager.IsEnabled to
    /// "return false", so the window group is dropped during UI boot. Forcing it true restores the overlay and
    /// its F2 binding. Patched from Awake, before the UI service builds its groups.
    /// </summary>
    internal static class DebugOverlay
    {
        public static void Enable(Harmony harmony, ManualLogSource log, bool wanted)
        {
            if (!wanted)
            {
                return;
            }
            var getter = AccessTools.PropertyGetter(typeof(DebugOverlayManager), "IsEnabled");
            if (getter == null)
            {
                log.LogWarning("DebugOverlayManager.IsEnabled not found; the native overlay stays off.");
                return;
            }
            harmony.Patch(getter, postfix: new HarmonyMethod(typeof(DebugOverlay)
                .GetMethod(nameof(ForceEnabled), BindingFlags.Static | BindingFlags.NonPublic)));
            log.LogInfo("Native debug overlay enabled - press F2 in game.");
        }

        private static void ForceEnabled(ref bool __result) => __result = true;
    }
}
