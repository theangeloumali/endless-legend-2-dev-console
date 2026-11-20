using System;
using Forge.Core;
using Xunit;

namespace Forge.Core.Tests
{
    public class HexGridTests
    {
        public static TheoryData<HexLayout> Layouts => new TheoryData<HexLayout>
        {
            HexLayout.OddRow, HexLayout.EvenRow, HexLayout.OddColumn, HexLayout.EvenColumn,
        };

        [Theory]
        [MemberData(nameof(Layouts))]
        public void Relative_ThenApply_RoundTrips_OnEveryLayout(HexLayout layout)
        {
            var grid = new HexGrid(20, 12, layout, wrapsX: false);
            var anchor = 5 * 20 + 7;
            foreach (var target in new[] { 0, 19, 4 * 20 + 9, 6 * 20 + 6, 11 * 20 + 19, 3 * 20 + 2 })
            {
                var offset = grid.Relative(anchor, target);
                Assert.True(grid.TryApply(anchor, offset, out var tile));
                Assert.Equal(target, tile);
            }
        }

        [Fact]
        public void TryApply_OffTopEdge_ReturnsFalse()
        {
            var grid = new HexGrid(10, 10, HexLayout.OddRow, wrapsX: true);
            Assert.False(grid.TryApply(5, new HexOffset(0, -1), out _));
        }

        [Fact]
        public void TryApply_PastRightEdge_WrapsOnlyWhenWrapsX()
        {
            var wrapping = new HexGrid(10, 10, HexLayout.OddRow, wrapsX: true);
            var flat = new HexGrid(10, 10, HexLayout.OddRow, wrapsX: false);
            var rightEdge = 2 * 10 + 9;
            var oneEast = new HexOffset(1, 0);

            Assert.True(wrapping.TryApply(rightEdge, oneEast, out var wrapped));
            Assert.Equal(2 * 10, wrapped);
            Assert.False(flat.TryApply(rightEdge, oneEast, out _));
        }

        [Fact]
        public void Relative_AcrossTheSeam_TakesTheShortWayOnAWrappingMap()
        {
            var grid = new HexGrid(10, 10, HexLayout.OddRow, wrapsX: true);
            var offset = grid.Relative(2 * 10 + 9, 2 * 10);
            Assert.Equal(new HexOffset(1, 0), offset);
        }

        [Fact]
        public void TryApply_OnSmallerMap_FallsOffAndReturnsFalse()
        {
            var big = new HexGrid(40, 30, HexLayout.OddRow, wrapsX: false);
            var small = new HexGrid(20, 12, HexLayout.OddRow, wrapsX: false);
            var offset = big.Relative(5 * 40 + 5, 25 * 40 + 30);

            Assert.False(small.TryApply(5 * 20 + 5, offset, out _));
        }

        [Fact]
        public void Constructor_RejectsEmptyMap()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new HexGrid(0, 10, HexLayout.OddRow, false));
        }
    }
}
