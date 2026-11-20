using System;

namespace Forge.Core
{
    /// <summary>Which offset rows or columns are shifted half a hex; tile index is always row * width + column.</summary>
    public enum HexLayout
    {
        OddRow,
        EvenRow,
        OddColumn,
        EvenColumn,
    }

    /// <summary>A step between two tiles in axial coordinates, so it means the same thing on any map.</summary>
    public readonly struct HexOffset : IEquatable<HexOffset>
    {
        public readonly int Dq;
        public readonly int Dr;

        public HexOffset(int dq, int dr)
        {
            Dq = dq;
            Dr = dr;
        }

        public bool Equals(HexOffset other) => Dq == other.Dq && Dr == other.Dr;

        public override bool Equals(object obj) => obj is HexOffset other && Equals(other);

        public override int GetHashCode() => (Dq * 397) ^ Dr;

        public override string ToString() => $"({Dq}, {Dr})";
    }

    /// <summary>Converts tile indexes to axial coordinates and back, so a recipe can store edits relative to an
    /// anchor tile and replay them on a map of a different size.</summary>
    public readonly struct HexGrid
    {
        public readonly int Width;
        public readonly int Height;
        public readonly HexLayout Layout;
        public readonly bool WrapsX;

        public HexGrid(int width, int height, HexLayout layout, bool wrapsX)
        {
            if (width <= 0 || height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width), "map dimensions must be positive");
            }
            Width = width;
            Height = height;
            Layout = layout;
            WrapsX = wrapsX;
        }

        public int TileCount => Width * Height;

        public bool Contains(int tile) => tile >= 0 && tile < TileCount;

        /// <summary>On a wrapping map the shorter way round wins, so an edit just across the seam stays a small step.</summary>
        public HexOffset Relative(int fromTile, int toTile)
        {
            var (fromQ, fromR) = ToAxial(fromTile % Width, fromTile / Width);
            var column = toTile % Width;
            var row = toTile / Width;
            var best = Delta(fromQ, fromR, column, row);
            if (!WrapsX)
            {
                return best;
            }
            foreach (var shifted in new[] { column - Width, column + Width })
            {
                var candidate = Delta(fromQ, fromR, shifted, row);
                if (Distance(candidate) < Distance(best))
                {
                    best = candidate;
                }
            }
            return best;
        }

        /// <summary>False when the step leaves the map; columns wrap only on a wrapping map.</summary>
        public bool TryApply(int anchorTile, HexOffset offset, out int tile)
        {
            tile = -1;
            if (!Contains(anchorTile))
            {
                return false;
            }
            var (q, r) = ToAxial(anchorTile % Width, anchorTile / Width);
            var (column, row) = ToOffset(q + offset.Dq, r + offset.Dr);
            if (row < 0 || row >= Height)
            {
                return false;
            }
            if (column < 0 || column >= Width)
            {
                if (!WrapsX)
                {
                    return false;
                }
                column = ((column % Width) + Width) % Width;
            }
            tile = row * Width + column;
            return true;
        }

        private HexOffset Delta(int fromQ, int fromR, int column, int row)
        {
            var (q, r) = ToAxial(column, row);
            return new HexOffset(q - fromQ, r - fromR);
        }

        private static int Distance(HexOffset offset) =>
            (Math.Abs(offset.Dq) + Math.Abs(offset.Dr) + Math.Abs(offset.Dq + offset.Dr)) / 2;

        // (x & 1) is the parity for negative coordinates too, so (x ± (x & 1)) / 2 stays exact.
        public (int Q, int R) ToAxial(int column, int row)
        {
            switch (Layout)
            {
                case HexLayout.OddRow: return (column - (row - (row & 1)) / 2, row);
                case HexLayout.EvenRow: return (column - (row + (row & 1)) / 2, row);
                case HexLayout.OddColumn: return (column, row - (column - (column & 1)) / 2);
                default: return (column, row - (column + (column & 1)) / 2);
            }
        }

        private (int Column, int Row) ToOffset(int q, int r)
        {
            switch (Layout)
            {
                case HexLayout.OddRow: return (q + (r - (r & 1)) / 2, r);
                case HexLayout.EvenRow: return (q + (r + (r & 1)) / 2, r);
                case HexLayout.OddColumn: return (q, r + (q - (q & 1)) / 2);
                default: return (q, r + (q + (q & 1)) / 2);
            }
        }
    }
}
