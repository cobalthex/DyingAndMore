using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Takai.Game
{
    public partial class MapClass
    {
        public struct PathTile
        {
            public uint heuristic;
            internal uint generation;
        }
        internal uint pathGeneration = 0;

        [Data.Serializer.Ignored]
        public PathTile[,] PathInfo { get; set; } = new PathTile[0, 0];

        public struct HeuristicScore
        {
            public Point tile;
            public uint value;
        }

        internal uint MaxHeuristic = 0;

        internal static readonly Point[] HeuristicDirections =
        {
            new Point( 0, -1),
            new Point(-1,  0),
            new Point( 0,  1),
            new Point( 1,  0),
        };

        public static readonly Point[] NavigationDirections =
        {
            //new Point(-1, -1),
            new Point( 0, -1),
            //new Point( 1, -1),
            new Point(-1,  0),
            new Point( 1,  0),
            //new Point(-1,  1),
            new Point( 0,  1),
            //new Point( 1,  1),
        };

        public bool CanPath(Point tile)
        {
            if (!TileBounds.Contains(tile))
                return false;
            return Tiles[tile.Y, tile.X] >= 0;
        }

        //dynamic heuristic that only builds to visible region/entities

        /// <summary>
        /// Build the hueristic from a speicified start point.
        /// Only overwrites areas that can be reached from start
        /// </summary>
        /// <param name="start">Where to start the fill</param>
        /// <param name="region">The area to update (in tiles)</param>
        public void BuildHeuristic(Point start, Rectangle region)
        {
            if (!CanPath(start))
                return;

            var visibleTiles = Rectangle.Intersect(Bounds, new Rectangle(
                region.X / TileSize,
                region.Y / TileSize,
                Util.CeilDiv(region.Width, TileSize),
                Util.CeilDiv(region.Height, TileSize)
            ));

            ++pathGeneration;
            PathInfo[start.Y, start.X] = new PathTile
            {
                heuristic = 0,
                generation = pathGeneration
            };

            var queue = new Queue<HeuristicScore>(); //static?
            queue.Enqueue(new HeuristicScore { tile = start, value = 1 });
            while (queue.Count > 0)
            {
                var first = queue.Dequeue();

                MaxHeuristic = Math.Max(MaxHeuristic, first.value);

                foreach (var i in HeuristicDirections)
                {
                    var pos = first.tile + i;
                    if (CanPath(pos) &&
                        visibleTiles.Contains(pos) &&
                        PathInfo[pos.Y, pos.X].generation != pathGeneration)
                    {
                        PathInfo[pos.Y, pos.X] = new PathTile
                        {
                            heuristic = first.value,
                            generation = pathGeneration
                        };
                        queue.Enqueue(new HeuristicScore
                        {
                            tile = pos,
                            value = first.value + 1
                        });
                    }
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

            public override int GetHashCode()
            {
                return (int)((tile.X << 4) & 0xffff0000) + (tile.Y & 0x0000ffff);
            }
        }

        public const int StraightCost = 10;
        public const int DiagonalCost = 14;

        readonly static AStarPathNode[] SearchDirections =
        {
            new AStarPathNode(new Point(-1, -1), DiagonalCost, 0),
            new AStarPathNode(new Point( 0, -1), StraightCost, 0),
            new AStarPathNode(new Point( 1, -1), DiagonalCost, 0),
            new AStarPathNode(new Point(-1,  0), StraightCost, 0),
            new AStarPathNode(new Point( 1,  0), StraightCost, 0),
            new AStarPathNode(new Point(-1,  1), DiagonalCost, 0),
            new AStarPathNode(new Point( 0,  1), StraightCost, 0),
            new AStarPathNode(new Point( 1,  1), DiagonalCost, 0)
        };

        public static int ManhattanDistance(Point start, Point end)
        {
            const int StraightCost = 10;
            const int DiagonalCost = 14;

            var dx = Math.Abs(start.X - end.X);
            var dy = Math.Abs(start.Y - end.Y);
            return StraightCost * (dx + dy) + (DiagonalCost - 2 * StraightCost) * Math.Min(dx, dy);
        }

        //fluids should affect cost of moving through tile

        public List<Point> AStarBuildPath(Vector2 start, Vector2 goal)
        {
            var pstart = (start / Class.TileSize).ToPoint();
            var pgoal = (goal / Class.TileSize).ToPoint();

            if (!Class.CanPath(pstart) || !Class.CanPath(pgoal))
                return new List<Point> { pstart };

            AStarPathNode goalNode = new AStarPathNode(pgoal, 0, 0);
            goalNode.score = ManhattanDistance(pstart, pgoal);

            var open = new SortedList<int, HashSet<AStarPathNode>>(); //score: nodes -- //todo: use heap?
            var closed = new HashSet<Point>();
            var traveled = new List<Tuple<AStarPathNode, AStarPathNode>>(); //current and link

            int openCount = 0;
            void addOpen(AStarPathNode node)
            {
                if (!open.TryGetValue(node.score, out var list) || list == null)
                    open[node.score] = new HashSet<AStarPathNode> { node };
                else
                    list.Add(node);
                ++openCount;
            }
            void removeOpen(AStarPathNode node)
            {
                if (open.TryGetValue(node.score, out var set))
                {
                    set.Remove(node);
                    --openCount;
                }
            }

            addOpen(new AStarPathNode(pstart, 0, 0));
            //pathFindScores[(int)start.X, (int)start.Y] = GetManhattanDist(start, goal);

            while (openCount > 0)
            {
                AStarPathNode current = new AStarPathNode(Point.Zero, 0, int.MaxValue);
                foreach (var i in open)
                {
                    if (i.Value.Count > 0)
                    {
                        current = System.Linq.Enumerable.FirstOrDefault(i.Value);
                        break;
                    }
                }

                if (current.tile == pgoal) //path navigated
                    return ReconstructPath(traveled, goalNode);

                removeOpen(current);
                closed.Add(current.tile);
                //search in 8 directions around point
                foreach (var dir in SearchDirections)
                {
                    //jump to end
                    var neighbor = new AStarPathNode(current.tile + dir.tile, 0, 0);

                    if (closed.Contains(neighbor.tile) || !Class.CanPath(neighbor.tile)) //neighbor part of closed set or cannot be pathed on
                        continue;

                    //if diagonal check if cardinals are blocked to disable corner cutting
                    if ((Math.Abs(dir.tile.X) == Math.Abs(dir.tile.Y)) &&
                        (!Class.CanPath(new Point(current.tile.X, neighbor.tile.Y)) || !Class.CanPath(new Point(neighbor.tile.X, current.tile.Y))))
                        continue;

                    int cost = current.cost + dir.cost;

                    //the open list does not contain the neighbor or the neighbor has a better score
                    bool containsNeighbor = open.TryGetValue(neighbor.score, out var neighborsList) && neighborsList.Contains(neighbor);
                    if (!containsNeighbor || cost <= neighbor.cost)
                    {
                        if (containsNeighbor)
                            neighborsList.Remove(neighbor);

                        traveled.Add(new Tuple<AStarPathNode, AStarPathNode>(neighbor, current));
                        neighbor.cost = cost;
                        neighbor.score = cost + ManhattanDistance(neighbor.tile, pgoal);
                        addOpen(neighbor);
                    }
                }

            }

            return new List<Point> { pstart }; //No path between start and goal
        }

        List<Point> ReconstructPath(List<Tuple<AStarPathNode, AStarPathNode>> traveled, AStarPathNode current)
        {
            foreach (var t in traveled)
            {
                if (t.Item1.tile == current.tile)
                {
                    var p = ReconstructPath(traveled, t.Item2);
                    p.Add(current.tile);
                    return p;
                }
            }
            return new List<Point> { current.tile };
        }

        //https://stackoverflow.com/questions/15114950/how-to-improve-the-perfomance-of-my-a-path-finder/15120213#15120213
    }
}
