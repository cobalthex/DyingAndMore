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
            public int hueristic;
        }

        public PathTile[,] PathInfo { get; set; }

        /// <summary>
        /// Build the hueristic from a speicified start point.
        /// Only overwrites areas that can be reached from start
        /// </summary>
        /// <param name="start">Where to start the fill</param>
        public void BuildHueristic(Point start)
        {
            if (PathInfo == null)
                PathInfo = new PathTile[Height, Width];
            else
                PathInfo.Resize(Height, Width);

            var queue = new Queue<Point>();
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var first = queue.Dequeue();


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
