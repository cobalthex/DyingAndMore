using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Takai.Game
{
    public partial class MapClass
    {
        public static int ManhattanDistance(Point start, Point end)
        {
            const int StraightCost = 10;
            const int DiagonalCost = 14;

            var dx = Math.Abs(start.X - end.X);
            var dy = Math.Abs(start.Y - end.Y);
            return StraightCost * (dx + dy) + (DiagonalCost - 2 * StraightCost) * Math.Min(dx, dy);
        }

        public struct PathTile
        {
            internal bool marked;
            public uint heuristic;
        }

        [Data.Serializer.Ignored]
        public PathTile[,] PathInfo { get; set; }

        struct HeuristicPoint
        {
            public Point tile;
            public uint value;
        }

        internal uint MaxHeuristic = 0;

        /// <summary>
        /// Build the hueristic from a speicified start point.
        /// Only overwrites areas that can be reached from start
        /// </summary>
        /// <param name="start">Where to start the fill</param>
        public void BuildHeuristic(Point start)
        {
            PathInfo = new PathTile[Height, Width];
            for (int y = 0; y < Height; ++y)
                for (int x = 0; x < Width; ++x)
                    PathInfo[y, x] = new PathTile { heuristic = uint.MaxValue };

            if (Tiles[start.Y, start.X] < 0)
                return;

            var queue = new Queue<HeuristicPoint>();
            queue.Enqueue(new HeuristicPoint { tile = start, value = 1 });
            while (queue.Count > 0)
            {
                var first = queue.Dequeue();

                MaxHeuristic = Math.Max(MaxHeuristic, first.value);

                var left = first.tile.X;
                var right = first.tile.X;
                for (; left > 0 && Tiles[first.tile.Y, left - 1] >= 0 && !PathInfo[first.tile.Y, left - 1].marked; --left) ;
                for (; right < Width - 1 && Tiles[first.tile.Y, right + 1] >= 0 && !PathInfo[first.tile.Y, right + 1].marked; ++right) ;

                for (; left <= right; ++left)
                {
                    PathInfo[first.tile.Y, left] = new PathTile
                    {
                        heuristic = first.value + (uint)Math.Abs(first.tile.X - left),
                        marked = true,
                    };

                    if (first.tile.Y > 0 && Tiles[first.tile.Y - 1, left] >= 0 && !PathInfo[first.tile.Y - 1, left].marked)
                        queue.Enqueue(new HeuristicPoint { tile = new Point(left, first.tile.Y - 1), value = PathInfo[first.tile.Y, left].heuristic + 1 });

                    if (first.tile.Y < Height - 1 && Tiles[first.tile.Y + 1, left] >= 0 && !PathInfo[first.tile.Y + 1, left].marked)
                        queue.Enqueue(new HeuristicPoint { tile = new Point(left, first.tile.Y + 1), value = PathInfo[first.tile.Y, left].heuristic + 1 });
                }
            }
        }
    }

    public partial class MapInstance
    {
        public List<Point> GetPath(Vector2 start, Vector2 goal)
        {
            return new List<Point>();
        }
    }
}
