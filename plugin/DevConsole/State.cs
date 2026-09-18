using BepInEx.Configuration;
using UnityEngine;

namespace DevConsole
{
    /// <summary>Console settings, persisted by BepInEx in BepInEx/config/angelo.el2.devconsole.cfg.</summary>
    internal sealed class State
    {
        public const int InstantFactor = 1000;
        public static readonly int[] MultiplierSteps = { 1, 2, 10, 100, 1000 };

        public readonly ConfigEntry<KeyboardShortcut> Hotkey;
        public readonly ConfigEntry<float> UiScale;
        public readonly ConfigEntry<bool> NativeOverlay;
        public readonly ConfigEntry<int> Amount;
        public readonly ConfigEntry<int> DustMultiplier;
        public readonly ConfigEntry<int> IndustryMultiplier;
        public readonly ConfigEntry<int> ScienceMultiplier;
        public readonly ConfigEntry<int> InfluenceMultiplier;
        public readonly ConfigEntry<bool> InstantBuild;
        public readonly ConfigEntry<bool> InstantResearch;
        public readonly ConfigEntry<bool> Invulnerable;
        public readonly ConfigEntry<bool> OneHitKills;

        public State(ConfigFile config)
        {
            Hotkey = config.Bind("General", "Hotkey", new KeyboardShortcut(KeyCode.Insert), "Shows / hides the console window.");
            NativeOverlay = config.Bind("General", "NativeOverlay", true, "Unlock the game's own debug overlay (F2). Turn off if it misbehaves; the Insert window is unaffected.");
            UiScale = config.Bind("General", "UiScale", 0f, "Window scale; 0 = auto from screen height (2 at 4K), so the window stays readable at high resolutions.");
            Amount = config.Bind("General", "Amount", 10_000, "Units added per resource button press.");
            DustMultiplier = config.Bind("Yields", "Dust", 1, "Multiplies Dust income (1 = off).");
            IndustryMultiplier = config.Bind("Yields", "Industry", 1, "Multiplies city production (1 = off).");
            ScienceMultiplier = config.Bind("Yields", "Science", 1, "Multiplies research income (1 = off).");
            InfluenceMultiplier = config.Bind("Yields", "Influence", 1, "Multiplies Influence income (1 = off).");
            // descriptions double as the window's toggle labels
            InstantBuild = config.Bind("Instant", "Build", false, "Instant build (production x1000: anything completes next turn)");
            InstantResearch = config.Bind("Instant", "Research", false, "Instant research (science x1000: a technology per turn)");
            Invulnerable = config.Bind("Combat", "Invulnerable", false, "Invulnerable (your units take no damage)");
            OneHitKills = config.Bind("Combat", "OneHitKills", false, "One-hit kills (your units deal 99,999 damage)");
        }

        public int EffectiveIndustry => InstantBuild.Value ? InstantFactor : IndustryMultiplier.Value;
        public int EffectiveScience => InstantResearch.Value ? InstantFactor : ScienceMultiplier.Value;

        /// <summary>Configured scale, or an auto value from screen height (1 at 1080p, ~2 at 4K) when set to 0.</summary>
        public float EffectiveUiScale =>
            UiScale.Value > 0f ? UiScale.Value : Mathf.Clamp(Mathf.Round(Screen.height / 1080f), 1f, 3f);
    }
}
