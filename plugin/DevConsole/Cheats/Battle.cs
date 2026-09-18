using Amplitude.Mercury;

namespace DevConsole.Cheats
{
    /// <summary>
    /// Amplitude's own battle cheats, shipped in the retail build. Nothing is patched here — BattleDebug is public
    /// and static. writeRegistry stays false so a cheat never outlives the session.
    /// </summary>
    internal static class Battle
    {
        public static readonly (BattleCheatType Cheat, string Label)[] All =
        {
            (BattleCheatType.InfiniteMovement, "Infinite movement"),
            (BattleCheatType.InfiniteActionToken, "Infinite action tokens"),
            (BattleCheatType.InfiniteBattleSkill, "Infinite battle skills"),
            (BattleCheatType.IgnoreZoneOfControl, "Ignore zone of control"),
            (BattleCheatType.IgnoreRoundCount, "Ignore round count"),
            (BattleCheatType.IgnoreEmpirePlaying, "Ignore empire playing"),
            (BattleCheatType.LineOfSightDebug, "Line of sight debug"),
        };

        public static bool Get(BattleCheatType cheat) => BattleDebug.GetCheat(cheat);

        public static void Set(BattleCheatType cheat, bool value) => BattleDebug.SetCheat(cheat, value, false);

        public static bool AnyEnabled => BattleDebug.IsAnyCheatEnabled();

        public static void ClearAll() => BattleDebug.DeactivateCheats();
    }
}
