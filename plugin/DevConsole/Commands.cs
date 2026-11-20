using System;
using System.Collections.Generic;
using Forge.Core;

namespace DevConsole
{
    /// <summary>One button the search palette can find. Exactly one of Run and RunAtTile is set.</summary>
    internal sealed class Command
    {
        public string Tab;
        public string Section;
        public string Label;
        public Action Run;
        public Action<int> RunAtTile;
        public bool Enabled;
        public bool Destructive;

        /// <summary>A one-click map pick rather than a repeating tile tool.</summary>
        public bool Pick;

        public string Location => string.IsNullOrEmpty(Section) ? Tab : Tab + " › " + Section;
    }

    /// <summary>
    /// The palette's index is built by drawing every tab off-screen once when it opens: each Widgets button
    /// records itself here. That keeps search in step with every tab without a second list of commands.
    /// </summary>
    internal static class Commands
    {
        public static readonly CommandIndex<Command> Index = new CommandIndex<Command>();

        /// <summary>Bumped on every rebuild, so the palette knows when its cached results are stale.</summary>
        public static int Version { get; private set; }

        private static readonly Dictionary<string, float> SectionTops = new Dictionary<string, float>();
        private static string currentTab;
        private static string currentSection;

        public static bool Collecting { get; private set; }

        public static void BeginCollecting()
        {
            Index.Clear();
            Version++;
        }

        public static void BeginTab(string tab, bool collecting)
        {
            currentTab = tab;
            currentSection = null;
            Collecting = collecting;
        }

        public static void EndTab() => Collecting = false;

        public static void EnterSection(string title) => currentSection = title;

        public static void Record(string label, Action run, Action<int> runAtTile, bool enabled, bool destructive, bool pick)
        {
            if (!Collecting || string.IsNullOrEmpty(label))
            {
                return;
            }
            var command = new Command
            {
                Tab = currentTab,
                Section = currentSection,
                Label = label,
                Run = run,
                RunAtTile = runAtTile,
                Enabled = enabled,
                Destructive = destructive,
                Pick = pick,
            };
            Index.Add(command, label, command.Location);
        }

        /// <summary>Section positions are scroll-view coordinates, captured on Repaint of the visible tab.</summary>
        public static void RememberSectionTop(string section, float top) => SectionTops[currentTab + "\n" + section] = top;

        public static float? SectionTop(string tab, string section) =>
            section != null && SectionTops.TryGetValue(tab + "\n" + section, out var top) ? top : (float?)null;
    }
}
