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

        public string Location => string.IsNullOrEmpty(Section) ? Tab : Tab + " › " + Section;
    }

    /// <summary>
    /// The palette's index is rebuilt by drawing every tab off-screen while it is open: each Widgets button
    /// records itself here. That keeps search in step with every tab without a second list of commands.
    /// </summary>
    internal static class Commands
    {
        public static readonly CommandIndex<Command> Index = new CommandIndex<Command>();

        private static readonly Dictionary<string, float> SectionTops = new Dictionary<string, float>();

        public static bool Collecting { get; private set; }

        public static string CurrentTab { get; private set; }

        public static string CurrentSection { get; private set; }

        public static void BeginTab(string tab, bool collecting)
        {
            CurrentTab = tab;
            CurrentSection = null;
            Collecting = collecting;
        }

        public static void EndTab() => Collecting = false;

        public static void EnterSection(string title) => CurrentSection = title;

        public static void Record(string label, Action run, Action<int> runAtTile, bool enabled, bool destructive)
        {
            if (!Collecting || string.IsNullOrEmpty(label))
            {
                return;
            }
            var command = new Command
            {
                Tab = CurrentTab,
                Section = CurrentSection,
                Label = label,
                Run = run,
                RunAtTile = runAtTile,
                Enabled = enabled,
                Destructive = destructive,
            };
            Index.Add(command, label, command.Location);
        }

        /// <summary>Section positions are scroll-view coordinates, captured on Repaint of the visible tab.</summary>
        public static void RememberSectionTop(string section, float top) => SectionTops[CurrentTab + "\n" + section] = top;

        public static float? SectionTop(string tab, string section) =>
            section != null && SectionTops.TryGetValue(tab + "\n" + section, out var top) ? top : (float?)null;
    }
}
