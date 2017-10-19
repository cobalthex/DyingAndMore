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
        public struct AStarPathNode
        {
            public Point tile;
            public int cost;
            public int score;

            public AStarPathNode(Point tile, int cost, int score)
            {
                this.tile = tile;
                this.cost = cost;
                this.score = score;
            }
        }

        readonly static Point[] SearchDirections =
        {
            new Point(-1, -1),
            new Point( 0, -1),
            new Point( 1, -1),
            new Point(-1,  0),
            new Point( 1,  0),
            new Point(-1,  1),
            new Point( 0,  1),
            new Point( 1,  1)
        };

        //fluids should affect cost of moving through tile

        public List<Point> AStarBuildPath(Vector2 start, Vector2 goal)
        {
            var pstart = (start / Class.TileSize).ToPoint();
            var pgoal = (goal / Class.TileSize).ToPoint();

            AStarPathNode goalNode = new AStarPathNode(pgoal, 0, 0);
            goalNode.score = MapClass.ManhattanDistance(pstart, pgoal);

            var open = new List<AStarPathNode>();
            var closed = new List<AStarPathNode>();
            var traveled = new List<Tuple<AStarPathNode, AStarPathNode>>(); //current and link

            open.Add(new AStarPathNode(pstart, 0, 0));
            //pathFindScores[(int)start.X, (int)start.Y] = GetManhattanDist(start, goal);

            while (open.Count > 0)
            {
                AStarPathNode current = new AStarPathNode(Point.Zero, 0, int.MaxValue);

                foreach (var search in open) //find lowest score
                {
                    if (search.score < current.score)
                        current = search;
                }

                if (current.Equals(goal)) //path navigated
                    throw new NotImplementedException(); //return ReconstructPath(traveled, goal);

                open.Remove(current);
                closed.Add(current);
                //search in 8 directions around point
                foreach (var dir in SearchDirections)
                {
                    var nbr = new AStarPathNode(current.tile + dir, 0, 0); //neighbor

                    if (closed.Contains(nbr) || !CanPath(nbr.x, nbr.y)) //neighbor part of closed set or cannot be pathed on
                        continue;

                    //if diagonal check if cardinals are blocked to disable corner cutting
                    if ((System.Math.Abs(dir.x) == System.Math.Abs(dir.y)) && (!CanPath(current.x, nbr.y) || !CanPath(nbr.x, current.y)))
                        continue;

                    int cost = current.cost + dir.cost;
                    int idx = open.IndexOf(nbr);
                    if (idx < 0 || cost <= open[idx].cost) //if the open list does not contain the neighbor or the neighbor has a better score
                    {
                        traveled.Add(new System.Tuple<AStarPathNode, AStarPathNode>(nbr, current));
                        nbr.cost = cost;
                        nbr.score = cost + ManhattanDist(nbr.tile, pgoal);
                        if (idx < 0)
                            open.Add(nbr);
                        else
                            open[idx] = nbr;
                    }
                }

            }

            return new List<Point>(new[] { pstart }); //No path between start and goal
        }
    }
    }
}
