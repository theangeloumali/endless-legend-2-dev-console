using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace DevConsole
{
    /// <summary>
    /// Resolves every game type and member this DLL was compiled against (the TypeRef and MemberRef metadata
    /// tables) on the running build. A patch that renames an order field shows up here as a count, not as a
    /// button that silently does nothing.
    /// </summary>
    internal static class SelfCheck
    {
        private const int TypeRefTable = 0x01000000;
        private const int MemberRefTable = 0x0A000000;

        public static bool Done { get; private set; }

        public static int Total { get; private set; }

        /// <summary>Computed once when the check finishes; drawn on every IMGUI event.</summary>
        public static string Summary { get; private set; } = "Self-check: waiting for a game";

        public static readonly List<string> Missing = new List<string>();

        public static string GameVersion => Application.version.Split('+')[0];

        public static void Run()
        {
            if (Done)
            {
                return;
            }
            Done = true;
            var module = typeof(SelfCheck).Module;
            foreach (var table in new[] { TypeRefTable, MemberRefTable })
            {
                // Mono reports both "past the end" and "needs a generic context" as ArgumentException, so the
                // end of a table is a long run of them rather than one specific exception.
                var invalidRun = 0;
                for (var row = 1; invalidRun < 64; row++)
                {
                    try
                    {
                        var member = table == TypeRefTable ? (MemberInfo)module.ResolveType(table | row) : module.ResolveMember(table | row);
                        invalidRun = 0;
                        if (IsGame(member))
                        {
                            Total++;
                        }
                    }
                    catch (ArgumentException)
                    {
                        invalidRun++;
                    }
                    catch (Exception exception) when (exception is MissingMemberException || exception is TypeLoadException)
                    {
                        invalidRun = 0;
                        Total++;
                        Missing.Add(exception.Message);
                    }
                    catch (Exception)
                    {
                        invalidRun = 0;  // unrelated resolution failure: not evidence of a moved game member
                    }
                }
            }
            Summary = Missing.Count == 0
                ? $"Self-check: every game reference found on {GameVersion}"
                : $"Self-check: {Missing.Count} of {Total} game references missing on {GameVersion} (see log)";
            if (Missing.Count == 0)
            {
                Orders.Log.LogInfo($"self-check: all {Total} game references found on {GameVersion}");
            }
            else
            {
                Orders.Log.LogWarning($"self-check: {Missing.Count} of {Total} game references missing on {GameVersion}: "
                                      + string.Join("; ", Missing.Take(10)));
            }
        }

        /// <summary>Only Amplitude's assemblies move between patches; Unity, BepInEx and mscorlib are not counted.</summary>
        private static bool IsGame(MemberInfo member)
        {
            var assembly = (member as Type ?? member.DeclaringType)?.Assembly.GetName().Name ?? string.Empty;
            return assembly.StartsWith("Amplitude") || assembly == "Assembly-CSharp";
        }
    }
}
