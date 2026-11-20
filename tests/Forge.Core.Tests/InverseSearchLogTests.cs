using System.Linq;
using Forge.Core;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Forge.Core.Tests
{
    public class InverseTableTests
    {
        [Fact]
        public void TryInvert_SpawnObservatory_GivesRemoveAtSameTile()
        {
            var done = new OrderRecord { Type = "EditorOrderSpawnObservatoryAt" };
            done.Tiles["TileIndex"] = 4870;

            Assert.True(InverseTable.TryInvert(done, null, out var undo));
            Assert.Equal("EditorOrderRemoveObservatoryAt", undo.Type);
            Assert.Equal(4870, undo.Tiles["TileIndex"]);
        }

        [Fact]
        public void TryInvert_CreateCity_MapsToTheDestroyFieldName()
        {
            var done = new OrderRecord { Type = "EditorOrderCreateCityAt" };
            done.Tiles["CityTileIndex"] = 3120;

            Assert.True(InverseTable.TryInvert(done, null, out var undo));
            Assert.Equal("EditorOrderDestroySettlement", undo.Type);
            Assert.Equal(3120, undo.Tiles["SettlementTileIndex"]);
        }

        [Fact]
        public void TryInvert_ClearMountain_ReturnsFalse()
        {
            var done = new OrderRecord { Type = "EditorOrderClearMountain" };
            done.Tiles["DestinationTileIndex"] = 3988;

            Assert.False(InverseTable.TryInvert(done, null, out var undo));
            Assert.Null(undo);
        }

        [Fact]
        public void TryInvert_SetMoney_UsesCapturedPreviousValue()
        {
            var done = new OrderRecord { Type = "EditorOrderSetMoneyStock" };
            done.Scalars["MoneyStock"] = new JValue(1_000_000);

            Assert.True(InverseTable.TryInvert(done, field => field == "MoneyStock" ? 2310 : (int?)null, out var undo));
            Assert.Equal("EditorOrderSetMoneyStock", undo.Type);
            Assert.Equal(2310, undo.Scalars["MoneyStock"].Value<int>());
            Assert.Equal(1_000_000, done.Scalars["MoneyStock"].Value<int>());
        }

        [Fact]
        public void TryInvert_SetMoney_WithoutPreviousValue_ReturnsFalse()
        {
            var done = new OrderRecord { Type = "EditorOrderSetMoneyStock" };
            done.Scalars["MoneyStock"] = new JValue(5);

            Assert.False(InverseTable.TryInvert(done, _ => null, out _));
        }
    }

    public class CommandIndexTests
    {
        [Fact]
        public void Search_WordPrefixOutranksSubstring()
        {
            var index = new CommandIndex<string>();
            index.Add("mid", "Clear underground dungeon", "AI & Story");
            index.Add("prefix", "Remove dungeon", "World › Places");
            index.Add("other", "Plant forest", "World");

            var results = index.Search("dung");

            Assert.Equal(new[] { "mid", "prefix" }, results.OrderBy(r => r).ToArray());
            index.Add("inner", "Redungeoned", "World");
            Assert.Equal("inner", index.Search("dung").Last());
        }

        [Fact]
        public void Search_MatchesLocationWhenLabelDoesNot()
        {
            var index = new CommandIndex<string>();
            index.Add("spawn", "Spawn observatory", "World › Places");

            Assert.Equal(new[] { "spawn" }, index.Search("places"));
        }

        [Fact]
        public void Search_EmptyQuery_ReturnsNothing()
        {
            var index = new CommandIndex<string>();
            index.Add("a", "Anything", "Tab");

            Assert.Empty(index.Search("   "));
        }
    }

    public class EditLogTests
    {
        [Fact]
        public void EditLog_OverCapacity_DropsOldest()
        {
            var log = new EditLog(capacity: 3);
            for (var i = 1; i <= 5; i++)
            {
                log.Add(new EditEntry { Label = "edit " + i });
            }

            Assert.Equal(3, log.Count);
            Assert.Equal(5, log.TotalAdded);
            Assert.Equal(new[] { "edit 5", "edit 4", "edit 3" }, log.Latest(10).Select(e => e.Label));
        }
    }
}
