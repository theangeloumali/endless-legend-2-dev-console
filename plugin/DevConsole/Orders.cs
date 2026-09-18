using System;
using Amplitude.Mercury.Interop;
using Amplitude.Mercury.Sandbox;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;

namespace DevConsole
{
    /// <summary>
    /// Posts the game's own order DTOs. This is the path Amplitude's in-house editor uses: PostOrder enqueues onto
    /// a ConcurrentQueue that the simulation thread drains, so it is safe to call from OnGUI and the order is
    /// validated by the game before it applies.
    /// </summary>
    internal static class Orders
    {
        private static ManualLogSource log;

        public static void Initialize(ManualLogSource logger) => log = logger;

        public static ManualLogSource Log => log;

        /// <summary>True once a game is running; posting before that only logs on the game's side.</summary>
        public static bool Ready => SandboxManager.IsSandboxAlive && Sim.LocalEmpireIndex >= 0;

        /// <summary>A plain order carries no empire field — it is routed to the department of the target empire,
        /// so it must be posted against our own index rather than the default -1.</summary>
        public static void Post(Order order, string what) =>
            Send(order, what, () => SandboxManager.PostOrder(order, Sim.LocalEmpireIndex));

        /// <summary>Editor orders are checked by the game before posting, because a rejected one is silently
        /// dropped — a village on the wrong terrain or a camp your faction cannot build would otherwise look like
        /// the console doing nothing at all.</summary>
        public static void Post(EditorOrder order, string what)
        {
            if (!Allowed(order, what))
            {
                return;
            }
            Send(order, what, () => SandboxManager.PostOrder(order));
        }

        private static readonly MethodInfo Validate =
            AccessTools.Method(AccessTools.TypeByName("Amplitude.Mercury.Simulation.EditorOrderProcessors"), "ValidateOrder");

        private static bool Allowed(EditorOrder order, string what)
        {
            if (Validate == null)
            {
                return true;  // no validator on this build: post and let the game decide
            }
            try
            {
                if ((bool)Validate.Invoke(null, new object[] { order }))
                {
                    return true;
                }
                log.LogWarning($"{what}: the game rejected this — not valid here (terrain, ownership or a faction ability)");
                return false;
            }
            catch (Exception exception)
            {
                log.LogWarning($"{what}: validation threw ({exception.InnerException?.Message ?? exception.Message}); posting anyway");
                return true;
            }
        }

        private static void Send(object order, string what, Action post)
        {
            if (!Guard(order, what))
            {
                return;
            }
            try
            {
                post();
                log.LogInfo($"posted {what}");
            }
            catch (Exception exception)
            {
                log.LogError($"{what} failed: {exception.Message}");
            }
        }

        private static bool Guard(object order, string what)
        {
            if (order == null)
            {
                log.LogWarning($"{what}: order was null");
                return false;
            }
            if (Ready)
            {
                return true;
            }
            log.LogWarning($"{what}: no game running");
            return false;
        }
    }
}
