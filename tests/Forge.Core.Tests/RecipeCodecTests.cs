using System;
using Forge.Core;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Forge.Core.Tests
{
    public class RecipeCodecTests
    {
        private static Recipe Sample()
        {
            var order = new OrderRecord { Type = "EditorOrderSpawnDungeonOrSpawnPointAt" };
            order.Tiles["TileIndex"] = 812;
            order.Scalars["DangerLevel"] = new JValue(3);
            order.Scalars["DefinitionName"] = new JValue("Curiosity_Ancient");
            order.LocalEmpireFields.Add("EmpireIndex");
            var step = new RecipeStep { Order = order };
            step.RelativeTiles["TileIndex"] = new HexOffset(2, -1);
            return new Recipe
            {
                Name = "fortress-start",
                GameBuild = "1.0.239",
                CreatedUtc = new DateTime(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc),
                Anchor = RecipeAnchor.Capital,
                AnchorTile = 800,
                Steps = { step },
            };
        }

        [Fact]
        public void Write_ThenRead_RoundTripsEveryField()
        {
            var read = RecipeCodec.Read(RecipeCodec.Write(Sample()), out var problems);

            Assert.Empty(problems);
            Assert.Equal("fortress-start", read.Name);
            Assert.Equal("1.0.239", read.GameBuild);
            Assert.Equal(new DateTime(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc), read.CreatedUtc.ToUniversalTime());
            Assert.Equal(RecipeAnchor.Capital, read.Anchor);
            Assert.Equal(800, read.AnchorTile);
            var step = Assert.Single(read.Steps);
            Assert.Equal("EditorOrderSpawnDungeonOrSpawnPointAt", step.Order.Type);
            Assert.Equal(812, step.Order.Tiles["TileIndex"]);
            Assert.Equal(3, step.Order.Scalars["DangerLevel"].Value<int>());
            Assert.Equal("Curiosity_Ancient", step.Order.Scalars["DefinitionName"].Value<string>());
            Assert.Contains("EmpireIndex", step.Order.LocalEmpireFields);
            Assert.Equal(new HexOffset(2, -1), step.RelativeTiles["TileIndex"]);
        }

        [Fact]
        public void Read_UnknownFormat_ReportsProblem()
        {
            var recipe = RecipeCodec.Read("{\"Format\": 99, \"Name\": \"future\"}", out var problems);

            Assert.Equal(99, recipe.Format);
            Assert.Empty(recipe.Steps);
            Assert.Contains(problems, problem => problem.Contains("format 99"));
        }

        [Fact]
        public void Read_MissingStepType_ReportsProblemAndKeepsOtherSteps()
        {
            var json = JObject.Parse(RecipeCodec.Write(Sample()));
            ((JArray)json["Steps"]).Insert(0, new JObject { ["Order"] = new JObject() });

            var recipe = RecipeCodec.Read(json.ToString(), out var problems);

            Assert.Single(recipe.Steps);
            Assert.Contains(problems, problem => problem.StartsWith("step 1"));
        }

        [Fact]
        public void Read_WrongTypedTile_SkipsThatStepOnly()
        {
            var json = JObject.Parse(RecipeCodec.Write(Sample()));
            var bad = (JObject)json["Steps"][0].DeepClone();
            bad["Order"]["Tiles"]["TileIndex"] = "abc";
            ((JArray)json["Steps"]).Add(bad);

            var recipe = RecipeCodec.Read(json.ToString(), out var problems);

            Assert.Single(recipe.Steps);
            Assert.Contains(problems, problem => problem.StartsWith("step 2: malformed"));
        }

        [Fact]
        public void Read_WrongTypedFormat_ReportsProblemInsteadOfThrowing()
        {
            var recipe = RecipeCodec.Read("{\"Format\": \"one\", \"Steps\": []}", out var problems);

            Assert.NotNull(recipe);
            Assert.Contains(problems, problem => problem.StartsWith("recipe header is malformed"));
        }

        [Fact]
        public void Read_InvalidJson_ReturnsNullWithProblem()
        {
            Assert.Null(RecipeCodec.Read("{ not json", out var problems));
            Assert.Single(problems);
        }
    }
}
