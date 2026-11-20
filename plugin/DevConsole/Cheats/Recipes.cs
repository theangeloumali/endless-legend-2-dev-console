using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using Forge.Core;

namespace DevConsole.Cheats
{
    /// <summary>
    /// Records accepted edits and replays them on another game. Tiles are stored as hex steps from the capital,
    /// so a recipe lands in the same shape around a new capital; steps that fall off the map, or use an order this
    /// build lacks, are skipped and reported rather than posted.
    /// </summary>
    internal static class Recipes
    {
        public sealed class Listing
        {
            public string Name;
            public string Path;
            public int Steps;
            public string Build;
            public List<string> Problems;
        }

        public static string Folder => System.IO.Path.Combine(Paths.ConfigPath, "forge-recipes");

        private static Recipe recording;
        private static HexGrid? recordingGrid;

        public static bool Recording => recording != null;

        public static int RecordedSteps => recording?.Steps.Count ?? 0;

        public static void Initialize() => Edits.Added += Capture;

        public static void StartRecording()
        {
            var capital = Sim.CapitalTile();
            recordingGrid = Sim.Grid();
            var relative = capital >= 0 && recordingGrid != null;
            recording = new Recipe
            {
                GameBuild = SelfCheck.GameVersion,
                CreatedUtc = DateTime.UtcNow,
                Anchor = relative ? RecipeAnchor.Capital : RecipeAnchor.Absolute,
                AnchorTile = relative ? capital : -1,
            };
            Orders.Log.LogInfo(relative
                ? $"recipe: recording relative to the capital at tile {capital}"
                : "recipe: recording at absolute tiles (no capital yet, or this map's hex layout was not recognised)");
        }

        public static void StopRecording() => recording = null;

        public static void Save(string name)
        {
            if (recording == null || recording.Steps.Count == 0)
            {
                return;
            }
            Directory.CreateDirectory(Folder);
            recording.Name = string.IsNullOrWhiteSpace(name) ? "recipe" : name.Trim();
            var path = System.IO.Path.Combine(Folder, FileName(recording.Name) + ".json");
            File.WriteAllText(path, RecipeCodec.Write(recording));
            Orders.Log.LogInfo($"recipe saved: {path} ({recording.Steps.Count} steps)");
            recording = null;
        }

        public static List<Listing> List()
        {
            if (!Directory.Exists(Folder))
            {
                return new List<Listing>();
            }
            return Directory.GetFiles(Folder, "*.json").OrderBy(path => path).Select(path =>
            {
                var recipe = Load(path, out var problems);
                return new Listing
                {
                    Name = recipe?.Name ?? System.IO.Path.GetFileNameWithoutExtension(path),
                    Path = path,
                    Steps = recipe?.Steps.Count ?? 0,
                    Build = recipe?.GameBuild ?? "?",
                    Problems = problems,
                };
            }).ToList();
        }

        /// <summary>atSameTiles replays the recorded tile numbers, which only makes sense on the same map.</summary>
        public static void Replay(Listing listing, bool atSameTiles)
        {
            var recipe = Load(listing.Path, out var problems);
            if (recipe == null)
            {
                Report(listing.Name, 0, 0, problems);
                return;
            }
            var grid = Sim.Grid();
            var capital = Sim.CapitalTile();
            var relative = !atSameTiles && recipe.Anchor == RecipeAnchor.Capital;
            if (relative && (grid == null || capital < 0))
            {
                problems.Add("no capital or unrecognised map layout here, so tiles were replayed as recorded");
                relative = false;
            }
            int posted = 0, skipped = 0;
            foreach (var step in recipe.Steps)
            {
                var reason = Prepare(step, relative, grid, capital, out var record);
                var order = reason == null ? OrderCodec.Build(record, out reason) : null;
                if (order == null)
                {
                    skipped++;
                    problems.Add($"{step.Order.Type}: {reason}");
                    continue;
                }
                Orders.PostAny(order, $"recipe {recipe.Name}: {step.Order.Type}", EditOrigin.Replay);
                posted++;
            }
            Report(recipe.Name, posted, skipped, problems);
        }

        private static void Capture(EditEntry entry)
        {
            if (recording == null || entry.Origin != EditOrigin.User || entry.Outcome != EditOutcome.Posted
                || entry.Record == null || entry.Record.Unrecordable)
            {
                return;
            }
            var step = new RecipeStep { Order = entry.Record.Clone() };
            if (recording.Anchor == RecipeAnchor.Capital && recordingGrid is HexGrid grid)
            {
                foreach (var tile in step.Order.Tiles.Where(tile => grid.Contains(tile.Value)))
                {
                    step.RelativeTiles[tile.Key] = grid.Relative(recording.AnchorTile, tile.Value);
                }
            }
            recording.Steps.Add(step);
        }

        /// <summary>Null when the step can be built; otherwise why it is skipped.</summary>
        private static string Prepare(RecipeStep step, bool relative, HexGrid? grid, int capital, out OrderRecord record)
        {
            record = step.Order.Clone();
            foreach (var field in record.Tiles.Keys.ToList())
            {
                if (relative && grid is HexGrid map && step.RelativeTiles.TryGetValue(field, out var offset))
                {
                    if (!map.TryApply(capital, offset, out var tile))
                    {
                        return "off map";
                    }
                    record.Tiles[field] = tile;
                }
                else if (grid is HexGrid bounds && !bounds.Contains(record.Tiles[field]) && record.Tiles[field] >= 0)
                {
                    return "off map";
                }
            }
            return null;
        }

        private static Recipe Load(string path, out List<string> problems)
        {
            try
            {
                return RecipeCodec.Read(File.ReadAllText(path), out problems);
            }
            catch (IOException exception)
            {
                problems = new List<string> { exception.Message };
                return null;
            }
        }

        private static void Report(string name, int posted, int skipped, List<string> problems)
        {
            var summary = $"recipe {name}: {posted} posted, {skipped} skipped";
            Edits.Add(new EditEntry
            {
                Turn = Sim.Turn,
                Label = summary,
                Origin = EditOrigin.Replay,
                Outcome = skipped == 0 && problems.Count == 0 ? EditOutcome.Posted : EditOutcome.Skipped,
                Reason = problems.Count == 0 ? null : string.Join("; ", problems.Take(3)) + (problems.Count > 3 ? " …" : string.Empty),
            });
            Orders.Log.LogInfo(summary + (problems.Count == 0 ? string.Empty : ": " + string.Join("; ", problems)));
        }

        private static string FileName(string name) =>
            new string(name.Select(c => System.IO.Path.GetInvalidFileNameChars().Contains(c) ? '-' : c).ToArray());
    }
}
