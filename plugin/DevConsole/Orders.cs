using System;
using Amplitude.Mercury.Interop;
using Amplitude.Mercury.Sandbox;
using System.Reflection;
using BepInEx.Logging;
using Forge.Core;
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

        /// <summary>Raised just before posting, so undo can read a value the order is about to overwrite.</summary>
        public static event Action<object> Posting;

        /// <summary>Every attempt the game saw: posted, rejected by its validator, or thrown while posting.</summary>
        public static event Action<object, string, EditOrigin, EditOutcome, string> Outcome;

        /// <summary>True once a game is running; posting before that only logs on the game's side.</summary>
        public static bool Ready => SandboxManager.IsSandboxAlive && Sim.LocalEmpireIndex >= 0;

        /// <summary>A plain order carries no empire field — it is routed to the department of the target empire,
        /// so it must be posted against our own index rather than the default -1.</summary>
        /// <summary>True when the order reached the game's queue (not rejected, not failed).</summary>
        public static bool Post(Order order, string what, EditOrigin origin = EditOrigin.User) =>
            Send(order, what, origin, () => SandboxManager.PostOrder(order, Sim.LocalEmpireIndex));

        /// <summary>Editor orders are checked by the game before posting, because a rejected one is silently
        /// dropped — a village on the wrong terrain or a camp your faction cannot build would otherwise look like
        /// the console doing nothing at all.</summary>
        public static bool Post(EditorOrder order, string what, EditOrigin origin = EditOrigin.User) =>
            Allowed(order, what, origin) && Send(order, what, origin, () => SandboxManager.PostOrder(order));

        /// <summary>For orders rebuilt from a record (undo, recipes), whose static type is only object.</summary>
        public static bool PostAny(object order, string what, EditOrigin origin)
        {
            switch (order)
            {
                case EditorOrder editorOrder:
                    return Post(editorOrder, what, origin);
                case Order plainOrder:
                    return Post(plainOrder, what, origin);
                default:
                    log.LogWarning($"{what}: {order?.GetType().Name ?? "null"} is not a game order");
                    return false;
            }
        }

        private static readonly MethodInfo Validate =
            AccessTools.Method(AccessTools.TypeByName("Amplitude.Mercury.Simulation.EditorOrderProcessors"), "ValidateOrder");

        private static bool Allowed(EditorOrder order, string what, EditOrigin origin)
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
                Raise(order, what, origin, EditOutcome.Rejected, "not valid here");
                return false;
            }
            catch (Exception exception)
            {
                log.LogWarning($"{what}: validation threw ({exception.InnerException?.Message ?? exception.Message}); posting anyway");
                return true;
            }
        }

        private static bool Send(object order, string what, EditOrigin origin, Action post)
        {
            if (!Guard(order, what))
            {
                return false;
            }
            try
            {
                Notify(() => Posting?.Invoke(order), what);
                post();
                log.LogInfo($"posted {what}");
                Raise(order, what, origin, EditOutcome.Posted, null);
                return true;
            }
            catch (Exception exception)
            {
                log.LogError($"{what} failed: {exception.Message}");
                Raise(order, what, origin, EditOutcome.Failed, exception.Message);
                return false;
            }
        }

        /// <summary>A listener bug must never stop an order or take the console down with it.</summary>
        private static void Raise(object order, string what, EditOrigin origin, EditOutcome outcome, string reason) =>
            Notify(() => Outcome?.Invoke(order, what, origin, outcome, reason), what);

        private static void Notify(Action listeners, string what)
        {
            try
            {
                listeners();
            }
            catch (Exception exception)
            {
                log.LogError($"edit log listener failed for {what}: {exception.Message}");
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
