using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Forge.Core
{
    public enum RecipeAnchor
    {
        Capital,
        Absolute,
    }

    public sealed class RecipeStep
    {
        public OrderRecord Order;

        /// <summary>Each tile field as a step from the recipe's anchor tile.</summary>
        public Dictionary<string, HexOffset> RelativeTiles = new Dictionary<string, HexOffset>();
    }

    public sealed class Recipe
    {
        public const int CurrentFormat = 1;

        public int Format = CurrentFormat;
        public string Name;
        public string GameBuild;
        public DateTime CreatedUtc;
        public RecipeAnchor Anchor = RecipeAnchor.Capital;
        public int AnchorTile;
        public List<RecipeStep> Steps = new List<RecipeStep>();
    }

    /// <summary>Recipe files, format 1. Reading never throws on bad content: it reports what it skipped so a
    /// recipe shared from another build still loads whatever it can.</summary>
    public static class RecipeCodec
    {
        public static string Write(Recipe recipe)
        {
            var steps = new JArray(recipe.Steps.Select(step => new JObject
            {
                ["Order"] = WriteOrder(step.Order),
                ["RelativeTiles"] = new JObject(step.RelativeTiles.Select(pair =>
                    new JProperty(pair.Key, new JObject { ["Dq"] = pair.Value.Dq, ["Dr"] = pair.Value.Dr }))),
            }));
            var root = new JObject
            {
                ["Format"] = recipe.Format,
                ["Name"] = recipe.Name,
                ["GameBuild"] = recipe.GameBuild,
                ["CreatedUtc"] = recipe.CreatedUtc.ToString("o"),
                ["Anchor"] = recipe.Anchor.ToString(),
                ["AnchorTile"] = recipe.AnchorTile,
                ["Steps"] = steps,
            };
            return root.ToString(Formatting.Indented);
        }

        public static Recipe Read(string json, out List<string> problems)
        {
            problems = new List<string>();
            JObject root;
            try
            {
                // DateParseHandling.None: JObject.Parse would turn "…Z" into a local DateTime and shift CreatedUtc
                using (var reader = new JsonTextReader(new System.IO.StringReader(json)) { DateParseHandling = DateParseHandling.None })
                {
                    root = JObject.Load(reader);
                }
            }
            catch (JsonException exception)
            {
                problems.Add("not valid JSON: " + exception.Message);
                return null;
            }

            var recipe = new Recipe
            {
                Format = root.Value<int?>("Format") ?? 0,
                Name = root.Value<string>("Name"),
                GameBuild = root.Value<string>("GameBuild"),
                AnchorTile = root.Value<int?>("AnchorTile") ?? 0,
            };
            if (recipe.Format != Recipe.CurrentFormat)
            {
                problems.Add($"recipe format {recipe.Format} is not supported (expected {Recipe.CurrentFormat})");
                return recipe;
            }
            if (DateTime.TryParse(root.Value<string>("CreatedUtc"), null, System.Globalization.DateTimeStyles.RoundtripKind, out var created))
            {
                recipe.CreatedUtc = created;
            }
            if (Enum.TryParse(root.Value<string>("Anchor"), out RecipeAnchor anchor))
            {
                recipe.Anchor = anchor;
            }

            var index = 0;
            foreach (var token in root["Steps"] as JArray ?? new JArray())
            {
                index++;
                var step = ReadStep(token as JObject);
                if (step == null)
                {
                    problems.Add($"step {index}: no order type, skipped");
                    continue;
                }
                recipe.Steps.Add(step);
            }
            return recipe;
        }

        private static JObject WriteOrder(OrderRecord order) =>
            new JObject
            {
                ["Type"] = order.Type,
                ["Scalars"] = new JObject(order.Scalars.Select(pair => new JProperty(pair.Key, pair.Value))),
                ["Tiles"] = new JObject(order.Tiles.Select(pair => new JProperty(pair.Key, pair.Value))),
                ["LocalEmpireFields"] = new JArray(order.LocalEmpireFields),
            };

        private static RecipeStep ReadStep(JObject step)
        {
            var order = step?["Order"] as JObject;
            var type = order?.Value<string>("Type");
            if (string.IsNullOrEmpty(type))
            {
                return null;
            }
            var record = new OrderRecord { Type = type };
            foreach (var property in (order["Scalars"] as JObject)?.Properties() ?? Enumerable.Empty<JProperty>())
            {
                record.Scalars[property.Name] = property.Value;
            }
            foreach (var property in (order["Tiles"] as JObject)?.Properties() ?? Enumerable.Empty<JProperty>())
            {
                record.Tiles[property.Name] = property.Value.Value<int>();
            }
            foreach (var field in order["LocalEmpireFields"] as JArray ?? new JArray())
            {
                record.LocalEmpireFields.Add(field.Value<string>());
            }

            var result = new RecipeStep { Order = record };
            foreach (var property in (step["RelativeTiles"] as JObject)?.Properties() ?? Enumerable.Empty<JProperty>())
            {
                var offset = property.Value as JObject;
                result.RelativeTiles[property.Name] = new HexOffset(offset?.Value<int>("Dq") ?? 0, offset?.Value<int>("Dr") ?? 0);
            }
            return result;
        }
    }
}
